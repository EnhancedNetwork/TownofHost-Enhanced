using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Overclocked
{
    private static readonly int Id = 19800;

    public static OptionItem OverclockedReduction;

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Overclocked, canSetNum: true);
        OverclockedReduction = FloatOptionItem.Create(Id + 10, "OverclockedReduction", new(0f, 90f, 5f), 40f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Overclocked])
            .SetValueFormat(OptionFormat.Percent);
    }
}

