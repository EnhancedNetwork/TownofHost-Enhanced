
namespace TOHE.Roles.AddOns.Common;

public class Silent : IAddon
{
    private const int Id = 26600;
    public AddonTypes Type => AddonTypes.Helpful;
    public void SetupCustomOption()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Silent, canSetNum: true, tab: TabGroup.Addons, teamSpawnOptions: true);
    }
}
