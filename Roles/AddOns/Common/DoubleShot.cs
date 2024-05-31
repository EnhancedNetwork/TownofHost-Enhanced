using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class DoubleShot
{
    public static HashSet<byte> IsActive = [];

    public static OptionItem ImpCanBeDoubleShot;
    public static OptionItem CrewCanBeDoubleShot;
    public static OptionItem NeutralCanBeDoubleShot;

    public static void SetupCustomOption()
    {
        SetupAdtRoleOptions(19200, CustomRoles.DoubleShot, canSetNum: true, tab: TabGroup.Addons);
        ImpCanBeDoubleShot = BooleanOptionItem.Create("ImpCanBeDoubleShot", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.DoubleShot]);
        CrewCanBeDoubleShot = BooleanOptionItem.Create("CrewCanBeDoubleShot", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.DoubleShot]);
        NeutralCanBeDoubleShot = BooleanOptionItem.Create("NeutralCanBeDoubleShot", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.DoubleShot]);
    }
    public static void Init()
    {
        IsActive = [];
    }
}