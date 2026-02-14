using Hazel;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using TOHE.Patches;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.MeetingHudStartPatch;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Retributionist : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Retributionist;
    private const int Id = 11000;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    private static OptionItem RetributionistCanKillNum;
    private static OptionItem MinimumPlayersAliveToRetri;
    private static OptionItem CanOnlyRetributeWithTasksDone;
    private static OptionItem PreventSeeRolesBeforeSkillUsedUp;

    private static readonly Dictionary<byte, int> RetributionistRevenged = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Retributionist);
        RetributionistCanKillNum = IntegerOptionItem.Create(Id + 10, "RetributionistCanKillNum", new(1, 15, 1), 1, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Retributionist])
            .SetValueFormat(OptionFormat.Players);
        PreventSeeRolesBeforeSkillUsedUp = BooleanOptionItem.Create(Id + 20, "PreventSeeRolesBeforeSkillUsedUp", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Retributionist]);
        MinimumPlayersAliveToRetri = IntegerOptionItem.Create(Id + 11, "MinimumPlayersAliveToRetri", new(0, 15, 1), 5, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Retributionist])
            .SetValueFormat(OptionFormat.Players);
        CanOnlyRetributeWithTasksDone = BooleanOptionItem.Create(Id + 12, "CanOnlyRetributeWithTasksDone", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Retributionist]);
        OverrideTasksData.Create(Id + 13, TabGroup.CrewmateRoles, CustomRoles.Retributionist);
    }
    public override void Init()
    {
        RetributionistRevenged.Clear();
    }
    public override void Add(byte playerId)
    {
        RetributionistRevenged[playerId] = 0;
    }
    public static bool PreventKnowRole(PlayerControl seer)
    {
        if (!seer.Is(CustomRoles.Retributionist) || seer.IsAlive()) return false;
        if (PreventSeeRolesBeforeSkillUsedUp.GetBool() && RetributionistRevenged.TryGetValue(seer.PlayerId, out var killNum) && killNum < RetributionistCanKillNum.GetInt())
            return true;
        return false;
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;

        if (!seer.IsAlive() && seen.IsAlive())
            return ColorString(GetRoleColor(CustomRoles.Retributionist), " " + seen.PlayerId.ToString()) + " ";

        return string.Empty;
    }

    public static void RetributionCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            ChatCommands.RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        if (!GameStates.IsInGame || player == null) return;
        if (!player.Is(CustomRoles.Retributionist)) return;

        bool isUI = player.IsModded();

        if (RetributionistCanKillNum.GetInt() < 1)
        {
            player.ShowInfoMessage(isUI, GetString("RetributionistKillDisable"));
            return;
        }
        int playerCount = Main.AllAlivePlayerControls.Count;

        if (playerCount <= MinimumPlayersAliveToRetri.GetInt() && !player.IsAlive())
        {
            player.ShowInfoMessage(isUI, GetString("RetributionistKillTooManyDead"));
            return;
        }

        if (CanOnlyRetributeWithTasksDone.GetBool())
        {
            if (!player.GetPlayerTaskState().IsTaskFinished && !player.IsAlive() && !CopyCat.playerIdList.Contains(player.PlayerId) && !Main.TasklessCrewmate.Contains(player.PlayerId))
            {
                player.ShowInfoMessage(isUI, GetString("RetributionistKillDisable"));
                return;
            }
        }
        if (player.IsAlive())
        {
            player.ShowInfoMessage(isUI, GetString("RetributionistAliveKill"));
            return;
        }

        if (args.Length < 2 || !int.TryParse(args[1], out int targetId))
        {
            bool canSeeRoles = PreventSeeRolesBeforeSkillUsedUp.GetBool();
            string txt = GetString("PlayerIdList");
            foreach (var npc in Main.EnumerateAlivePlayerControls())
                txt += $"\n{npc.PlayerId} → " + (canSeeRoles ? $"({npc.GetDisplayRoleAndSubName(npc, false, false)}) " : string.Empty) + npc.GetRealName();
            SendMessage(txt, player.PlayerId);
            return;
        }

        if (RetributionistRevenged.TryGetValue(player.PlayerId, out var killNum) && killNum >= RetributionistCanKillNum.GetInt())
        {
            player.ShowInfoMessage(isUI, GetString("RetributionistKillMax"));
            return;
        }
        else
        {
            RetributionistRevenged.TryAdd(player.PlayerId, 0);
        }

        PlayerControl target;
        try
        {
            target = GetPlayerById(targetId);
        }
        catch
        {
            player.ShowInfoMessage(isUI, GetString("Retributionist.InvalidTarget"));
            return;
        }

        if (target == null || !target.IsAlive())
        {
            player.ShowInfoMessage(isUI, GetString("RetributionistKillDead"));
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
            Logger.Info($"Guess Immune target {target.PlayerId} have role {target.GetCustomRole()}", "Retributionist");
            return;
        }

        Logger.Info($"{player.GetNameWithRole()} revenge {target.GetNameWithRole()}", "Retributionist");

        string Name = target.GetRealName();

        RetributionistRevenged[player.PlayerId]++;

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

            _ = new LateTask(() =>
            {
                SendMessage(string.Format(GetString("RetributionistKillSucceed"), Name), 255, ColorString(GetRoleColor(CustomRoles.Retributionist), GetString("Retributionist").ToUpper()), true);
            }, 0.6f, "Retributionist Kill");

        }, 0.2f, "Retributionist Start Kill");
    }

    private static void SendRPC(byte playerId)
    {
        var msg = new RpcRetributionistRevenge(PlayerControl.LocalPlayer.NetId, playerId);
        RpcUtils.LateBroadcastReliableMessage(msg);
    }
    public static void ReceiveRPC_Custom(MessageReader reader, PlayerControl pc)
    {
        int PlayerId = reader.ReadByte();
        RetributionCommand(pc, "Command.Retribution", $"/ret {PlayerId}", ["/ret", $"{PlayerId}"]);
        // RetributionistMsgCheck(pc, $"/ret {PlayerId}", true);
    }

    private static void RetributionistOnClick(byte playerId /*, MeetingHud __instance*/)
    {
        Logger.Msg($"Click: ID {playerId}", "Retributionist UI");
        var pc = GetPlayerById(playerId);
        if (pc == null || !pc.IsAlive() || !GameStates.IsVoting) return;
        if (AmongUsClient.Instance.AmHost) RetributionCommand(PlayerControl.LocalPlayer, "Command.Retribution", $"/ret {playerId}", ["/ret", $"{playerId}"]);
        else SendRPC(playerId);
    }
    public override void OnMeetingHudStart(PlayerControl pc)
    {
        if (!pc.IsAlive())
            AddMsg(GetString("RetributionistDeadMsg"), pc.PlayerId);
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class StartMeetingPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (PlayerControl.LocalPlayer.Is(CustomRoles.Retributionist) && !PlayerControl.LocalPlayer.IsAlive())
                CreateRetributionButton(__instance);
        }
    }
    public static void CreateRetributionButton(MeetingHud __instance)
    {
        foreach (var pva in __instance.playerStates.ToArray())
        {
            var pc = GetPlayerById(pva.TargetPlayerId);
            if (pc == null || !pc.IsAlive()) continue;

            GameObject template = pva.Buttons.transform.Find("CancelButton").gameObject;
            GameObject targetBox = UnityEngine.Object.Instantiate(template, pva.transform);
            targetBox.name = "ShootButton";
            targetBox.transform.localPosition = new Vector3(-0.35f, 0.03f, -1.31f);
            SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
            renderer.sprite = CustomButton.Get("MeetingKillButton");
            PassiveButton button = targetBox.GetComponent<PassiveButton>();
            button.OnClick.RemoveAllListeners();
            button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => RetributionistOnClick(pva.TargetPlayerId/*, __instance*/)));
        }
    }
}
