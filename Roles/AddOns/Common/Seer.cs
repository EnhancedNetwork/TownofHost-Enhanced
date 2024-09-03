
namespace TOHE.Roles.AddOns.Common;

public class Seer : IAddon
{
    private const int Id = 20000;
    public AddonTypes Type => AddonTypes.Helpful;

    public void SetupCustomOption()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Seer, canSetNum: true, teamSpawnOptions: true);
    }
}
