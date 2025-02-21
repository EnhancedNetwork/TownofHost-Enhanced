
namespace TOHE.Roles.AddOns.Common;

public class Silent : IAddon
{
    public CustomRoles Role => CustomRoles.Silent;
    private const int Id = 26600;
    public AddonTypes Type => AddonTypes.Helpful;
    public void SetupCustomOption()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Silent, canSetNum: true, tab: TabGroup.Addons, teamSpawnOptions: true);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
}
