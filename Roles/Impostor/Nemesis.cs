using AmongUs.GameOptions;
using Hazel;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using TOHE.Patches;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.MeetingHudStartPatch;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Nemesis : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Nemesis;
    private const int Id = 3600;
    public override CustomRoles ThisRoleBase => LegacyNemesis.GetBool() ? CustomRoles.Shapeshifter : CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    private static OptionItem NemesisCanKillNum;
    public static OptionItem PreventSeeRolesBeforeSkillUsedUp;
    public static OptionItem LegacyNemesis;
    private static OptionItem NemesisShapeshiftCD;
    private static OptionItem NemesisShapeshiftDur;

    private static readonly Dictionary<byte, int> NemesisRevenged = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Nemesis);
        NemesisCanKillNum = IntegerOptionItem.Create(Id + 10, "NemesisCanKillNum", new(0, 15, 1), 1, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Nemesis])
                .SetValueFormat(OptionFormat.Players);
        PreventSeeRolesBeforeSkillUsedUp = BooleanOptionItem.Create(Id + 14, "PreventSeeRolesBeforeSkillUsedUp", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Nemesis]);
        LegacyNemesis = BooleanOptionItem.Create(Id + 11, "UseLegacyVersion", false, TabGroup.ImpostorRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Nemesis]);
        NemesisShapeshiftCD = FloatOptionItem.Create(Id + 12, GeneralOption.ShapeshifterBase_ShapeshiftCooldown, new(1f, 180f, 1f), 15f, TabGroup.ImpostorRoles, false)
                .SetParent(LegacyNemesis)
                .SetValueFormat(OptionFormat.Seconds);
        NemesisShapeshiftDur = FloatOptionItem.Create(Id + 13, GeneralOption.ShapeshifterBase_ShapeshiftDuration, new(1f, 180f, 1f), 30f, TabGroup.ImpostorRoles, false)
                .SetParent(LegacyNemesis)
                .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        NemesisRevenged.Clear();
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = NemesisShapeshiftCD.GetFloat();
        AURoleOptions.ShapeshifterDuration = NemesisShapeshiftDur.GetFloat();
    }
    public static bool PreventKnowRole(PlayerControl seer)
    {
        if (!seer.Is(CustomRoles.Nemesis) || seer.IsAlive()) return false;
        if (PreventSeeRolesBeforeSkillUsedUp.GetBool() && NemesisRevenged.TryGetValue(seer.PlayerId, out var killNum) && killNum < NemesisCanKillNum.GetInt())
            return true;
        return false;
    }
    public override void OnMeetingHudStart(PlayerControl player)
    {
        if (!player.IsAlive())
            AddMsg(GetString("NemesisDeadMsg"), player.PlayerId);
    }

    public static void RevengeCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            ChatCommands.RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        if (!GameStates.IsInGame || player == null) return;
        if (!player.Is(CustomRoles.Nemesis)) return;

        bool isUI = player.IsModded();

        if (NemesisCanKillNum.GetInt() < 1)
        {
            player.ShowInfoMessage(isUI, GetString("NemesisKillDisable"));
            return;
        }

        if (player.IsAlive())
        {
            player.ShowInfoMessage(isUI, GetString("NemesisAliveKill"));
            return;
        }

        if (args.Length < 2 || !int.TryParse(args[1], out int targetId))
        {
            bool canSeeRoles = PreventSeeRolesBeforeSkillUsedUp.GetBool();
            string txt = GetString("PlayerIdList");
            foreach (var npc in Main.AllAlivePlayerControls)
                txt += $"\n{npc.PlayerId} → " + (canSeeRoles ? $"({npc.GetDisplayRoleAndSubName(npc, false, false)}) " : string.Empty) + npc.GetRealName();
            Utils.SendMessage(txt, player.PlayerId);
            return;
        }

        if (NemesisRevenged.TryGetValue(player.PlayerId, out var killNum) && killNum >= NemesisCanKillNum.GetInt())
        {
            player.ShowInfoMessage(isUI, GetString("NemesisKillMax"));
            return;
        }
        else
        {
            NemesisRevenged.Add(player.PlayerId, 0);
        }

        PlayerControl target;
        try
        {
            target = Utils.GetPlayerById(targetId);
        }
        catch
        {
            player.ShowInfoMessage(isUI, GetString("Nemesis.InvalidTarget"));
            return;
        }

        if (target == null || !target.IsAlive())
        {
            player.ShowInfoMessage(isUI, GetString("NemesisKillDead"));
            return;
        }
        else if (target.IsTransformedNeutralApocalypse())
        {
            player.ShowInfoMessage(isUI, GetString("ApocalypseImmune"));
            return;
        }
        else if (target.Is(CustomRoles.NiceMini) && Mini.Age < 18)
        {
            player.ShowInfoMessage(isUI, GetString("GuessMini"));
            return;
        }
        else if (target.Is(CustomRoles.Solsticer))
        {
            player.ShowInfoMessage(isUI, GetString("GuessSolsticer"));
            return;
        }
        else if (target.Is(CustomRoles.CursedWolf))
        {
            player.ShowInfoMessage(isUI, GetString("GuessImmune"));
            return;
        }
        else if (!player.RpcCheckAndMurder(target, true))
        {
            player.ShowInfoMessage(isUI, GetString("GuessImmune"));
            Logger.Info($"Guess Immune target {target.PlayerId} have role {target.GetCustomRole()}", "Nemesis");
            return;
        }

        Logger.Info($"{player.GetNameWithRole()} revenge {target.GetNameWithRole()}", "Nemesis");

        string Name = target.GetRealName();

        NemesisRevenged[player.PlayerId]++;

        CustomSoundsManager.RPCPlayCustomSoundAll("AWP");

        _ = new LateTask(() =>
        {
            target.SetDeathReason(PlayerState.DeathReason.Revenge);
            if (GameStates.IsMeeting)
            {
                Main.PlayersDiedInMeeting.Add(target.PlayerId);
                GuessManager.RpcGuesserMurderPlayer(target);
                MurderPlayerPatch.AfterPlayerDeathTasks(player, target, true);
            }
            else
            {
                target.RpcMurderPlayer(target);
            }
            target.SetRealKiller(player);

            _ = new LateTask(() => { Utils.SendMessage(string.Format(GetString("NemesisKillSucceed"), Name), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Nemesis), GetString("Nemesis").ToUpper()), true); }, 0.6f, "Nemesis Kill");
        }, 0.2f, "Nemesis Start Kill");
    }

    private static void SendRPC(byte playerId)
    {
        var msg = new RpcNemesisRevenge(PlayerControl.LocalPlayer.NetId, playerId);
        RpcUtils.LateBroadcastReliableMessage(msg);
    }
    public static void ReceiveRPC_Custom(MessageReader reader, PlayerControl pc)
    {
        int PlayerId = reader.ReadByte();
        RevengeCommand(pc, "Command.Revenge", $"/rv {PlayerId}", ["/rv", $"{PlayerId}"]);
        // NemesisMsgCheck(pc, $"/rv {PlayerId}", true);
    }

    public override bool CanUseKillButton(PlayerControl pc) => CheckCanUseKillButton(pc);

    public static bool CheckCanUseKillButton(PlayerControl pc)
    {
        if (Main.PlayerStates == null) return false;

        //  Number of Living Impostors excluding Nemesis
        int LivingImpostorsNum = 0;
        foreach (var player in Main.AllAlivePlayerControls)
        {
            var role = player.GetCustomRole();
            if (role != CustomRoles.Nemesis && role.IsImpostor() && !player.Is(CustomRoles.Narc)) LivingImpostorsNum++;
        }

        // if Nemesis is Narc, they can use kill buttom when all Sheriffs are dead
        // if not, they can use kill button when LivingImpostorNum is 0
        return pc.Is(CustomRoles.Narc) ? !CustomRoles.Sheriff.RoleExist() : LivingImpostorsNum <= 0;
    }

    private static void NemesisOnClick(byte playerId /*, MeetingHud __instance*/)
    {
        Logger.Msg($"Click: ID {playerId}", "Nemesis UI");
        var pc = Utils.GetPlayerById(playerId);
        if (pc == null || !pc.IsAlive() || !GameStates.IsVoting) return;
        if (AmongUsClient.Instance.AmHost) RevengeCommand(pc, "Command.Revenge", $"/rv {playerId}", ["/rv", $"{playerId}"]);
        else SendRPC(playerId);
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;

        if (!seer.IsAlive() && seen.IsAlive())
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Nemesis), " " + seen.PlayerId.ToString()) + " ";

        return string.Empty;
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class StartMeetingPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (PlayerControl.LocalPlayer.Is(CustomRoles.Nemesis) && !PlayerControl.LocalPlayer.IsAlive())
                CreateRevengeButton(__instance);
        }
    }
    public static void CreateRevengeButton(MeetingHud __instance)
    {
        foreach (var pva in __instance.playerStates.ToArray())
        {
            var pc = Utils.GetPlayerById(pva.TargetPlayerId);
            if (pc == null || !pc.IsAlive()) continue;

            GameObject template = pva.Buttons.transform.Find("CancelButton").gameObject;
            GameObject targetBox = Object.Instantiate(template, pva.transform);
            targetBox.name = "ShootButton";
            targetBox.transform.localPosition = new Vector3(-0.35f, 0.03f, -1.31f);
            SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
            renderer.sprite = CustomButton.Get("MeetingKillButton");
            PassiveButton button = targetBox.GetComponent<PassiveButton>();
            button.OnClick.RemoveAllListeners();
            button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => NemesisOnClick(pva.TargetPlayerId/*, __instance*/)));
        }
    }
}
