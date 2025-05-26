using Hazel;
using TOHE.Modules;
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

    public static bool RetributionistMsgCheck(PlayerControl pc, string msg, bool isUI = false)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsInGame || pc == null) return false;
        if (!pc.Is(CustomRoles.Retributionist)) return false;
        msg = msg.Trim().ToLower();
        if (msg.Length < 4 || msg[..4] != "/ret") return false;
        if (RetributionistCanKillNum.GetInt() < 1)
        {
            pc.ShowInfoMessage(isUI, GetString("RetributionistKillDisable"));
            return true;
        }
        int playerCount = Main.AllAlivePlayerControls.Length;

        if (playerCount <= MinimumPlayersAliveToRetri.GetInt() && !pc.IsAlive())
        {
            pc.ShowInfoMessage(isUI, GetString("RetributionistKillTooManyDead"));
            return true;
        }


        if (CanOnlyRetributeWithTasksDone.GetBool())
        {
            if (!pc.GetPlayerTaskState().IsTaskFinished && !pc.IsAlive() && !CopyCat.playerIdList.Contains(pc.PlayerId) && !Main.TasklessCrewmate.Contains(pc.PlayerId))
            {
                pc.ShowInfoMessage(isUI, GetString("RetributionistKillDisable"));
                return true;
            }
        }
        if (pc.IsAlive())
        {
            pc.ShowInfoMessage(isUI, GetString("RetributionistAliveKill"));
            return true;
        }

        if (msg == "/ret")
        {
            bool canSeeRoles = PreventSeeRolesBeforeSkillUsedUp.GetBool();
            string text = GetString("PlayerIdList");
            foreach (var npc in Main.AllAlivePlayerControls)
                text += $"\n{npc.PlayerId} → " + (canSeeRoles ? $"({npc.GetDisplayRoleAndSubName(npc, false, false)}) " : string.Empty) + npc.GetRealName();
            SendMessage(text, pc.PlayerId);
            return true;
        }

        if (RetributionistRevenged.TryGetValue(pc.PlayerId, out var killNum) && killNum >= RetributionistCanKillNum.GetInt())
        {
            pc.ShowInfoMessage(isUI, GetString("RetributionistKillMax"));
            return true;
        }
        else
        {
            RetributionistRevenged.TryAdd(pc.PlayerId, 0);
        }

        int targetId;
        PlayerControl target;
        try
        {
            targetId = int.Parse(msg.Replace("/ret", string.Empty));
            target = GetPlayerById(targetId);
        }
        catch
        {
            pc.ShowInfoMessage(isUI, GetString("RetributionistKillDead"));
            return true;
        }

        if (target == null || !target.IsAlive())
        {
            pc.ShowInfoMessage(isUI, GetString("RetributionistKillDead"));
            return true;
        }
        else if (target.IsTransformedNeutralApocalypse())
        {
            pc.ShowInfoMessage(isUI, GetString("ApocalypseImmune"));
            return true;
        }
        else if (target.Is(CustomRoles.NiceMini) && Mini.Age < 18)
        {
            pc.ShowInfoMessage(isUI, GetString("GuessMini"));
            return true;
        }
        else if (target.Is(CustomRoles.Solsticer))
        {
            pc.ShowInfoMessage(isUI, GetString("GuessSolsticer"));
            return true;
        }
        else if (target.Is(CustomRoles.Jinx) || target.Is(CustomRoles.CursedWolf))
        {
            pc.ShowInfoMessage(isUI, GetString("GuessImmune"));
            return true;
        }
        else if (pc.RpcCheckAndMurder(target, true) == false)
        {
            pc.ShowInfoMessage(isUI, GetString("GuessImmune"));
            Logger.Info($"Guess Immune target {target.PlayerId} have role {target.GetCustomRole()}", "Retributionist");
            return true;
        }

        Logger.Info($"{pc.GetNameWithRole()} revenge {target.GetNameWithRole()}", "Retributionist");

        string Name = target.GetRealName();

        RetributionistRevenged[pc.PlayerId]++;

        CustomSoundsManager.RPCPlayCustomSoundAll("AWP");

        _ = new LateTask(() =>
        {
            target.SetDeathReason(PlayerState.DeathReason.Revenge);
            if (GameStates.IsMeeting)
            {
                Main.PlayersDiedInMeeting.Add(target.PlayerId);
                GuessManager.RpcGuesserMurderPlayer(target);
                MurderPlayerPatch.AfterPlayerDeathTasks(pc, target, true);
            }
            else
            {
                target.RpcMurderPlayer(target);
            }
            target.SetRealKiller(pc);

            _ = new LateTask(() =>
            {
                SendMessage(string.Format(GetString("RetributionistKillSucceed"), Name), 255, ColorString(GetRoleColor(CustomRoles.Retributionist), GetString("Retributionist").ToUpper()), true);
            }, 0.6f, "Retributionist Kill");

        }, 0.2f, "Retributionist Start Kill");
        return true;
    }

    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RetributionistRevenge, SendOption.Reliable, -1);
        writer.Write(playerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC_Custom(MessageReader reader, PlayerControl pc)
    {
        int PlayerId = reader.ReadByte();
        RetributionistMsgCheck(pc, $"/ret {PlayerId}", true);
    }

    private static void RetributionistOnClick(byte playerId /*, MeetingHud __instance*/)
    {
        Logger.Msg($"Click: ID {playerId}", "Retributionist UI");
        var pc = GetPlayerById(playerId);
        if (pc == null || !pc.IsAlive() || !GameStates.IsVoting) return;
        if (AmongUsClient.Instance.AmHost) RetributionistMsgCheck(PlayerControl.LocalPlayer, $"/ret {playerId}", true);
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
                CreateJudgeButton(__instance);
        }
    }
    public static void CreateJudgeButton(MeetingHud __instance)
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
