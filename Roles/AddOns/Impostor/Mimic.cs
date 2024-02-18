using TOHE.Roles.AddOns.Common;
using static TOHE.Options;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.AddOns.Impostor;
public static class Mimic
{
    private static readonly int Id = 23100;

    private static OptionItem CanSeeDeadRolesOpt;
    public static void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Mimic, canSetNum: true, tab: TabGroup.Addons);
        CanSeeDeadRolesOpt = BooleanOptionItem.Create(Id + 10, "MimicCanSeeDeadRoles", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mimic]);
    }

    public static bool CanSeeDeadRoles(PlayerControl seer, PlayerControl target) => CanSeeDeadRolesOpt.GetBool() && Main.VisibleTasksCount && seer.Is(CustomRoles.Mimic) && !target.IsAlive();
}