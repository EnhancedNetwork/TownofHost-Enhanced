using Hazel;
using System;
using System.Text.RegularExpressions;
using TOHE.Modules.ChatManager;
using TOHE.Patches;
using TOHE.Roles.Crewmate;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Coven;

internal class Ritualist : CovenManager
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Ritualist;
    private const int Id = 30800;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenPower;
    //==================================================================\\

    private static OptionItem MaxRitsPerRound;
    public static OptionItem TryHideMsg;
    public static OptionItem EnchantedKnowsCoven;
    public static OptionItem EnchantedKnowsEnchanted;

    private static readonly Dictionary<byte, int> RitualLimit = [];
    private static readonly Dictionary<byte, List<byte>> EnchantedPlayers = [];

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, CustomRoles.Ritualist, 1, zeroOne: false);
        MaxRitsPerRound = IntegerOptionItem.Create(Id + 10, "RitualistMaxRitsPerRound", new(1, 15, 1), 2, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Ritualist])
            .SetValueFormat(OptionFormat.Times);
        TryHideMsg = BooleanOptionItem.Create(Id + 11, "RitualistTryHideMsg", true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Ritualist])
            .SetColor(Color.green);
        EnchantedKnowsCoven = BooleanOptionItem.Create(Id + 12, "RitualistEnchantedKnowsCoven", true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Ritualist]);
        EnchantedKnowsEnchanted = BooleanOptionItem.Create(Id + 13, "RitualistEnchantedKnowsEnchanted", true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Ritualist]);

    }
    public override void Init()
    {
        RitualLimit.Clear();
        EnchantedPlayers.Clear();
    }
    public override void Add(byte PlayerId)
    {
        EnchantedPlayers[PlayerId] = [];
        RitualLimit.Add(PlayerId, MaxRitsPerRound.GetInt());
    }
    public override bool CanUseKillButton(PlayerControl pc) => HasNecronomicon(pc);
    public override void OnReportDeadBody(PlayerControl hatsune, NetworkedPlayerInfo miku)
    {
        foreach (var pid in RitualLimit.Keys)
        {
            RitualLimit[pid] = MaxRitsPerRound.GetInt();
        }
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.GetCustomRole().IsCovenTeam() && !(Main.PlayerStates[killer.PlayerId].IsRandomizer || Main.PlayerStates[target.PlayerId].IsRandomizer))
        {
            killer.Notify(GetString("CovenDontKillOtherCoven"));
            return false;
        }
        return true;
    }
    public override string NotifyPlayerName(PlayerControl seer, PlayerControl target, string TargetPlayerName = "", bool IsForMeeting = false)
        => IsForMeeting && seer.IsAlive() && target.IsAlive() ? ColorString(GetRoleColor(CustomRoles.Ritualist), target.PlayerId.ToString()) + " " + TargetPlayerName : "";
    public override string PVANameText(PlayerVoteArea pva, PlayerControl seer, PlayerControl target)
        => seer.IsAlive() && target.IsAlive() ? ColorString(GetRoleColor(CustomRoles.Ritualist), target.PlayerId.ToString()) + " " + pva.NameText.text : "";

    public static void RitualCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            ChatCommands.RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        var originMsg = text;

        if (!GameStates.IsMeeting || player == null || GameStates.IsExilling) return;
        if (!player.Is(CustomRoles.Ritualist)) return;

        bool isUI = player.IsModded();

        if (!player.IsAlive())
        {
            player.ShowInfoMessage(isUI, GetString("GuessDead"));
            return;
        }

        if (TryHideMsg.GetBool())
        {
            TryHideMsgForRitual();
            ChatManager.SendPreviousMessagesToAll();
        }
        else if (player.AmOwner) SendMessage(originMsg, 255, player.GetRealName());
        if (RitualLimit[player.PlayerId] <= 0)
        {
            player.ShowInfoMessage(isUI, GetString("RitualistRitualMax"));
            return;
        }

        if (!GetTargetAndRoleFromCommand(text, out byte targetId, out CustomRoles role, out string error))
        {
            player.ShowInfoMessage(isUI, error);
            return;
        }
        var target = GetPlayerById(targetId);
        if (role.IsAdditionRole())
        {
            player.ShowInfoMessage(isUI, GetString("RitualistGuessAddon"));
            return;
        }
        if (!target.Is(role))
        {
            RPC.PlaySoundRPC(Sounds.SabotageSound, player.PlayerId);
            player.ShowInfoMessage(isUI, GetString("RitualistRitualFail"));
            RitualLimit[player.PlayerId] = 0;
            return;
        }
        if (!target.CanBeRecruitedBy(player))
        {
            player.ShowInfoMessage(isUI, GetString("RitualistRitualImpossible"));
            return;
        }

        Logger.Info($"{player.GetNameWithRole()} enchant {target.GetNameWithRole()}", "Ritualist");

        RitualLimit[player.PlayerId]--;

        EnchantedPlayers[player.PlayerId].Add(target.PlayerId);
        RPC.PlaySoundRPC(Sounds.TaskUpdateSound, target.PlayerId);
        target.ShowInfoMessage(target.IsModded(), string.Format(GetString("RitualistConvertNotif"), CustomRoles.Ritualist.ToColoredString()));
        // SendMessage(string.Format(GetString("RitualistConvertNotif"), CustomRoles.Ritualist.ToColoredString()), target.PlayerId);
        RPC.PlaySoundRPC(Sounds.TaskComplete, player.PlayerId);
        player.ShowInfoMessage(isUI, string.Format(GetString("RitualistRitualSuccess"), target.GetRealName()));
        // SendMessage(string.Format(GetString("RitualistRitualSuccess"), target.GetRealName()), player.PlayerId);
    }
    private static void TryHideMsgForRitual()
    {
        ChatUpdatePatch.DoBlockChat = true;
        if (ChatManager.quickChatSpamMode != QuickChatSpamMode.QuickChatSpam_Disabled)
        {
            ChatManager.SendQuickChatSpam();
            ChatUpdatePatch.DoBlockChat = false;
            return;
        }

        List<CustomRoles> roles = [.. CustomRolesHelper.AllRoles.Where(x => x is not CustomRoles.NotAssigned)];
        var rd = IRandom.Instance;
        string msg;
        string[] command = GetString("Command.Ritual").Split("|");
        for (int i = 0; i < 20; i++)
        {
            msg = "/";
            if (rd.Next(100) < 20)
            {
                msg += "id";
            }
            else
            {
                msg += command[rd.Next(0, command.Length - 1)];
                msg += rd.Next(100) < 50 ? string.Empty : " ";
                msg += rd.Next(16).ToString();
                msg += rd.Next(100) < 50 ? string.Empty : " ";
                CustomRoles role = roles.RandomElement();
                msg += rd.Next(100) < 50 ? string.Empty : " ";
                msg += GetRoleName(role);

            }
            var player = Main.AllAlivePlayerControls.RandomElement();
            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg);
            var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
            writer.StartMessage(-1);
            writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
                .Write(msg)
                .EndRpc();
            writer.EndMessage();
            writer.SendMessage();
        }
        ChatUpdatePatch.DoBlockChat = false;
    }
    public override void AfterMeetingTasks()
    {
        foreach (var rit in EnchantedPlayers.Keys)
        {
            var ritualist = GetPlayerById(rit);
            foreach (var pc in EnchantedPlayers[rit])
            {
                ConvertRole(ritualist, GetPlayerById(pc));
            }
            EnchantedPlayers[rit].Clear();
        }
    }
    public static void ConvertRole(PlayerControl killer, PlayerControl target)
    {
        var addon = killer.GetBetrayalAddon(true);
        if (target.CanBeRecruitedBy(killer))
        {
            Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + addon.ToString(), "Ritualist Assign");
            target.RpcSetCustomRole(addon);
            if (addon is CustomRoles.Admired)
            {
                Admirer.AdmiredList[killer.PlayerId].Add(target.PlayerId);
                Admirer.SendRPC(killer.PlayerId, target.PlayerId);
            }
        }
    }
    private static bool GetTargetAndRoleFromCommand(string msg, out byte id, out CustomRoles role, out string error)
    {
        if (msg.StartsWith("/")) msg = msg.Replace("/", string.Empty);

        Regex r = new("\\d+"); // number regex
        MatchCollection mc = r.Matches(msg);
        string result = string.Empty;
        for (int i = 0; i < mc.Count; i++)
        {
            result += mc[i];
        }

        if (int.TryParse(result, out int num))
        {
            id = Convert.ToByte(num);
        }
        else
        {
            id = byte.MaxValue;
            error = GetString("RitualistCommandHelp");
            role = new();
            return false;
        }

        PlayerControl target = GetPlayerById(id);
        if (target == null || target.Data.IsDead || !target.IsAlive())
        {
            error = GetString("GuessNull");
            role = new();
            return false;
        }

        if (!ChatCommands.GetRoleByName(msg, out role))
        {
            error = GetString("RitualistCommandHelp");
            return false;
        }

        error = string.Empty;
        return true;
    }
    public static bool CanBeConverted(PlayerControl pc)
    {
        return pc != null && !(pc.GetCustomRole().IsCovenTeam() || pc.GetBetrayalAddon().IsCovenTeam()) && !pc.IsTransformedNeutralApocalypse() && !pc.Is(CustomRoles.Solsticer);
    }
}
