/*
using Hazel;
using System;
using UnityEngine;
using System.Text;
using static TOHE.Utils;
using static TOHE.Options;
using static TOHE.Translator;
using TOHE.Roles.Core;

namespace TOHE.Roles.Crewmate;

internal class Tracker : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 10000;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Tracker);
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem TrackLimitOpt;
    private static OptionItem OptionCanSeeLastRoomInMeeting;
    private static OptionItem CanGetColoredArrow;
    private static OptionItem HidesVote;
    private static OptionItem TrackerAbilityUseGainWithEachTaskCompleted;

    private static bool CanSeeLastRoomInMeeting;

    private static readonly Dictionary<byte, List<byte>> TrackerTarget = [];
    private static readonly Dictionary<byte, float> TempTrackLimit = [];
    private bool Didvote = false;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Tracker);
        TrackLimitOpt = IntegerOptionItem.Create(Id + 5, "FortuneTellerSkillLimit", new(0, 20, 1), 3, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Tracker])
            .SetValueFormat(OptionFormat.Times);
        CanGetColoredArrow = BooleanOptionItem.Create(Id + 6, "TrackerCanGetArrowColor", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Tracker]);
        OptionCanSeeLastRoomInMeeting = BooleanOptionItem.Create(Id + 7, "EvilTrackerCanSeeLastRoomInMeeting", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Tracker]);
       TrackerAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 9, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Tracker])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        TrackerTarget.Clear();
        CanSeeLastRoomInMeeting = OptionCanSeeLastRoomInMeeting.GetBool();
        TempTrackLimit.Clear();
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = TrackLimitOpt.GetInt();
        TrackerTarget.Add(playerId, []);
    }
    public override void Remove(byte playerId)
    {
        TrackerTarget.Remove(playerId);
    }
    public void SendRPC(int operate, byte trackerId = byte.MaxValue, byte targetId = byte.MaxValue)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetTrackerTarget, SendOption.Reliable, -1);
        writer.Write(trackerId);
        writer.Write(operate);
        if (operate == 0) writer.Write(targetId);
        if (operate == 2) writer.Write(AbilityLimit);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte trackerId = reader.ReadByte();
        int operate = reader.ReadInt32();
        if (operate == 0)
        {
            byte targetId = reader.ReadByte();

            Main.PlayerStates[trackerId].RoleClass.AbilityLimit--; 
            TrackerTarget[trackerId].Add(targetId);
            TargetArrow.Add(trackerId, targetId);
        }
        if (operate == 1)
        {
            TempTrackLimit[trackerId] = Main.PlayerStates[trackerId].RoleClass.AbilityLimit;
        }
        if (operate == 2)
        {
            float limit = reader.ReadSingle();
            Main.PlayerStates[trackerId].RoleClass.AbilityLimit = limit;
        }
    }
    public override string GetMark(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false) => !(seer == null || target == null) && TrackerTarget.ContainsKey(seer.PlayerId) && TrackerTarget[seer.PlayerId].Contains(target.PlayerId) ? Utils.ColorString(seer.GetRoleColor(), "◀") : "";
    public override void AfterMeetingTasks() => Didvote = false;
    public override bool CheckVote(PlayerControl player, PlayerControl target)
    {
        if (player == null || target == null) return true;
        if (AbilityLimit < 1 || Didvote) return true;
        if (player.PlayerId == target.PlayerId) return true;
        if (TrackerTarget[player.PlayerId].Contains(target.PlayerId)) return true;

        Didvote = true;
        AbilityLimit--;

        TrackerTarget[player.PlayerId].Add(target.PlayerId);
        TargetArrow.Add(player.PlayerId, target.PlayerId);

        SendRPC(0, player.PlayerId, target.PlayerId);
        SendMessage(GetString("VoteHasReturned"), player.PlayerId, title: ColorString(GetRoleColor(CustomRoles.Tracker), string.Format(GetString("VoteAbilityUsed"), GetString("Tracker"))));
        return false;
    }

    public override void OnReportDeadBody(PlayerControl reported, NetworkedPlayerInfo repoted)
    {
        foreach (var trackerId in _playerIdList) 
        {
            TempTrackLimit[trackerId] = AbilityLimit;
            SendRPC(1, trackerId);
        }
    }

    public override string GetSuffix(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false)
    {
        target ??= seer;

        if (isForMeeting)
        {
            if (IsTrackTarget(seer, target))
            {
                var roomName = GetArrowAndLastRoom(seer, target);
                return roomName.Length == 0 ? string.Empty : $"<size=1.5>{roomName}</size>";
            }
            return string.Empty;
        }
        else
        {
            return GetTargetArrow(seer, target);
        }
    }

    private bool IsTrackTarget(PlayerControl seer, PlayerControl target)
        => seer.IsAlive() && seer == _Player
            && TrackerTarget[seer.PlayerId].Contains(target.PlayerId)
            && target.IsAlive();

    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (player.IsAlive())
        {
            AbilityLimit += TrackerAbilityUseGainWithEachTaskCompleted.GetFloat();
            SendRPC(2, player.PlayerId);
        }
        return true;
    }
    private static string GetTargetArrow(PlayerControl seer, PlayerControl target)
    {
        if (seer == null || !seer.Is(CustomRoles.Tracker)) return string.Empty;
        if (target != null && seer.PlayerId != target.PlayerId) return string.Empty;
        if (!TrackerTarget.ContainsKey(seer.PlayerId)) return string.Empty;

        var arrows = "";
        var targetList = TrackerTarget[seer.PlayerId];

        foreach (var trackTarget in targetList)
        {
            if (!TrackerTarget[seer.PlayerId].Contains(trackTarget)) continue;

            var targetData = GetPlayerById(trackTarget);
            if (targetData == null) continue;

            var arrow = TargetArrow.GetArrows(seer, trackTarget);
            arrows += ColorString(CanGetColoredArrow.GetBool() ? Palette.PlayerColors[targetData.Data.DefaultOutfit.ColorId] : Color.white, arrow);
        }
        return arrows;
    }
    public static string GetArrowAndLastRoom(PlayerControl seer, PlayerControl target)
    {
        if (!CanSeeLastRoomInMeeting) return string.Empty;
        if (seer == null || target == null) return string.Empty;

        string text = ColorString(GetRoleColor(CustomRoles.Tracker), TargetArrow.GetArrows(seer, target.PlayerId));
        var room = Main.PlayerStates[target.PlayerId].LastRoom;
        if (room == null) text += ColorString(Color.gray, "@" + GetString("FailToTrack"));
        else text += ColorString(GetRoleColor(CustomRoles.Tracker), "@" + GetString(room.RoomId.ToString()));
        return text;
    }
    public override string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        var taskState11 = Main.PlayerStates?[playerId].TaskState;
        Color TextColor11;
        var TaskCompleteColor11 = Color.green;
        var NonCompleteColor11 = Color.yellow;
        var NormalColor11 = taskState11.IsTaskFinished ? TaskCompleteColor11 : NonCompleteColor11;
        TextColor11 = comms ? Color.gray : NormalColor11;
        string Completed11 = comms ? "?" : $"{taskState11.CompletedTasksCount}";
        Color TextColor111;
        if (AbilityLimit < 1) TextColor111 = Color.red;
        else TextColor111 = Color.white;
        ProgressText.Append(ColorString(TextColor11, $"({Completed11}/{taskState11.AllTasksCount})"));
        ProgressText.Append(ColorString(TextColor111, $" <color=#ffffff>-</color> {Math.Round(AbilityLimit, 1)}"));
        return ProgressText.ToString();
    }
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Track");
}
*/