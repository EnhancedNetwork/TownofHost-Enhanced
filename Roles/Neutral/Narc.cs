using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Narc : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 32800;
    private static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();

    public override CustomRoles Role => CustomRoles.Narc;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralEvil;
    //==================================================================\\
    private bool AmnesiacReset = true;
    public override void Init()
    {
        PlayerIds.Clear();
    }
    public override void Add(byte playerId)
    {
        PlayerIds.Add(playerId);
    }
    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Narc);
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        var allAlivePlayers = Main.AllAlivePlayerControls;
        int impnum = allAlivePlayers.Count(pc => pc.Is(Custom_Team.Impostor));
        if (target.GetCustomRole().IsImpostor())
        {
            Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Arrested;
            killer.RpcGuardAndKill(target);
            target.RpcExileV2();
            Main.PlayerStates[target.PlayerId].SetDead();
            target.Data.IsDead = true;
            target.SetRealKiller(killer);
            killer.SetKillCooldown();
            if (impnum == 0)
            {
                AmnesiacReset = false;
                if (!CustomWinnerHolder.CheckForConvertedWinner(killer.PlayerId))
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Narc);
                    CustomWinnerHolder.WinnerIds.Add(killer.PlayerId);
                }
            }
            return false;
        }
        if (target.GetCustomRole().IsCrewmate())
        {
            killer.RpcSetCustomRole(CustomRoles.Amnesiac);
            return true;
        }
        if (target.GetCustomRole().IsNeutral() || target.Is(CustomRoles.Madmate) || target.GetCustomRole().IsConverted())
        {
            killer.RpcSetCustomRole(CustomRoles.Amnesiac);
            return true;
        }
        return true;
    }
    public override void OnFixedUpdateLowLoad(PlayerControl player)
    {
        var allAlivePlayers = Main.AllAlivePlayerControls;
        int impnum = allAlivePlayers.Count(pc => pc.Is(Custom_Team.Impostor));
        if (!player.IsAlive()) return;
        if (impnum == 0)
        {
            if (AmnesiacReset == true)
            {
                if (player.Is(CustomRoles.Narc))
                {
                    player.RpcSetCustomRole(CustomRoles.Amnesiac);
                }
            }
        }
    }
    public override bool CanUseKillButton(PlayerControl pc) => true;
}
