using TOHE.Roles.Neutral;
using TOHE;
using UnityEngine;
using System.Collections.Generic;
using TOHE.Roles.Core;

internal class Summoned : RoleBase
{

    private readonly Dictionary<byte, int> PlayerTimers = new(); // Tracks remaining time
    private readonly Dictionary<byte, long> LastUpdateTimes = new(); // Tracks last update time
    public int NumKills { get; set; } = 0;

    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralChaos;


    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Summoner.KillCooldownOption.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override void Add(byte playerId)
    {
        base.Add(playerId);

        // Initialize the timer for the summoned player
        if (!PlayerTimers.ContainsKey(playerId))
        {
            int deathTimer = Mathf.RoundToInt(Summoner.DeathTimerOption?.GetFloat() ?? 40f);
            PlayerTimers[playerId] = deathTimer;
            LastUpdateTimes[playerId] = Utils.GetTimeStamp();
            NotifySummonedHealth(playerId);
        }
    }

    private void NotifySummonedHealth(byte playerId)
    {
        if (PlayerTimers.TryGetValue(playerId, out int timer))
        {
            PlayerControl targetPlayer = null;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player.PlayerId == playerId)
                {
                    targetPlayer = player;
                    break; // Exit the loop once the player is found
                }
            }

            if (targetPlayer != null)
            {
                targetPlayer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Summoned), $"Time Remaining: {timer}s"));
            }
        }
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (lowLoad || GameStates.IsMeeting) return;

        foreach (var playerId in PlayerTimers.Keys.ToList()) // Use ToList to avoid modifying the collection during iteration
        {
            if (LastUpdateTimes.TryGetValue(playerId, out long lastUpdateTime) && lastUpdateTime + 1 <= nowTime)
            {
                // Update the timer
                LastUpdateTimes[playerId] = nowTime;
                PlayerTimers[playerId]--;

                if (PlayerTimers[playerId] <= 0)
                {
                    // Timer expired, kill the player
                    PlayerControl targetPlayer = null;

                    foreach (var p in PlayerControl.AllPlayerControls)
                    {
                        if (p.PlayerId == playerId)
                        {
                            targetPlayer = p;
                            break; // Exit loop once the player is found
                        }
                    }

                    if (targetPlayer != null)
                    {
                        KillSummonedPlayer(targetPlayer);
                    }

                    PlayerTimers.Remove(playerId);
                    LastUpdateTimes.Remove(playerId);
                }
                else
                {
                    // Notify the player of their updated timer
                    NotifySummonedHealth(playerId);
                }
            }
        }
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.Is(CustomRoles.Summoned) && (target.Is(CustomRoles.Summoner) || target.Is(CustomRoles.Summoned)))
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

    public static void KillSummonedPlayer(PlayerControl player)
    {
        if (player == null || player.Data == null) return;

        // Set the player's death reason
        player.SetDeathReason(PlayerState.DeathReason.Expired);

        // Mark the player as dead in their state
        var playerState = Main.PlayerStates[player.PlayerId];
        if (playerState != null)
        {
            playerState.IsDead = true; // Explicitly mark the player as dead
        }

        // Use RpcExileV2 to kill the player without leaving a body
        player.RpcExileV2();

        // Log the event for debugging purposes

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

    public static bool KnowRole(PlayerControl player, PlayerControl target)
    {
        // Summoner can see Summoned, Summoned can see Summoner
        if (player.Is(CustomRoles.Summoner) && target.Is(CustomRoles.Summoned)) return true;
        if (player.Is(CustomRoles.Summoned) && target.Is(CustomRoles.Summoner)) return true;

        // Summoned can see other Summoned players if enabled
        if (Summoner.KnowSummonedRoles.GetBool() &&
            player.Is(CustomRoles.Summoned) &&
            target.Is(CustomRoles.Summoned))
            return true;

        return false;
    }

    public void OnRemove(byte playerId)
    {
        PlayerTimers.Remove(playerId);
        LastUpdateTimes.Remove(playerId);
    }

    public static void ResetKillCounts()
    {
        foreach (var playerState in Main.PlayerStates.Values)
        {
            if (playerState.RoleClass is Summoned summoned)
            {
                summoned.NumKills = 0; // Reset the kill count for all Summoned players

            }
        }
    }
}
