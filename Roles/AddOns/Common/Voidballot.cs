using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class VoidBallot : IAddon
{
    private const int Id = 21100;
    public AddonTypes Type => AddonTypes.Harmful;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.VoidBallot, canSetNum: true, teamSpawnOptions: true);
    }
}