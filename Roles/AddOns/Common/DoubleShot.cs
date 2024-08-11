using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class DoubleShot : IAddon
{
    public static HashSet<byte> IsActive = [];
    public AddonTypes Type => AddonTypes.Guesser;


    public static OptionItem ImpCanBeDoubleShot;
    public static OptionItem CrewCanBeDoubleShot;
    public static OptionItem NeutralCanBeDoubleShot;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(19200, CustomRoles.DoubleShot, canSetNum: true, tab: TabGroup.Addons, teamSpawnOptions: true);
        ImpCanBeDoubleShot = BooleanOptionItem.Create(19210, "ImpCanBeDoubleShot", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.DoubleShot]);
        CrewCanBeDoubleShot = BooleanOptionItem.Create(19211, "CrewCanBeDoubleShot", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.DoubleShot]);
        NeutralCanBeDoubleShot = BooleanOptionItem.Create(19212, "NeutralCanBeDoubleShot", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.DoubleShot]);
    }
    public static void Init()
    {
        IsActive = [];
    }
}