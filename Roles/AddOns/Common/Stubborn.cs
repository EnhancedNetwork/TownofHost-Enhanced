using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Stubborn : IAddon
{
    private const int Id = 22500;
    public AddonTypes Type => AddonTypes.Mixed;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Stubborn, canSetNum: true, teamSpawnOptions: true);
    }
}

