using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Rebel : IAddon
{
    public CustomRoles Role => CustomRoles.Rebel;
    private const int Id = 31400;
    public AddonTypes Type => AddonTypes.Misc;

    public static OptionItem CanWinAfterDeath;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Rebel, canSetNum: true, tab: TabGroup.Addons);
        CanWinAfterDeath = BooleanOptionItem.Create(Id + 10, "CanWinAfterDeath", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rebel]);
    }

    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
}
