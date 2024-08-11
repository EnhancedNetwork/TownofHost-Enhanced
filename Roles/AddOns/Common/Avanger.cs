﻿using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Neutral;
using static TOHE.Options;


namespace TOHE.Roles.AddOns.Common;

public class Avanger : IAddon
{
    private const int Id = 21500;
    public AddonTypes Type => AddonTypes.Mixed;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Avanger, canSetNum: true, teamSpawnOptions: true);
    }

    public static void OnMurderPlayer(PlayerControl target)
    {
        var pcList = Main.AllAlivePlayerControls.Where(pc => pc.PlayerId != target.PlayerId && !Pelican.IsEaten(pc.PlayerId) && !Medic.ProtectList.Contains(pc.PlayerId)
            && !pc.Is(CustomRoles.Pestilence) && !pc.Is(CustomRoles.Necromancer) && !pc.Is(CustomRoles.PunchingBag) && !pc.Is(CustomRoles.Solsticer) && !((pc.Is(CustomRoles.NiceMini) || pc.Is(CustomRoles.EvilMini)) && Mini.Age < 18)).ToList();
        
        if (pcList.Any())
        {
            PlayerControl rp = pcList.RandomElement();
            rp.SetDeathReason(PlayerState.DeathReason.Revenge);
            rp.RpcMurderPlayer(rp);
            rp.SetRealKiller(target);
        }
    }
}

