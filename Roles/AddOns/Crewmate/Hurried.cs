using System.Collections.Generic;

namespace TOHE.Roles.AddOns.Crewmate
{
    public static class Hurried
    {
        private static readonly int Id = 21300;

        public static OptionItem CanBeOnMadMate;
        public static OptionItem CanBeOnTaskBasedCrew;
        public static OptionItem CanBeConverted;

        public static void SetupCustomOption()
        {
            Options.SetupAdtRoleOptions(Id, CustomRoles.Hurried, canSetNum: true);
            CanBeOnMadMate = BooleanOptionItem.Create(Id + 11, "MadmateCanBeHurried", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Hurried]);
            CanBeOnTaskBasedCrew = BooleanOptionItem.Create(Id + 12, "TaskBasedCrewCanBeHurried", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Hurried]);
            CanBeConverted = BooleanOptionItem.Create(Id + 13, "HurriedCanBeConverted", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Hurried]);
        }

        public static bool CheckWinState(PlayerControl pc)
        {
            if (pc == null) return false;
            if (!pc.Is(CustomRoles.Hurried)) return true;
            if (!pc.GetCustomRole().IsCrewmate() && !pc.Is(CustomRoles.Madmate)) return true;

            if (pc.GetCustomRole().IsTasklessCrewmate()) return true;
            if (pc.Is(CustomRoles.Madmate) && !CanBeOnMadMate.GetBool()) return true;
            if (pc.GetCustomRole().IsTaskBasedCrewmate() && !CanBeOnTaskBasedCrew.GetBool()) return true;

            var taskState = pc.GetPlayerTaskState();
            if (!taskState.hasTasks || taskState.IsTaskFinished) return true;

            foreach (var role in pc.GetCustomSubRoles())
            {
                if (!role.IsConverted()) continue;
                if (!CanBeConverted.GetBool()) return true;
            }
            return false;
        }
        //Hard to check specific player, loop check all player
    }
}
