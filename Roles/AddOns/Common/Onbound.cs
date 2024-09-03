using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Onbound : IAddon
{
    private const int Id = 25800;
    public AddonTypes Type => AddonTypes.Guesser;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Onbound, canSetNum: true, tab: TabGroup.Addons, teamSpawnOptions: true);
    }
}

