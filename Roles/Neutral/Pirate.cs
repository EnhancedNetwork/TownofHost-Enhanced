using Hazel;
using System.Text;
using System.Text.RegularExpressions;
using TOHE.Modules;
using TOHE.Modules.ChatManager;
using TOHE.Modules.Rpc;
using TOHE.Patches;
using TOHE.Roles.Core;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;


namespace TOHE.Roles.Neutral;
internal class Pirate : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Pirate;
    private const int Id = 15000;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Pirate);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralChaos;
    //==================================================================\\

    private static OptionItem SuccessfulDuelsToWin;
    private static OptionItem TryHideMsg;
    private static OptionItem DuelCooldown;

    private static readonly Dictionary<byte, bool> DuelDone = [];

    private static byte PirateTarget;
    private static int pirateChose, targetChose;

    public override void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Pirate);
        DuelCooldown = FloatOptionItem.Create(Id + 12, "DuelCooldown", new(0f, 180f, 2.5f), 22.5f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Pirate])
                .SetValueFormat(OptionFormat.Seconds);
        TryHideMsg = BooleanOptionItem.Create(Id + 10, "PirateTryHideMsg", true, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Pirate])
            .SetColor(Color.green);
        SuccessfulDuelsToWin = IntegerOptionItem.Create(Id + 11, "SuccessfulDuelsToWin", new(1, 20, 1), 2, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Pirate])
            .SetValueFormat(OptionFormat.Times);
    }

    public override void Init()
    {
        PirateTarget = byte.MaxValue;
        DuelDone.Clear();
        pirateChose = -1;
        targetChose = -1;
    }
    public override void Add(byte playerId)
    {
        DuelDone[playerId] = false;
        playerId.SetAbilityUseLimit(0);
    }
    public override void OnMeetingHudStart(PlayerControl pc)
    {
        if (PirateTarget == byte.MaxValue) return;

        var tpc = PirateTarget.GetPlayer();
        if (!tpc.IsAlive()) return;

        MeetingHudStartPatch.AddMsg(GetString("PirateMeetingMsg"), pc.PlayerId, ColorString(GetRoleColor(CustomRoles.Pirate), GetString("Pirate").ToUpper()));
        MeetingHudStartPatch.AddMsg(GetString("PirateTargetMeetingMsg"), tpc.PlayerId, ColorString(GetRoleColor(CustomRoles.Pirate), GetString("Pirate").ToUpper()));
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = DuelCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        Color TextColor = GetRoleColor(CustomRoles.Pirate).ShadeColor(0.25f);

        ProgressText.Append(ColorString(TextColor, $"({playerId.GetAbilityUseLimit()}/{SuccessfulDuelsToWin.GetInt()})"));
        return ProgressText.ToString();
    }

    private void SendRPC(byte target = byte.MaxValue)
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(target);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte target = reader.ReadByte();

        PirateTarget = target;
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.Is(CustomRoles.NiceMini) && Mini.Age < 18)
        {
            killer.Notify(ColorString(GetRoleColor(CustomRoles.Gangster), GetString("CantDuel")));
            return true;
        }

        if (target.Is(CustomRoles.Pestilence)) return true;
        if (PirateTarget != byte.MaxValue)
        {
            killer.Notify(GetString("PirateTargetAlreadyChosen"));
            return false;
        }
        Logger.Msg($"{killer.GetNameWithRole()} chose a target {target.GetNameWithRole()}", "Pirate");

        PirateTarget = target.PlayerId;
        SendRPC(target: target.PlayerId);

        DuelDone[PirateTarget] = false;

        if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(killer);
        else killer.SetKillCooldown();

        return false;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("PirateDuelButtonText"));
    }

    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Challenge");

    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isMeeting = false)
        => isMeeting && target.PlayerId == PirateTarget ? ColorString(GetRoleColor(CustomRoles.Pirate), " ⦿") : string.Empty;

    public override void OnCheckForEndVoting(PlayerState.DeathReason deathReason, params byte[] exileIds)
    {
        if (_Player == null || PirateTarget == byte.MaxValue) return;

        var pirateId = _state.PlayerId;
        if (!DuelDone[pirateId]) return;

        var pirateTarget = PirateTarget.GetPlayer();
        if (DuelDone[PirateTarget])
        {
            if (targetChose == pirateChose)
            {
                _Player.RpcIncreaseAbilityUseLimitBy(1);
                if (pirateTarget.IsAlive() && !pirateTarget.IsTransformedNeutralApocalypse())
                {
                    CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Pirate, PirateTarget);
                    pirateTarget.SetRealKiller(_Player);
                }
            }
        }
        else if (pirateTarget.IsAlive() && !pirateTarget.IsTransformedNeutralApocalypse())
        {
            CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Pirate, PirateTarget);
            pirateTarget.SetRealKiller(_Player);
        }
    }
    public override void AfterMeetingTasks()
    {
        if (_Player == null) return;
        var pirateId = _state.PlayerId;

        if (_Player.GetAbilityUseLimit() >= SuccessfulDuelsToWin.GetInt())
        {
            if (!CustomWinnerHolder.CheckForConvertedWinner(pirateId))
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Pirate);
                CustomWinnerHolder.WinnerIds.Add(pirateId);
            }
        }

        DuelDone.Clear();
        PirateTarget = byte.MaxValue;

        SendRPC(target: byte.MaxValue);
        foreach (byte playerId in Main.PlayerStates.Values.Where(x => x.MainRole == CustomRoles.Pirate).Select(x => x.PlayerId)) { DuelDone[playerId] = false; }
    }
    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        PirateTarget = byte.MaxValue;
        SendRPC(target: byte.MaxValue);
    }

    public static void DuelCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            ChatCommands.RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        var originMsg = text;

        if (!GameStates.IsMeeting || player == null || GameStates.IsExilling) return;
        if (!player.Is(CustomRoles.Pirate) && PirateTarget != player.PlayerId) return;

        if (!player.IsAlive())
        {
            player.ShowInfoMessage(false, GetString("PirateDead"));
            return;
        }

        if (TryHideMsg.GetBool())
        {
            TryHideMsgForDuel();
            ChatManager.SendPreviousMessagesToAll();
        }
        else if (player.AmOwner) SendMessage(originMsg, 255, player.GetRealName());

        if (!GetNumFromCommand(text, out int rpsOption, out string error))
        {
            SendMessage(error, player.PlayerId);
            return;
        }

        Logger.Info($"{player.GetNameWithRole()} selected {rpsOption}", "Pirate");

        if (DuelDone[player.PlayerId])
        {
            _ = new LateTask(() =>
            {
                player.ShowInfoMessage(false, GetString("DuelAlreadyDone"));
                Logger.Msg("Duel attempted more than once", "Pirate");
            }, 0.2f, "Pirate Duel Already Done");
            return;
        }
        else
        {
            if (player.Is(CustomRoles.Pirate))
            {
                pirateChose = rpsOption;
            }
            else
            {
                targetChose = rpsOption;
            }
            _ = new LateTask(() =>
            {
                player.ShowInfoMessage(false, string.Format(GetString("DuelDone"), rpsOption));
            }, 0.2f, "Pirate Duel Done");

            DuelDone[player.PlayerId] = true;
            return;
        }
    }

    private static bool GetNumFromCommand(string msg, out int rpsOpt, out string error)
    {
        if (msg.StartsWith("/")) msg = msg.Replace("/", string.Empty);

        Regex r = new("\\d+"); // regex for numbers
        MatchCollection mc = r.Matches(msg);
        string result = string.Empty;
        for (int i = 0; i < mc.Count; i++)
        {
            result += mc[i];
        }

        if (int.TryParse(result, out int num))
        {
            if (num < 0 || num > 2)
            {
                rpsOpt = -1;
                error = GetString("DuelHelp");
                return false;
            }
            else { rpsOpt = num; }
        }
        else
        {
            rpsOpt = -1;
            error = GetString("DuelHelp");
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static void TryHideMsgForDuel()
    {
        ChatUpdatePatch.DoBlockChat = true;

        if (ChatManager.quickChatSpamMode != QuickChatSpamMode.QuickChatSpam_Disabled)
        {
            ChatManager.SendQuickChatSpam();
            ChatUpdatePatch.DoBlockChat = false;
            return;
        }

        List<CustomRoles> roles = CustomRolesHelper.AllRoles.Where(x => x is not CustomRoles.NotAssigned).ToList();
        var rd = IRandom.Instance;
        string msg;
        string[] command = ["duel", "rps"];
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
                msg += " ";
                msg += rd.Next(0, 3).ToString();
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
}
