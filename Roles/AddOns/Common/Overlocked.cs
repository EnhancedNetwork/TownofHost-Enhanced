using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Overclocked : IAddon
{
    private const int Id = 19800;
    public AddonTypes Type => AddonTypes.Helpful;

    public static OptionItem OverclockedReduction;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Overclocked, canSetNum: true);
        OverclockedReduction = FloatOptionItem.Create(Id + 10, "OverclockedReduction", new(0f, 90f, 5f), 40f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Overclocked])
            .SetValueFormat(OptionFormat.Percent);
    }
}

