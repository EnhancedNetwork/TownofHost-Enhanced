using System;
using System.Collections.Generic;
using System.Linq;
using static TOHE.Options;
using static TOHE.MeetingHudStartPatch;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate
{
    internal class Detective : RoleBase
    {
        private const int Id = 7900;
        public static bool On = false;
        public override bool IsEnable => On;
        public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;

        public static OptionItem DetectiveCanknowKiller;

        public static Dictionary<byte, string> DetectiveNotify = [];

        public static void SetupCustomOptions()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Detective);
            DetectiveCanknowKiller = BooleanOptionItem.Create(7902, "DetectiveCanknowKiller", true, TabGroup.CrewmateRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Detective]);
        }

        public override void Init()
        {
            DetectiveNotify = [];
            On = false;
        }

        public override void Add(byte playerId)
        {
            On = true;
        }
        public override void OnReportDeadBody(PlayerControl player, PlayerControl target)
        {
            var tpc = target;
            if (player.Is(CustomRoles.Detective) && player.PlayerId != target.PlayerId)
            {
                string msg;
                msg = string.Format(GetString("DetectiveNoticeVictim"), tpc.GetRealName(), tpc.GetDisplayRoleAndSubName(tpc, false));
                if (DetectiveCanknowKiller.GetBool())
                {
                    var realKiller = tpc.GetRealKiller();
                    if (realKiller == null) msg += "；" + GetString("DetectiveNoticeKillerNotFound");
                    else msg += "；" + string.Format(GetString("DetectiveNoticeKiller"), realKiller.GetDisplayRoleAndSubName(realKiller, false));
                }
                DetectiveNotify.Add(player.PlayerId, msg);
            }
        }

        public override void OnMeetingHudStart(PlayerControl pc)
        {
            if (DetectiveNotify.ContainsKey(pc.PlayerId))
                AddMsg(DetectiveNotify[pc.PlayerId], pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Detective), GetString("DetectiveNoticeTitle")));
        }
        public override void MeetingHudClear() => DetectiveNotify = [];
    }
}
