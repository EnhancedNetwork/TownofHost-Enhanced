﻿using TOHE.Roles.Crewmate;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Crewmate;

public class Bloodthirst : IAddon
{
    private const int Id = 21700;
    public AddonTypes Type => AddonTypes.Mixed;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Bloodthirst, canSetNum: true);
    }

    public static void Add()
    {
        Alchemist.AddBloodlus();
    }

    public static void OnTaskComplete(PlayerControl player)
    {
        if (Alchemist.BloodthirstList.ContainsKey(player.PlayerId)) return;

        Alchemist.BloodthirstList[player.PlayerId] = player.PlayerId;
        player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Bloodthirst), Translator.GetString("BloodthirstAdded")));
    }
}
