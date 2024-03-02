using Hazel;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Text;
using TOHE.Roles.Core;
using static TOHE.Utils;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.CheckForEndVotingPatch;

namespace TOHE.Roles.Crewmate
{
    internal class Tracker : RoleBase
    {
        private static readonly int Id = 10000;
        private static List<byte> playerIdList = [];
        private static bool On = false;
        public override bool IsEnable => On;
        public static bool HasEnabled => CustomRoles.Tracker.IsClassEnable();
        public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;

        private static OptionItem TrackLimitOpt;
        private static OptionItem OptionCanSeeLastRoomInMeeting;
        private static OptionItem CanGetColoredArrow;
        public static OptionItem HidesVote;
        public static OptionItem TrackerAbilityUseGainWithEachTaskCompleted;

        public static bool CanSeeLastRoomInMeeting;

        public static Dictionary<byte, float> TrackLimit = [];
        public static Dictionary<byte, List<byte>> TrackerTarget = [];
        public static Dictionary<byte, float> TempTrackLimit = [];

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Tracker);
            TrackLimitOpt = IntegerOptionItem.Create(Id + 5, "FortuneTellerSkillLimit", new(0, 20, 1), 3, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Tracker])
                .SetValueFormat(OptionFormat.Times);
            CanGetColoredArrow = BooleanOptionItem.Create(Id + 6, "TrackerCanGetArrowColor", true, TabGroup.CrewmateRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Tracker]);
            OptionCanSeeLastRoomInMeeting = BooleanOptionItem.Create(Id + 7, "EvilTrackerCanSeeLastRoomInMeeting", true, TabGroup.CrewmateRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Tracker]);
            HidesVote = BooleanOptionItem.Create(Id + 8, "TrackerHideVote", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Tracker]);
            TrackerAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 9, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Tracker])
                .SetValueFormat(OptionFormat.Times);
        }
        public override void Init()
        {
            playerIdList = [];
            TrackLimit = [];
            TrackerTarget = [];
            CanSeeLastRoomInMeeting = OptionCanSeeLastRoomInMeeting.GetBool();
            TempTrackLimit = [];
            On = false;
        }
        public override void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            TrackLimit.Add(playerId, TrackLimitOpt.GetInt());
            TrackerTarget.Add(playerId, []);
            On = true;
        }
        public override void Remove(byte playerId)
        {
            playerIdList.Remove(playerId);
            TrackLimit.Remove(playerId);
            TrackerTarget.Remove(playerId);
        }
        public override bool HideVote(byte playerId) => CheckRole(playerId, CustomRoles.Tracker) && Tracker.HidesVote.GetBool() && Tracker.TempTrackLimit[playerId] > 0;
        public static void SendRPC(int operate, byte trackerId = byte.MaxValue, byte targetId = byte.MaxValue)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetTrackerTarget, SendOption.Reliable, -1);
            writer.Write(trackerId);
            writer.Write(operate);
            if (operate == 0) writer.Write(targetId);
            if (operate == 2) writer.Write(TrackLimit[trackerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            byte trackerId = reader.ReadByte();
            int operate = reader.ReadInt32();
            if (operate == 0)
            {
                byte targetId = reader.ReadByte();

                TrackLimit[trackerId]--;
                TrackerTarget[trackerId].Add(targetId);
                TargetArrow.Add(trackerId, targetId);
            }
            if (operate == 1)
            {
                TempTrackLimit[trackerId] = TrackLimit[trackerId];
            }
            if (operate == 2)
            {
                float limit = reader.ReadSingle();
                TrackLimit[trackerId] = limit;
            }
        }
        public override string GetMark(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false) => !(seer == null || target == null) && TrackerTarget.ContainsKey(seer.PlayerId) && TrackerTarget[seer.PlayerId].Contains(target.PlayerId) ? Utils.ColorString(seer.GetRoleColor(), "◀") : "";

        public override void OnVote(PlayerControl player, PlayerControl target)
        {
            if (player == null || target == null) return;
            if (TrackLimit[player.PlayerId] < 1) return;
            if (player.PlayerId == target.PlayerId) return;
            if (TrackerTarget[player.PlayerId].Contains(target.PlayerId)) return;

            TrackLimit[player.PlayerId]--;

            TrackerTarget[player.PlayerId].Add(target.PlayerId);
            TargetArrow.Add(player.PlayerId, target.PlayerId);

            SendRPC(0,player.PlayerId, target.PlayerId);
        }

        public override void OnReportDeadBody(PlayerControl reported, PlayerControl repoted)
        {
            foreach (var trackerId in playerIdList) 
            {
                TempTrackLimit[trackerId] = TrackLimit[trackerId];
                SendRPC(1, trackerId);
            }
        }

        public override string GetSuffix(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false)
        {
            if (seer == null) return string.Empty;
            if (!seer.Is(CustomRoles.Tracker)) return string.Empty;
            if (target != null && seer.PlayerId != target.PlayerId) return string.Empty;
            if (!TrackerTarget.ContainsKey(seer.PlayerId)) return string.Empty;
            if (GameStates.IsMeeting) return string.Empty;

            var arrows = "";
            var targetList = TrackerTarget[seer.PlayerId];

            foreach (var trackTarget in targetList)
            {
                if (!TrackerTarget[seer.PlayerId].Contains(trackTarget)) continue;

                var targetData = Utils.GetPlayerById(trackTarget);
                if (targetData == null) continue;

                var arrow = TargetArrow.GetArrows(seer, trackTarget);
                arrows += Utils.ColorString(CanGetColoredArrow.GetBool() ? Palette.PlayerColors[targetData.Data.DefaultOutfit.ColorId] : Color.white, arrow);
            }
            return arrows;
        }

        public static bool IsTrackTarget(PlayerControl seer, PlayerControl target)
            => seer.IsAlive() && playerIdList.Contains(seer.PlayerId)
                && TrackerTarget[seer.PlayerId].Contains(target.PlayerId)
                && target.IsAlive();
        public override void OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
        {
            if (!player.IsAlive()) return;
            Tracker.TrackLimit[player.PlayerId] += Tracker.TrackerAbilityUseGainWithEachTaskCompleted.GetFloat();
            Tracker.SendRPC(2, player.PlayerId);
        }
        public static string GetArrowAndLastRoom(PlayerControl seer, PlayerControl target)
        {
            if (seer == null || target == null) return string.Empty;

            string text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Tracker), TargetArrow.GetArrows(seer, target.PlayerId));
            var room = Main.PlayerStates[target.PlayerId].LastRoom;
            if (room == null) text += Utils.ColorString(Color.gray, "@" + GetString("FailToTrack"));
            else text += Utils.ColorString(Utils.GetRoleColor(CustomRoles.Tracker), "@" + GetString(room.RoomId.ToString()));
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
            if (Tracker.TrackLimit[playerId] < 1) TextColor111 = Color.red;
            else TextColor111 = Color.white;
            ProgressText.Append(ColorString(TextColor11, $"({Completed11}/{taskState11.AllTasksCount})"));
            ProgressText.Append(ColorString(TextColor111, $" <color=#ffffff>-</color> {Math.Round(Tracker.TrackLimit[playerId], 1)}"));
            return ProgressText.ToString();
        }
        public override Sprite AbilityButtonSprite => CustomButton.Get("Track");
    }
}