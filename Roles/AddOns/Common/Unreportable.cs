using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Unreportable : IAddon
{
    private const int Id = 20500;
    public AddonTypes Type => AddonTypes.Harmful;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Unreportable, canSetNum: true, teamSpawnOptions: true);
    }
}