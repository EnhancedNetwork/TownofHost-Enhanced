using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Onbound
{
    private static readonly int Id = 25800;

    public static OptionItem ImpCanBeOnbound;
    public static OptionItem CrewCanBeOnbound;
    public static OptionItem NeutralCanBeOnbound;

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Onbound, canSetNum: true, tab: TabGroup.TaskSettings);
        ImpCanBeOnbound = BooleanOptionItem.Create(Id + 10, "ImpCanBeOnbound", true, TabGroup.TaskSettings, false).SetParent(CustomRoleSpawnChances[CustomRoles.Onbound]);
        CrewCanBeOnbound = BooleanOptionItem.Create(Id + 11, "CrewCanBeOnbound", true, TabGroup.TaskSettings, false).SetParent(CustomRoleSpawnChances[CustomRoles.Onbound]);
        NeutralCanBeOnbound = BooleanOptionItem.Create(Id + 12, "NeutralCanBeOnbound", true, TabGroup.TaskSettings, false).SetParent(CustomRoleSpawnChances[CustomRoles.Onbound]);
    }
}

