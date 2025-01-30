using UnityEngine;
using TOHE.Roles.Core;
namespace TOHE.Roles.Coven;

internal class Summoned : RoleBase
{
    private const int Id = 921000;

    // Dictionaries to track remaining time and last update time for each player
    private readonly Dictionary<byte, int> PlayerDie = new(); // Tracks remaining time (in seconds)
    private readonly Dictionary<byte, long> LastTime = new(); // Tracks the last update time (timestamp)

    public int NumKills { get; set; } = 0;

    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;

    public override CustomRoles Role => CustomRoles.Summoned;

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Summoner.KillCooldownOption.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;

    public override void Add(byte playerId)
    {
        base.Add(playerId);
        var playerState = Main.PlayerStates[playerId];
        playerState.SetMainRole(CustomRoles.Summoned);
        playerState.IsSummoned = true;

        // Initialize timer for the summoned player
        if (!PlayerDie.ContainsKey(playerId))
        {
            int deathTimer = Mathf.RoundToInt(Summoner.DeathTimerOption?.GetFloat() ?? 40f);
            PlayerDie[playerId] = deathTimer;
        }

        LastTime[playerId] = Utils.GetTimeStamp(); // Set the initial timestamp
        NotifySummonedHealth(playerId); // Notify player of their timer
    }

    public override void Init()
    {
        base.Init(); // Call base logic
        PlayerDie.Clear();
        LastTime.Clear();
    }



    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (lowLoad || GameStates.IsMeeting) || player.Data.IsDead) return; // Skip if low-load or during meetings

        var playerId = player.PlayerId;

        // Check if the player has a timer in SummonedHealth
        if (Summoner.SummonedHealth.TryGetValue(playerId, out var remainingTime))
        {
            // Get the last update time or initialize it
            if (!Summoner.LastUpdateTimes.TryGetValue(playerId, out long lastUpdateTime))
            {
                Summoner.LastUpdateTimes[playerId] = nowTime;
                return; // Skip processing for this tick
            }

            // Calculate elapsed time since the last update
            if (nowTime >= lastUpdateTime + 1) // Check if at least one second has passed
            {
                Summoner.LastUpdateTimes[playerId] = nowTime; // Update the last update timestamp
                Summoner.SummonedHealth[playerId]--; // Decrease the timer by 1

                // Notify the player of the updated timer
                NotifySummonedHealth(playerId);

                // Check if the timer has expired
                if (Summoner.SummonedHealth[playerId] <= 0)
                {
                    KillSummonedPlayer(player); // Kill the player
                    Summoner.SummonedHealth.Remove(playerId); // Remove them from the timer list
                    Summoner.LastUpdateTimes.Remove(playerId); // Remove the timestamp entry
                }
            }
        }
        else
        {
            // Initialize timer for players who are summoned but not yet tracked
            if (player.Is(CustomRoles.Summoned))
            {
                Summoner.SummonedHealth[playerId] = Summoner.DeathTimerOption.GetFloat(); // Set default timer
                Summoner.LastUpdateTimes[playerId] = nowTime; // Initialize timestamp
                NotifySummonedHealth(playerId); // Notify the player of their initial timer
            }
        }
    }

    // Notify player of their remaining time
    private void NotifySummonedHealth(byte playerId)
    {
        if (Summoner.SummonedHealth.TryGetValue(playerId, out var timeRemaining) && timeRemaining > 0)
        {
            var player = Main.AllPlayerControls.FirstOrDefault(p => p.PlayerId == playerId);
            if (player != null)
            {
                player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Summoned), $"Time Remaining: {timeRemaining}s"));
            }
        }
    }

    // Kill the summoned player when their timer runs out
   
    public static void KillSummonedPlayer(PlayerControl player)
    {
        if (player == null || player.Data == null) return;

        // Set the player's death reason and mark them as dead
        player.SetDeathReason(PlayerState.DeathReason.Expired);
        var playerState = Main.PlayerStates[player.PlayerId];
        if (playerState != null)
        {
            playerState.IsDead = true;
        }

        // Use RpcExileV2 to remove the player without leaving a body
        player.RpcExileV2();
    }

    public override string GetProgressText(byte playerId, bool comms)
    {
        int killRequirement = Summoner.SummonedKillRequirement.GetInt();
        if (killRequirement == 0) return string.Empty;

        if (Main.PlayerStates[playerId].RoleClass is not Summoned summonedInstance) return string.Empty;

        int kills = summonedInstance.NumKills;
        Color color = kills >= killRequirement ? Color.green : Color.red;

        return Utils.ColorString(color, $"({kills}/{killRequirement})");
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.Is(CustomRoles.Summoned) && (target.Is(CustomRoles.Summoner) || target.Is(CustomRoles.Summoned) || target.Is(Custom_Team.Coven)))
        {
            string errorMessage = "You cannot kill the Summoner or other summoned players!";
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Summoner), errorMessage));
            return false; // Cancel the kill
        }

        return true; // Allow other kills
    }
    public override void OnMurderPlayerAsKiller(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (killer.Is(CustomRoles.Summoned))
        {
            if (!Summoner.SummonedKillCounts.ContainsKey(killer.PlayerId))
            {
                Summoner.SummonedKillCounts[killer.PlayerId] = 0;
            }

            Summoner.SummonedKillCounts[killer.PlayerId]++;


            // Update the role's NumKills property for UI purposes
            if (killer.GetRoleClass() is Summoned summoned)
            {
                summoned.NumKills = Summoner.SummonedKillCounts[killer.PlayerId];
            }
        }
    }
    public void OnRemove(byte playerId)
    {
        base.OnRemove(playerId);
        PlayerDie.Remove(playerId);
        LastTime.Remove(playerId);
    }
}
