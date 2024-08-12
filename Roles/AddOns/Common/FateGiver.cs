using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class FateGiver : IAddon
{
    private const int Id = 29000;
    public AddonTypes Type => AddonTypes.Misc;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.FateGiver, canSetNum: true, tab: TabGroup.Addons);
    }
}