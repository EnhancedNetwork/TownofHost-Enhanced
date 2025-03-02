using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Sorcerer : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Sorcerer;
    private const int Id = 34000;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor; 
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private bool usedSecondChance = false; // Checking if respawn has been used
    private List<PlayerControl> markedPlayers = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Sorcerer);
    }

    public override void Init() 
    {
        markedPlayers.Clear(); 
        usedSecondChance = false; // Reset second chance
    }

    // Try to mark Player (with distance check)
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target) 
    {
        if (target == null) return true; 
        if (markedPlayers.Contains(target)) return true;

        if (markedPlayers.Count >= 2) return true;

        markedPlayers.Add(target);
        return false;
    }

    public override void AfterMeetingTasks() 
    {
        if (!PlayerControl.LocalPlayer.IsAlive() && !usedSecondChance) 
        {
            PlayerControl.LocalPlayer.RpcRevive();
        }
        usedSecondChance = true;
    }

    // Check if all marked Players are dead
    private bool AreAllMarkedPlayersDead() 
    {
        foreach (var player in markedPlayers)
        {
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

    // Win: all marked Players dead + 3 or fewer alive
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
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
