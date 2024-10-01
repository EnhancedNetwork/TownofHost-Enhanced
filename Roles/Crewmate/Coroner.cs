using Hazel;
using System;
using System.Text;
using UnityEngine;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;
using InnerNet;

namespace TOHE.Roles.Crewmate;

internal class Coroner : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 7700;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Coroner);
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static readonly Dictionary<byte, HashSet<byte>> CoronerTargets = [];

    private static OptionItem ArrowsPointingToDeadBody;
    private static OptionItem UseLimitOpt;
    private static OptionItem LeaveDeadBodyUnreportable;
    private static OptionItem CoronerAbilityUseGainWithEachTaskCompleted;
    private static OptionItem InformKillerBeingTracked;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Coroner);
        ArrowsPointingToDeadBody = BooleanOptionItem.Create(Id + 10, "CoronerArrowsPointingToDeadBody", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Coroner]);
        LeaveDeadBodyUnreportable = BooleanOptionItem.Create(Id + 11, "CoronerLeaveDeadBodyUnreportable", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Coroner]);
        UseLimitOpt = IntegerOptionItem.Create(Id + 12, "AbilityUseLimit", new(0, 20, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Coroner])
        .SetValueFormat(OptionFormat.Times);
        CoronerAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 13, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Coroner])
        .SetValueFormat(OptionFormat.Times);
        InformKillerBeingTracked = BooleanOptionItem.Create(Id + 14, "CoronerInformKillerBeingTracked", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Coroner]);
    }
    public override void Init()
    {
        CoronerTargets.Clear();
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = UseLimitOpt.GetInt();
        CoronerTargets.Add(playerId, []);

        if (AmongUsClient.Instance.AmHost)
        {
            CustomRoleManager.CheckDeadBodyOthers.Add(CheckDeadBody);
        }
    }
    public override void Remove(byte playerId)
    {
        CoronerTargets.Remove(playerId);
    }

    private void SendRPCLimit(byte playerId, int operate, byte targetId = 0xff)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
        writer.Write(playerId);
        writer.Write(operate);
        writer.Write(AbilityLimit);
        if (operate != 2) writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte pid = reader.ReadByte();
        int opt = reader.ReadInt32();
        float limit = reader.ReadSingle();
        AbilityLimit = limit;
        if (opt != 2)
        {
            byte tid = reader.ReadByte();
            if (!CoronerTargets.ContainsKey(pid)) CoronerTargets[pid] = [];
            CoronerTargets[pid].Add(tid);
            if (opt == 1) Main.UnreportableBodies.Add(tid);
        }
    }

    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (player.IsAlive())
        {
            AbilityLimit += CoronerAbilityUseGainWithEachTaskCompleted.GetFloat();
            SendRPCLimit(player.PlayerId, operate: 2);
        }
        return true;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId) => hud.ReportButton.OverrideText(GetString("CoronerReportButtonText"));
    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody, PlayerControl killer)
    {

        if (reporter.Is(CustomRoles.Coroner))
        {
            if (killer != null)
            {
                FindKiller(reporter, deadBody, killer);
            }
            else
            {
                reporter.Notify(GetString("CoronerNoTrack"));
            }
            return false;
        }
        return true;
    }

    private bool FindKiller(PlayerControl pc, NetworkedPlayerInfo deadBody, PlayerControl killer)
    {
        if (CoronerTargets.TryGetValue(pc.PlayerId, out var target) && target.Contains(killer.PlayerId))
        {
            return true;
        }

        LocateArrow.Remove(pc.PlayerId, deadBody.Object.transform.position);

        if (AbilityLimit >= 1)
        {
            CoronerTargets[pc.PlayerId].Add(killer.PlayerId);
            TargetArrow.Add(pc.PlayerId, killer.PlayerId);

            pc.Notify(GetString("CoronerTrackRecorded"));
            AbilityLimit -= 1;
            int operate = 0;
            if (LeaveDeadBodyUnreportable.GetBool())
            {
                Main.UnreportableBodies.Add(deadBody.PlayerId);
                operate = 1;
            }
            SendRPCLimit(pc.PlayerId, operate, targetId: deadBody.PlayerId);

            if (InformKillerBeingTracked.GetBool())
            {
                killer.Notify(GetString("CoronerIsTrackingYou"));
            }
        }
        else
        {
            pc.Notify(GetString("OutOfAbilityUsesDoMoreTasks"));
        }
        return true;
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        foreach (var apc in _playerIdList.ToArray())
        {
            LocateArrow.RemoveAllTarget(apc);
        }

        foreach (var bloodhound in CoronerTargets)
        {
            foreach (var tar in bloodhound.Value.ToArray())
            {
                TargetArrow.Remove(bloodhound.Key, tar);
            }

            CoronerTargets[bloodhound.Key].Clear();
        }
    }

    private void CheckDeadBody(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        if (!ArrowsPointingToDeadBody.GetBool() || inMeeting || target.IsDisconnected()) return;

        foreach (var pc in _playerIdList.ToArray())
        {
            var player = GetPlayerById(pc);
            if (player == null || !player.IsAlive()) continue;
            LocateArrow.Add(pc, target.transform.position);
        }
    }

    public override string GetSuffix(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false)
    {
        if (!seer.Is(CustomRoles.Coroner)) return "";
        if (target != null && seer.PlayerId != target.PlayerId) return "";
        if (GameStates.IsMeeting) return "";
        if (CoronerTargets.ContainsKey(seer.PlayerId) && CoronerTargets[seer.PlayerId].Any())
        {
            var arrows = "";
            foreach (var targetId in CoronerTargets[seer.PlayerId])
            {
                var arrow = TargetArrow.GetArrows(seer, targetId);
                arrows += ColorString(seer.GetRoleColor(), arrow);
            }
            return arrows;
        }
        return ColorString(Color.white, LocateArrow.GetArrows(seer));
    }
    public override string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        var taskState12 = Main.PlayerStates?[playerId].TaskState;
        Color TextColor12;
        var TaskCompleteColor12 = Color.green;
        var NonCompleteColor12 = Color.yellow;
        var NormalColor12 = taskState12.IsTaskFinished ? TaskCompleteColor12 : NonCompleteColor12;
        TextColor12 = comms ? Color.gray : NormalColor12;
        string Completed12 = comms ? "?" : $"{taskState12.CompletedTasksCount}";
        Color TextColor121;
        if (AbilityLimit < 1) TextColor121 = Color.red;
        else TextColor121 = Color.white;
        ProgressText.Append(ColorString(TextColor12, $"({Completed12}/{taskState12.AllTasksCount})"));
        ProgressText.Append(ColorString(TextColor121, $" <color=#ffffff>-</color> {Math.Round(AbilityLimit, 1)}"));
        return ProgressText.ToString();
    }
}
