using System;
using static TOHE.Translator;

namespace TOHE.Roles.AddOns.Common
{
    public class Susceptible
    {
        private static readonly int Id = 27000;
        public static OptionItem CanBeOnCrew;
        public static OptionItem CanBeOnImp;
        public static OptionItem CanBeOnNeutral;
       

        public static void SetupCustomOptions()
        {
            Options.SetupAdtRoleOptions(Id, CustomRoles.Susceptible, canSetNum: true, tab: TabGroup.Addons);
            CanBeOnImp = BooleanOptionItem.Create(Id + 11, "ImpCanBeSusceptible", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Susceptible]);
            CanBeOnCrew = BooleanOptionItem.Create(Id + 12, "CrewCanBeSusceptible", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Susceptible]);
            CanBeOnNeutral = BooleanOptionItem.Create(Id + 13, "NeutralCanBeSusceptible", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Susceptible]);
        }

         public static void ChangeRandomDeath(PlayerControl victim)
        {
            PlayerState.DeathReason[] deathReasons = (PlayerState.DeathReason[])Enum.GetValues(typeof(PlayerState.DeathReason));
            Random random = new Random();
            int randomIndex = random.Next(deathReasons.Length);
            PlayerState.DeathReason randomReason = deathReasons[randomIndex];
            Logger.Info($"{victim.GetNameWithRole()} had the deathreason {randomReason}", "Susceptible");

            while (Main.PlayerStates[victim.PlayerId].deathReason != randomReason) 
            {
                Main.PlayerStates[victim.PlayerId].deathReason = randomReason;
            }
        }
    }
}
