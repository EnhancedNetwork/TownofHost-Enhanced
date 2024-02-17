using AmongUs.GameOptions;
using System.Linq;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common
{
    public static class Loyal
    {
        private static readonly int Id = 19400;

        public static OptionItem ImpCanBeLoyal;
        public static OptionItem CrewCanBeLoyal;
        public static void SetupCustomOptions()
        {
            SetupAdtRoleOptions(Id, CustomRoles.Loyal, canSetNum: true);
            ImpCanBeLoyal = BooleanOptionItem.Create(Id + 10, "ImpCanBeLoyal", true, TabGroup.Addons, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Loyal]);
            CrewCanBeLoyal = BooleanOptionItem.Create(Id + 11, "CrewCanBeLoyal", true, TabGroup.Addons, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Loyal]);
        }
    }
}