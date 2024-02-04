using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

    public static class Unreportable 
    { 
        private static readonly int Id = 20500;

        public static OptionItem ImpCanBeUnreportable;
        public static OptionItem CrewCanBeUnreportable;
        public static OptionItem NeutralCanBeUnreportable;

        public static void SetupCustomOptions()
        {
            SetupAdtRoleOptions(Id, CustomRoles.Unreportable, canSetNum: true);
            ImpCanBeUnreportable = BooleanOptionItem.Create(Id + 10, "ImpCanBeUnreportable", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Unreportable]);
            CrewCanBeUnreportable = BooleanOptionItem.Create(Id + 11, "CrewCanBeUnreportable", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Unreportable]);
            NeutralCanBeUnreportable = BooleanOptionItem.Create(Id + 12, "NeutralCanBeUnreportable", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Unreportable]);
        }


    }

