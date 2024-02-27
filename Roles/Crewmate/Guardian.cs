using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate
{
    internal class Guardian : RoleBase
    {
        public const int Id = 11700;
        public static bool On = false;
        public override bool IsEnable => On;
        public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;


        public static OverrideTasksData GuardianTasks;

        public static void SetupCustomOptions()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Guardian);
            GuardianTasks = OverrideTasksData.Create(Id + 10, TabGroup.CrewmateRoles, CustomRoles.Guardian);
        }

        public override void Init()
        {
            On = false;
        }

        public override void Add(byte playerId)
        {
            On = true;
        }

        public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl guesser)
        {

            if (target.Is(CustomRoles.Guardian) && target.GetPlayerTaskState().IsTaskFinished)
            {
                if (!isUI) Utils.SendMessage(GetString("GuessGuardianTask"), guesser.PlayerId);
                else guesser.ShowPopUp(GetString("GuessGuardianTask"));
                return true;
            }
            return false;
        }
    }
}
