using Hazel;
using System.Collections.Generic;
using System.Linq;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.AddOns.Common;

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
        IsEnable = true;
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


}