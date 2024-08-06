using static TOHE.Options;


namespace TOHE.Roles.AddOns.Common;

public class Rebound : IAddon
{
    private const int Id = 22300;
    public AddonTypes Type => AddonTypes.Guesser;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Rebound, canSetNum: true, tab: TabGroup.Addons, teamSpawnOptions: true);
    }
}