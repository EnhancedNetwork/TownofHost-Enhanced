using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Reach
{
    private static readonly int Id = 23700;

    public static CustomRoles IsReach = CustomRoles.Reach; // Used to find "references" of this addon.
    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Reach, canSetNum: true);
    }

    public static void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetInt(Int32OptionNames.KillDistance, 2);
    }
}