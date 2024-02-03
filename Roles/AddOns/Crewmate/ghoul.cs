using Hazel;
using System.Collections.Generic;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.AddOns.Common;

public class Ghoul
{
    private static readonly int Id = 21900;
    public static HashSet<byte> KillGhoul = [];
    public static void Init()
    {
        KillGhoul = [];
    }
    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Ghoul, canSetNum: true, tab: TabGroup.Addons);
    }
}