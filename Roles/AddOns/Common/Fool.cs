using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Fool
{
    private static readonly int Id = 25600;

    public static OptionItem ImpCanBeFool;
    public static OptionItem CrewCanBeFool;
    public static OptionItem NeutralCanBeFool;

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Fool, canSetNum: true, tab: TabGroup.Addons);
        ImpCanBeFool = BooleanOptionItem.Create(Id + 10, "ImpCanBeFool", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fool]);
        CrewCanBeFool = BooleanOptionItem.Create(Id + 11, "CrewCanBeFool", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fool]);
        NeutralCanBeFool = BooleanOptionItem.Create(Id + 12, "NeutralCanBeFool", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fool]);
    }


}

