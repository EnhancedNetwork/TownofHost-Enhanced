using Hazel;
using System.Collections.Generic;
using System.Linq;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.AddOns.Crewmate;

public class Ghoul
{
    private static readonly int Id = 21900;
    public static HashSet<byte> KillGhoul = [];
    public static bool IsEnable;
    
    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Ghoul, canSetNum: true, tab: TabGroup.Addons);
    }

    public static void Init()
    {
        KillGhoul = [];
        IsEnable = false;
    }

    public static void Add()
    {
        IsEnable = true;
    }

    public static void ApplyGameOptions(PlayerControl player) 
    {
        if (Main.AllPlayerControls.Any(x => x.Is(CustomRoles.Ghoul) && !x.IsAlive() && x.GetRealKiller()?.PlayerId == player.PlayerId))
        {
            var pc = Utils.GetPlayerById(player.PlayerId);
            if (pc != null)
            {
                if (!pc.Is(CustomRoles.Pestilence))
                    Ghoul.KillGhoul.Add(player.PlayerId);
            }

        }
    }

    public static void OnTaskComplete(PlayerControl player)
    {
        if (player.IsAlive())
        {
            _ = new LateTask(() =>
            {
                Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Suicide;
                player.RpcMurderPlayerV3(player);

            }, 0.2f, "Ghoul Suicide");
        }
        else
        {
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => KillGhoul.Contains(x.PlayerId)))
            {
                Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                player.RpcMurderPlayerV3(pc);
            }
        }
    }


}