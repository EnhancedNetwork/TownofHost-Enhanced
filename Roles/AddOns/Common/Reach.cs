using Hazel;
using System.Collections.Generic;
using System.Linq;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.AddOns.Common;

public class Reach
{
    private static readonly int Id = 23700;

    public static CustomRoles IsReach = CustomRoles.Reach; // Used to find "references" of this addon.
    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Reach, canSetNum: true);
    }
}