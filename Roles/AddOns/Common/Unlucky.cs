using System.Collections.Generic;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common
{
    public static class Unlucky
    {
        private static readonly int Id = 20200;

        public static OptionItem UnluckyTaskSuicideChance;
        public static OptionItem UnluckyKillSuicideChance;
        public static OptionItem UnluckyVentSuicideChance;
        public static OptionItem UnluckyReportSuicideChance;
        public static OptionItem UnluckySabotageSuicideChance;
        public static OptionItem ImpCanBeUnlucky;
        public static OptionItem CrewCanBeUnlucky;
        public static OptionItem NeutralCanBeUnlucky;

        public static void SetupCustomOptions()
        {
            SetupAdtRoleOptions(Id, CustomRoles.Unlucky, canSetNum: true);
            UnluckyKillSuicideChance = IntegerOptionItem.Create(Id + 10, "UnluckyKillSuicideChance", new(0, 100, 1), 2, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Unlucky])
                .SetValueFormat(OptionFormat.Percent);
            UnluckyTaskSuicideChance = IntegerOptionItem.Create(Id + 11, "UnluckyTaskSuicideChance", new(0, 100, 1), 5, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Unlucky])
                .SetValueFormat(OptionFormat.Percent);
            UnluckyVentSuicideChance = IntegerOptionItem.Create(Id + 12, "UnluckyVentSuicideChance", new(0, 100, 1), 3, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Unlucky])
                .SetValueFormat(OptionFormat.Percent);
            UnluckyReportSuicideChance = IntegerOptionItem.Create(Id + 13, "UnluckyReportSuicideChance", new(0, 100, 1), 1, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Unlucky])
                .SetValueFormat(OptionFormat.Percent);
            UnluckySabotageSuicideChance = IntegerOptionItem.Create(Id + 14, "UnluckySabotageSuicideChance", new(0, 100, 1), 4, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Unlucky])
                .SetValueFormat(OptionFormat.Percent);
            ImpCanBeUnlucky = BooleanOptionItem.Create(Id + 15, "ImpCanBeUnlucky", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Unlucky]);
            CrewCanBeUnlucky = BooleanOptionItem.Create(Id + 16, "CrewCanBeUnlucky", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Unlucky]);
            NeutralCanBeUnlucky = BooleanOptionItem.Create(Id + 17, "NeutralCanBeUnlucky", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Unlucky]);
        }

        public static void SuicideRand(PlayerControl victim)
        {
            var Ue = IRandom.Instance;
            if (Ue.Next(1, 100) <= UnluckyTaskSuicideChance.GetInt())
            {
                Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Suicide;
                victim.RpcMurderPlayerV3(victim);
            }
        }
    }
}