using Hazel;
using UnityEngine;
using System.Collections.Generic;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate
{
    public static class Tracker
    {
        private static readonly int Id = 8300;
        private static List<byte> playerIdList = new();
        public static bool IsEnable = false;

        private static OptionItem TrackLimitOpt;
        private static OptionItem OptionCanSeeLastRoomInMeeting;
        private static OptionItem CanGetColoredArrow;
        public static OptionItem HideVote;
        public static OptionItem TrackerAbilityUseGainWithEachTaskCompleted;

        public static bool CanSeeLastRoomInMeeting;

        public static Dictionary<byte, float> TrackLimit = new();
        public static Dictionary<byte, List<byte>> TrackerTarget = new();
        public static Dictionary<byte, float> TempTrackLimit = new();

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Tracker);
            TrackLimitOpt = IntegerOptionItem.Create(Id + 5, "DivinatorSkillLimit", new(0, 20, 1), 3, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Tracker])
                .SetValueFormat(OptionFormat.Times);
            CanGetColoredArrow = BooleanOptionItem.Create(Id + 6, "TrackerCanGetArrowColor", true, TabGroup.CrewmateRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Tracker]);
            OptionCanSeeLastRoomInMeeting = BooleanOptionItem.Create(Id + 7, "EvilTrackerCanSeeLastRoomInMeeting", true, TabGroup.CrewmateRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Tracker]);
            HideVote = BooleanOptionItem.Create(Id + 8, "TrackerHideVote", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Tracker]);
            TrackerAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 9, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Tracker])
                .SetValueFormat(OptionFormat.Times);
        }
        public static void Init()
        {
            playerIdList = new();
            TrackLimit = new();
            TrackerTarget = new();
            CanSeeLastRoomInMeeting = OptionCanSeeLastRoomInMeeting.GetBool();
            TempTrackLimit = new();
            IsEnable = false;
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            TrackLimit.Add(playerId, TrackLimitOpt.GetInt());
            TrackerTarget.Add(playerId, new List<byte>());
            IsEnable = true;
        }
        public static void SendRPC(byte trackerId = byte.MaxValue, byte targetId = byte.MaxValue)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetTrackerTarget, SendOption.Reliable, -1);
            writer.Write(trackerId);
            writer.Write(targetId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            byte trackerId = reader.ReadByte();
            byte targetId = reader.ReadByte();

            TrackLimit[trackerId]--;

            TrackerTarget[trackerId].Add(targetId);
            TargetArrow.Add(trackerId, targetId);

        }
        public static string GetTargetMark(PlayerControl seer, PlayerControl target) => !(seer == null || target == null) && TrackerTarget.ContainsKey(seer.PlayerId) && TrackerTarget[seer.PlayerId].Contains(target.PlayerId) ? Utils.ColorString(seer.GetRoleColor(), "◀") : "";

        public static void OnVote(PlayerControl player, PlayerControl target)
        {
            if (player == null || target == null) return;
            if (TrackLimit[player.PlayerId] < 1) return;
            if (player.PlayerId == target.PlayerId) return;
            if (TrackerTarget[player.PlayerId].Contains(target.PlayerId)) return;

            TrackLimit[player.PlayerId]--;

            TrackerTarget[player.PlayerId].Add(target.PlayerId);
            TargetArrow.Add(player.PlayerId, target.PlayerId);

            SendRPC(player.PlayerId, target.PlayerId);
        }

        public static void OnReportDeadBody()
        {
            if (!IsEnable) return;

            foreach (var trackerId in playerIdList) 
            {
                TempTrackLimit[trackerId] = TrackLimit[trackerId];
            }
        }

        public static string GetTrackerArrow(PlayerControl seer, PlayerControl target = null)
        {
            if (seer == null) return "";
            if (!seer.Is(CustomRoles.Tracker)) return "";
            if (target != null && seer.PlayerId != target.PlayerId) return "";
            if (!TrackerTarget.ContainsKey(seer.PlayerId)) return "";
            if (GameStates.IsMeeting) return "";

            var arrows = string.Empty;
            var targetList = TrackerTarget[seer.PlayerId];
            foreach (var trackTarget in targetList)
            {
                if (!TrackerTarget[seer.PlayerId].Contains(trackTarget)) return "";

                var targetData = Utils.GetPlayerById(trackTarget);

                var arrow = TargetArrow.GetArrows(seer, trackTarget);
                arrows += Utils.ColorString(CanGetColoredArrow.GetBool() ? Palette.PlayerColors[targetData.Data.DefaultOutfit.ColorId] : Color.white, arrow);
            }
            return arrows;
        }

        public static bool IsTrackTarget(PlayerControl seer, PlayerControl target)
            => seer.IsAlive() && playerIdList.Contains(seer.PlayerId)
                && TrackerTarget[seer.PlayerId].Contains(target.PlayerId)
                && target.IsAlive();

        public static string GetArrowAndLastRoom(PlayerControl seer, PlayerControl target)
        {
            string text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Tracker), TargetArrow.GetArrows(seer, target.PlayerId));
            var room = Main.PlayerStates[target.PlayerId].LastRoom;
            if (room == null) text += Utils.ColorString(Color.gray, "@" + GetString("FailToTrack"));
            else text += Utils.ColorString(Utils.GetRoleColor(CustomRoles.Tracker), "@" + GetString(room.RoomId.ToString()));
            return text;
        }
    }
}