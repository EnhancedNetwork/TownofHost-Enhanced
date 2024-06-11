
namespace TOHE.Roles.AddOns.Common;

public static class Silent
{
    private const int Id = 26600;

    public static OptionItem CanBeOnCrew;
    public static OptionItem CanBeOnImp;
    public static OptionItem CanBeOnNeutral;

    public static void SetupCustomOptions()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Silent, canSetNum: true, tab: TabGroup.Addons);
        CanBeOnImp = BooleanOptionItem.Create("ImpCanBeSilent", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Silent]);
        CanBeOnCrew = BooleanOptionItem.Create("CrewCanBeSilent", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Silent]);
        CanBeOnNeutral = BooleanOptionItem.Create("NeutralCanBeSilent", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Silent]);
    }
}
