using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TOHE.Options;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate
{
    internal class Lookout : RoleBase
    {
        public const int Id = 11800;
        public static bool On = false;
        public override bool IsEnable => On;
        public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;

        public static void SetupCustomOptions()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Lookout);
        }
        
        public override void Init()
        {
            On = false;
        }
        public override void Add(byte playerId)
        {
            On = true;
        }

        public override string NotifyRoleMark(PlayerControl seer, PlayerControl seen = null, string TargetPlayerName = "", bool isForMeeting = false)
        {
            if (!seer.IsAlive() || !seen.IsAlive()) return string.Empty;
            return ColorString(GetRoleColor(CustomRoles.Lookout), " " + seen.PlayerId.ToString()) + " " + TargetPlayerName;
        }


    }
}
