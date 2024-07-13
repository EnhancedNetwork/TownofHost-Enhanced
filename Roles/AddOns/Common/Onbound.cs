using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Onbound
{
    private const int Id = 25800;

    public static OptionItem ImpCanBeOnbound;
    public static OptionItem CrewCanBeOnbound;
    public static OptionItem NeutralCanBeOnbound;

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Onbound, canSetNum: true, tab: TabGroup.ModifierSettings);
        ImpCanBeOnbound = BooleanOptionItem.Create(Id + 10, "ImpCanBeOnbound", true, TabGroup.ModifierSettings, false).SetParent(CustomRoleSpawnChances[CustomRoles.Onbound]);
        CrewCanBeOnbound = BooleanOptionItem.Create(Id + 11, "CrewCanBeOnbound", true, TabGroup.ModifierSettings, false).SetParent(CustomRoleSpawnChances[CustomRoles.Onbound]);
        NeutralCanBeOnbound = BooleanOptionItem.Create(Id + 12, "NeutralCanBeOnbound", true, TabGroup.ModifierSettings, false).SetParent(CustomRoleSpawnChances[CustomRoles.Onbound]);
    }
}

