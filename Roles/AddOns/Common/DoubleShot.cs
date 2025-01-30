using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class DoubleShot : IAddon
{
    public CustomRoles Role => CustomRoles.DoubleShot;
    public static readonly HashSet<byte> IsActive = [];
    public AddonTypes Type => AddonTypes.Guesser;


    public static OptionItem ImpCanBeDoubleShot;
    public static OptionItem CrewCanBeDoubleShot;
    public static OptionItem NeutralCanBeDoubleShot;
    public static OptionItem CovenCanBeDoubleShot;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(19200, CustomRoles.DoubleShot, canSetNum: true, tab: TabGroup.Addons);
        ImpCanBeDoubleShot = BooleanOptionItem.Create(19210, "ImpCanBeDoubleShot", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.DoubleShot]);
        CrewCanBeDoubleShot = BooleanOptionItem.Create(19211, "CrewCanBeDoubleShot", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.DoubleShot]);
        NeutralCanBeDoubleShot = BooleanOptionItem.Create(19212, "NeutralCanBeDoubleShot", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.DoubleShot]);
        CovenCanBeDoubleShot = BooleanOptionItem.Create(19213, "CovenCanBeDoubleShot", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.DoubleShot]);
    }
    public void Init()
    {
        IsActive.Clear();
    }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
}
