using static TOHE.Options;


namespace TOHE.Roles.AddOns.Common;

public static class Rebound
{
    private const int Id = 22300;

    public static OptionItem ImpCanBeRebound;
    public static OptionItem CrewCanBeRebound;
    public static OptionItem NeutralCanBeRebound;

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Rebound, canSetNum: true, tab: TabGroup.ModifierSettings);
        ImpCanBeRebound = BooleanOptionItem.Create(Id + 10, "ImpCanBeRebound", true, TabGroup.ModifierSettings, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rebound]);
        CrewCanBeRebound = BooleanOptionItem.Create(Id + 11, "CrewCanBeRebound", true, TabGroup.ModifierSettings, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rebound]);
        NeutralCanBeRebound = BooleanOptionItem.Create(Id + 12, "NeutralCanBeRebound", true, TabGroup.ModifierSettings, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rebound]);
    }
}