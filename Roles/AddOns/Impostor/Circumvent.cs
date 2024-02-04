using System.Collections.Generic;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Impostor;
public static class Circumvent
{
    private static readonly int Id = 22600;
    // Holy shit, so revolutionary having a file for this 🙀🙀 // I do it for consistency tbh
    public static void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Circumvent, canSetNum: true, tab: TabGroup.Addons);
    }
}