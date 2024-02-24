using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate
{
    internal class Guardian : RoleBase
    {
        public const int Id = 11700;

        public static OverrideTasksData GuardianTasks;
        public static bool On = false;

        public override bool IsEnable => On;

        public override void Init()
        {
            On = false;
        }
        public override void Add(byte playerId)
        {
            On = true;
        }
        public static void SetupCustomOptions()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Guardian);
            GuardianTasks = OverrideTasksData.Create(Id + 10, TabGroup.CrewmateRoles, CustomRoles.Guardian);
        }

    }
}
