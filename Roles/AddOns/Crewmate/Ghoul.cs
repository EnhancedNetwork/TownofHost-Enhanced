using static TOHE.Options;

namespace TOHE.Roles.AddOns.Crewmate;

public class Ghoul : IAddon
{
    public CustomRoles Role => CustomRoles.Ghoul;
    private const int Id = 21900;
    public AddonTypes Type => AddonTypes.Mixed;

    public static bool IsEnable;
    public static readonly HashSet<byte> KillGhoul = [];

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Ghoul, canSetNum: true, tab: TabGroup.Addons);
    }

    public void Init()
    {
        KillGhoul.Clear();
        IsEnable = false;
    }
    public void Add(byte playerId, bool gameIsLoading = true)
    {
        IsEnable = true;
    }
    public void Remove(byte playerId)
    {
        if (!Main.AllPlayerControls.Any(x => x.Is(CustomRoles.Ghoul)))
            IsEnable = false;
    }

    public static void ApplyGameOptions(PlayerControl player)
    {
        if (Main.AllPlayerControls.Any(x => x.Is(CustomRoles.Ghoul) && !x.IsAlive() && x.GetRealKiller()?.PlayerId == player.PlayerId))
        {
            if (!player.IsTransformedNeutralApocalypse())
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
            var killer = player.GetRealKiller();

            if (killer != null && !killer.IsTransformedNeutralApocalypse())
            {
                killer.SetDeathReason(PlayerState.DeathReason.Kill);
                player.RpcMurderPlayer(killer);
            }
        }
    }
}
