using static TOHE.Options;

namespace TOHE.Roles.AddOns.Impostor;
public static class Mimic
{
    private static readonly int Id = 23100;

    public static OptionItem MimicCanSeeDeadRoles;
    public static void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Mimic, canSetNum: true, tab: TabGroup.Addons);
        MimicCanSeeDeadRoles = BooleanOptionItem.Create(Id + 10, "MimicCanSeeDeadRoles", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mimic]);
    }
}