using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Loyal : IAddon
{
    private const int Id = 19400;
    public AddonTypes Type => AddonTypes.Helpful;

    public static OptionItem ImpCanBeLoyal;
    public static OptionItem CrewCanBeLoyal;
    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Loyal, canSetNum: true);
        ImpCanBeLoyal = BooleanOptionItem.Create(Id + 10, "ImpCanBeLoyal", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Loyal]);
        CrewCanBeLoyal = BooleanOptionItem.Create(Id + 11, "CrewCanBeLoyal", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Loyal]);
    }
}