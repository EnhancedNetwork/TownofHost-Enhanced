﻿using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Necroview : IAddon
{
    public CustomRoles Role => CustomRoles.Necroview;
    private const int Id = 19600;
    public AddonTypes Type => AddonTypes.Helpful;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Necroview, canSetNum: true, tab: TabGroup.Addons, teamSpawnOptions: true);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }

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
                return "7f8c8d";
        }

        if ((customRole.IsImpostorTeamV2() || customRole.IsMadmate() || target.Is(CustomRoles.Madmate)) && !target.Is(CustomRoles.Admired) && !target.Is(CustomRoles.Narc))
        {
            return "ff1919";
        }

        if (customRole.IsCrewmate() || target.Is(CustomRoles.Admired) || target.Is(CustomRoles.Narc))
        {
            return "00ffff";
        }

        return "7f8c8d";
    }
}

