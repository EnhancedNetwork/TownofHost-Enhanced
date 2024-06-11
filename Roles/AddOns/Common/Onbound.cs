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
        SetupAdtRoleOptions(Id, CustomRoles.Onbound, canSetNum: true, tab: TabGroup.TaskSettings);
        ImpCanBeOnbound = BooleanOptionItem.Create("ImpCanBeOnbound", true, TabGroup.TaskSettings, false).SetParent(CustomRoleSpawnChances[CustomRoles.Onbound]);
        CrewCanBeOnbound = BooleanOptionItem.Create("CrewCanBeOnbound", true, TabGroup.TaskSettings, false).SetParent(CustomRoleSpawnChances[CustomRoles.Onbound]);
        NeutralCanBeOnbound = BooleanOptionItem.Create("NeutralCanBeOnbound", true, TabGroup.TaskSettings, false).SetParent(CustomRoleSpawnChances[CustomRoles.Onbound]);
    }
}

