using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Reach : IAddon
{
    private const int Id = 23700;
    public AddonTypes Type => AddonTypes.Helpful;
    public static CustomRoles IsReach => CustomRoles.Reach; // Used to find "references" of this addon.
    
    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Reach, canSetNum: true);
    }
    public static void ApplyGameOptions(IGameOptions opt) => opt.SetInt(Int32OptionNames.KillDistance, 2);
}