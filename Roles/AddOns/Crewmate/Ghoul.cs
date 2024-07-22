using static TOHE.Options;

namespace TOHE.Roles.AddOns.Crewmate;

public class Ghoul
{
    private const int Id = 21900;
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
            if (!player.Is(CustomRoles.Pestilence))
                KillGhoul.Add(player.PlayerId);
        }
    }

    public static void OnTaskComplete(PlayerControl player)
    {
        if (player.IsAlive())
        {
            _ = new LateTask(() =>
            {
                player.SetDeathReason(PlayerState.DeathReason.Suicide);
                player.RpcMurderPlayer(player);

            }, 0.2f, "Ghoul Suicide");
        }
        else
        {
            foreach (var killer in Main.AllAlivePlayerControls.Where(x => KillGhoul.Contains(x.PlayerId)))
            {
                killer.SetDeathReason(PlayerState.DeathReason.Kill);
                player.RpcMurderPlayer(killer);
            }
        }
    }
}
