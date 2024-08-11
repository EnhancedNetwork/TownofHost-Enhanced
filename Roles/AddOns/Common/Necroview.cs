﻿using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Necroview : IAddon
{
    private const int Id = 19600;
    public AddonTypes Type => AddonTypes.Helpful;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Necroview, canSetNum: true, tab: TabGroup.Addons, teamSpawnOptions: true);
    }

    public static string NameColorOptions(PlayerControl target)
    {
        var customRole = target.GetCustomRole();

        foreach (var SubRole in target.GetCustomSubRoles())
        {
            if (SubRole is CustomRoles.Charmed
                or CustomRoles.Infected
                or CustomRoles.Contagious
                or CustomRoles.Egoist
                or CustomRoles.Recruit
                or CustomRoles.Soulless)
                return Main.roleColors[CustomRoles.Knight];
        }

        if (customRole.IsImpostorTeamV2() || customRole.IsMadmate())
        {
            return Main.roleColors[CustomRoles.Impostor];
        }

        if (customRole.IsCrewmate())
        {
            return Main.roleColors[CustomRoles.Bait];
        }

        return Main.roleColors[CustomRoles.Knight];
    }
}

