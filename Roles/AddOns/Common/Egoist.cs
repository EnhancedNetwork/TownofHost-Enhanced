using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Egoist
{
    private const int Id = 23500;

    public static OptionItem CrewCanBeEgoist;
    public static OptionItem ImpCanBeEgoist;
    public static OptionItem ImpEgoistVisibalToAllies;
    public static OptionItem EgoistCountAsConverted;

    public static void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Egoist, canSetNum: true, tab: TabGroup.Addons);
        CrewCanBeEgoist = BooleanOptionItem.Create("CrewCanBeEgoist", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Egoist]);
        ImpCanBeEgoist = BooleanOptionItem.Create("ImpCanBeEgoist", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Egoist]);
        ImpEgoistVisibalToAllies = BooleanOptionItem.Create("ImpEgoistVisibalToAllies", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Egoist]);
        EgoistCountAsConverted = BooleanOptionItem.Create("EgoistCountAsConverted", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Egoist]);
    }
}