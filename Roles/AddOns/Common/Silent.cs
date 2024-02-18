
namespace TOHE.Roles.AddOns.Common;

public static class Silent
{
    private static readonly int Id = 26600;

    public static OptionItem CanBeOnCrew;
    public static OptionItem CanBeOnImp;
    public static OptionItem CanBeOnNeutral;

    public static void SetupCustomOptions()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Silent, canSetNum: true, tab: TabGroup.Addons);
        CanBeOnImp = BooleanOptionItem.Create(Id + 11, "ImpCanBeSilent", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Silent]);
        CanBeOnCrew = BooleanOptionItem.Create(Id + 12, "CrewCanBeSilent", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Silent]);
        CanBeOnNeutral = BooleanOptionItem.Create(Id + 13, "NeutralCanBeSilent", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Silent]);
    }
}
