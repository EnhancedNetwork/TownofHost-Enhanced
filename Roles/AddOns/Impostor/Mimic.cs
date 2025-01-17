using static TOHE.Options;

namespace TOHE.Roles.AddOns.Impostor;
public class Mimic : IAddon
{
    public CustomRoles Role => CustomRoles.Mimic;
    private const int Id = 23100;
    public AddonTypes Type => AddonTypes.Impostor;

    private static OptionItem CanSeeDeadRolesOpt;
    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Mimic, canSetNum: true, tab: TabGroup.Addons);
        CanSeeDeadRolesOpt = BooleanOptionItem.Create(Id + 10, "MimicCanSeeDeadRoles", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mimic]);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
    public static bool CanSeeDeadRoles(PlayerControl seer, PlayerControl target) => seer.Is(CustomRoles.Mimic) && CanSeeDeadRolesOpt.GetBool() && Main.VisibleTasksCount && !target.IsAlive();
}
