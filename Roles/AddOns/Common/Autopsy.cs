using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Autopsy : IAddon
{
    private const int Id = 18600;
    public AddonTypes Type => AddonTypes.Helpful;
    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Autopsy, canSetNum: true, teamSpawnOptions: true);
    }
}