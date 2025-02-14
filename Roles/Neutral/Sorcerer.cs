using TOHE.Modules;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;
internal class Sorcerer : RoleBase
{
    // ----------- Role Setup ----------- //
    public override CustomRoles Role => CustomRoles.Sorcerer;
    private const int Id = 34000;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor; 
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralEvil;

    // ----------- Settings ----------- //
    private List<byte> markedPlayers = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Sorcerer);
    }

    public override void Init() 
    {
        markedPlayers.Clear();
    }

    public override bool CanUseKillButton(PlayerControl pc) => true;

    // Try to mark a player (with distance check)
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target) 
    {
        if (target == null) return true; 
        if (markedPlayers.Contains(target.PlayerId)) return true;

        if (markedPlayers.Count >= 2)
        {
            killer.RpcGuardAndKill(killer);
            return false;
        }

        killer.RpcGuardAndKill(killer);
        markedPlayers.Add(target.PlayerId);
        return false;
    }

    // Check if all marked players are dead
    private bool AreAllMarkedPlayersDead() 
    {
        if (markedPlayers == null) return false;
        if (markedPlayers.Count == 1) return false;
        foreach (var playerid in markedPlayers)
        {
            var player = Utils.GetPlayerById(playerid);
            if (!player.Data.IsDead) return false;
        }
        return true; // All are dead
    }

    // Check if only 3 players or fewer are alive
    private bool IsLastThreePlayersAlive()
    {
        int aliveCount = 0;
        foreach (var player in PlayerControl.AllPlayerControls) 
        {
            if (!player.Data.IsDead) 
            {
                aliveCount++;
            }
        }
        return aliveCount <= 3;
    }

    // Win: all marked players dead + 3 or fewer alive
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (lowLoad || !player.IsAlive()) return;

        if (AreAllMarkedPlayersDead() && IsLastThreePlayersAlive())
        {
            if (!CustomWinnerHolder.CheckForConvertedWinner(player.PlayerId))
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Sorcerer);
                CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
            }
        }
    }

}
