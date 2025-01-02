using TOHE.Roles.Core;
using UnityEngine;
using UnityEngine.Playables;
using static TOHE.Options;
using static TOHE.Utils;

namespace TOHE.Roles.Neutral;

internal class Summoner : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 92000;
    private static readonly HashSet<byte> playerIdList = new(); // Initialize properly
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralChaos;
    //================================================================\\

    private static OptionItem ReviveDelayOption;
    private static OptionItem DeathTimerOption;
    private static OptionItem KnowSummonedRoles;
    private static OptionItem KillCooldownOption;
    private static readonly Dictionary<byte, (CustomRoles originalRole, List<AddOnBase> originalAddOns)> SavedStates = new();
    private static readonly Dictionary<byte, float> SummonedTimers = new();
    private static readonly Dictionary<byte, float> SummonedHealth = new();
    private static List<(PlayerControl, float)> PendingRevives = new();
    private static readonly Dictionary<byte, long> LastUpdateTimes = new();

    private bool HasSummonedThisMeeting = false;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Summoner);

        // Revive Delay
        ReviveDelayOption = FloatOptionItem.Create(Id + 10, "Revive Delay", new(1f, 30f, 1f), 5f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Summoner])
            .SetValueFormat(OptionFormat.Seconds);

        // Death Timer
        DeathTimerOption = FloatOptionItem.Create(Id + 11, "Summoned Player Duration", new(5f, 120f, 5f), 30f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Summoner])
            .SetValueFormat(OptionFormat.Seconds);

        // Kill Cooldown
        KillCooldownOption = FloatOptionItem.Create(Id + 12, "Summoned Player Kill Cooldown", new(5f, 60f, 1f), 15f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Summoner])
            .SetValueFormat(OptionFormat.Seconds);

        KnowSummonedRoles = BooleanOptionItem.Create(Id + 13, "Know Summoner/Summoned Roles", true, TabGroup.NeutralRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Summoner]);
    }

    public override void Add(byte playerId)
    {
        base.Add(playerId);
        CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdate);
        var playerState = Main.PlayerStates[playerId];
        playerState.SetMainRole(CustomRoles.Summoner);
        playerState.IsSummoner = true;
    }

    public override void Init()
    {
        SummonedHealth.Clear();
        LastUpdateTimes.Clear();
    }

    public static bool SummonerCheckMsg(PlayerControl pc, string msg, bool isUI = false, bool isSystemMessage = false)
    {
        if (isSystemMessage || pc == null || !AmongUsClient.Instance.AmHost) return false; // Skip if system message or not host
        if (!GameStates.IsMeeting || pc == null || GameStates.IsExilling) return false; // Only during meetings
        if (!pc.Is(CustomRoles.Summoner) || !(pc.GetRoleClass() is Summoner summonerInstance)) return false;

        msg = msg.ToLower().Trim();
        Logger.Info($"Received command: {msg} from {pc.PlayerId}, Host: {AmongUsClient.Instance.AmHost}", "Summoner");

        if (!CheckCommand(ref msg, "summon")) return false;

        if (!pc.IsAlive())
        {
            Logger.Warn("Summoner is dead and cannot use commands.", "Summoner");
            return true;
        }

        if (!byte.TryParse(msg, out var targetId))
        {
            Logger.Warn("Invalid target ID for /summon command.", "Summoner");
            pc.Notify("Invalid target ID! Use /summon <ID>");
            return true;
        }

        PlayerControl targetPlayer = null;
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player.PlayerId == targetId)
            {
                targetPlayer = player;
                break; // Exit the loop once the player is found
            }
        }

        if (targetPlayer == null || targetPlayer.IsAlive())
        {
            Logger.Warn("Target player is invalid or alive.", "Summoner");
            pc.Notify("Target is invalid or not dead.");
            return true;
        }

        if (summonerInstance.HasSummonedThisMeeting)
        {
            Logger.Warn("Summoner has already summoned a player this meeting.", "Summoner");
            pc.Notify("You can only summon one player per meeting.");
            return true;
        }

        summonerInstance.RevivePlayer(targetPlayer);
        summonerInstance.HasSummonedThisMeeting = true;

        Logger.Info($"Summoner {pc.PlayerId} has summoned player {targetPlayer.PlayerId}.", "Summoner");

        return true; // Indicate the command was handled and suppress the message
    }


    public static bool CheckCommand(ref string msg, string command)
    {
        var comList = command.Split('|');
        for (int i = 0; i < comList.Length; i++)
        {
            if (msg.StartsWith("/" + comList[i]))
            {
                msg = msg.Replace("/" + comList[i], string.Empty).Trim();
                return true;
            }
        }
        return false;
    }

    public void RevivePlayer(PlayerControl targetPlayer)
    {
        if (targetPlayer == null || targetPlayer.Data == null || !targetPlayer.Data.IsDead)
        {
            Logger.Warn($"RevivePlayer: Invalid target or player is not dead.", "Summoner");
            return;
        }

        float reviveDelay = ReviveDelayOption?.GetFloat() ?? 5f;

        new LateTask(() =>
        {
            if (targetPlayer.IsAlive())
            {
                Logger.Info($"Player {targetPlayer.PlayerId} is already alive. Revive skipped.", "Summoner");
                return;
            }

            // Handle players already in the Summoned role
            if (targetPlayer.Is(CustomRoles.Summoned))
            {
                Summoned.RefreshTimer(targetPlayer.PlayerId);
                Logger.Info($"Player {targetPlayer.PlayerId} re-summoned with a refreshed timer.", "Summoner");
                return;
            }

            // Save current role and add-ons
            SaveRoleAndAddons(targetPlayer);

            // Reset sub-roles and assign Summoned role
            targetPlayer.ResetSubRoles(); // Clear all sub-roles and add-ons
            targetPlayer.RpcSetCustomRole(CustomRoles.Summoned);

            Logger.Info($"Player {targetPlayer.PlayerId} summoned and their role was saved.", "Summoner");

        }, reviveDelay, "SummonerRevive");
    }

    private void SaveRoleAndAddons(PlayerControl player)
    {
        var originalRole = player.GetCustomRole();
        var originalAddons = player.GetAddOns();
        SavedStates[player.PlayerId] = (originalRole, originalAddons);
        Logger.Info($"Player {player.PlayerId}'s role and add-ons saved.", "Summoner");
    }

    private void RestoreRoleAndAddons(PlayerControl player)
    {
        if (SavedStates.TryGetValue(player.PlayerId, out var state))
        {
            // Use RpcSetRole to restore the role
            player.RpcSetRole((AmongUs.GameOptions.RoleTypes)state.originalRole);

            // Restore saved add-ons
            foreach (var addon in state.originalAddOns)
            {
                player.AddAddOn(addon);
            }

            SavedStates.Remove(player.PlayerId);
            Logger.Info($"Player {player.PlayerId} restored to their original role and add-ons.", "Summoner");
        }
    }

    public void OnRoleRemove(byte playerId)
    {
        foreach (var summonedId in SavedStates.Keys.ToList())
        {
            var summonedPlayer = PlayerControl.GetPlayerById(summonedId);
            if (summonedPlayer != null && summonedPlayer.IsAlive())
            {
                summonedPlayer.RpcExileV2(); // Kill the summoned player
                RestoreRoleAndAddons(summonedPlayer); // Restore original role and add-ons
            }
        }

        base.OnRoleRemove(playerId);
    }
}

    public static bool CheckSummoned(PlayerControl player)
    {
        return player.Is(CustomRoles.Summoned); // Replace with your Summoned role check logic
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo bodyInfo)
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player.HasSpecificSubRole(CustomRoles.Summoned) && player.IsAlive())
            {
                

                // Trigger Summoned's custom death mechanics
                KillSummonedPlayer(player);
            }
        }
    }


    
    

    private void PerformRevive(PlayerControl targetPlayer, float reviveDelay)
    {
        if (targetPlayer.IsAlive()) return;

        new LateTask(() =>
        {
            if (targetPlayer.IsAlive()) return;

            // Handle players already in the Summoned role
            if (targetPlayer.Is(CustomRoles.Summoned))
            {
                Summoned.RefreshTimer(targetPlayer.PlayerId);
                Logger.Info($"Player {targetPlayer.PlayerId} re-summoned with a refreshed timer.", "Summoner");
                return;
            }

            // Save the player's current role and replace with Summoned
            SaveRoleAndAddons(targetPlayer);
            targetPlayer.RpcSetCustomRole(CustomRoles.Summoned);
            Logger.Info($"Player {targetPlayer.PlayerId} summoned and original role saved.", "Summoner");
        }, reviveDelay, "SummonerRevive");
    }

   
   





    private void NotifySummonerAndSummoned(string message)
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player.Is(CustomRoles.Summoner) || player.HasSpecificSubRole(CustomRoles.Summoned))
            {
                Utils.SendMessage(message, player.PlayerId);
            }
        }
    }

    private void StartDeathTimer(PlayerControl targetPlayer)
    {
        int deathTimer = Mathf.CeilToInt(DeathTimerOption.GetFloat());
        SummonedHealth[targetPlayer.PlayerId] = deathTimer;
        LastUpdateTimes[targetPlayer.PlayerId] = Utils.GetTimeStamp();

        Logger.Info($"Player {targetPlayer.GetRealName()} has been given a death timer of {deathTimer} seconds.", "Summoner");

        // Notify the summoned player
        NotifySummonedHealth(targetPlayer);
    }


    private void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (lowLoad || GameStates.IsMeeting) return;

        foreach (var (playerId, health) in SummonedHealth.ToList()) // Avoid modification issues during iteration
        {
            PlayerControl targetPlayer = null;

            // Find the player with the given playerId
            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p.PlayerId == playerId)
                {
                    targetPlayer = p;
                    break;
                }
            }

            if (targetPlayer == null)
            {
                ResetHealth(playerId); // Clean up invalid players
                continue;
            }

            // Skip timer updates if the player is dead
            if (targetPlayer.Data.IsDead)
            {
                Logger.Info($"Skipping timer update for dead summoned player {playerId}.", "Summoner");
                continue;
            }

            // Get the current time
            var currentTime = (long)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds;

            if (!LastUpdateTimes.TryGetValue(playerId, out var lastUpdateTime))
            {
                lastUpdateTime = currentTime;
            }

            // Calculate time difference and update health
            var deltaTime = currentTime - lastUpdateTime;
            SummonedHealth[playerId] = Mathf.Clamp(SummonedHealth[playerId] - deltaTime, 0f, DeathTimerOption.GetFloat());

            // Apply visual updates for health
            NotifySummonedHealth(targetPlayer);

            if (SummonedHealth[playerId] <= 0)
            {
                KillSummonedPlayer(targetPlayer);
                SummonedHealth.Remove(playerId);
                LastUpdateTimes.Remove(playerId);
            }

            // Update the last timestamp
            LastUpdateTimes[playerId] = currentTime;
        }
    }

    public static bool KnowRole(PlayerControl player, PlayerControl target)
    {
        // Summoner can see Summoned, Summoned can see Summoner
        if (player.Is(CustomRoles.Summoner) && target.HasSpecificSubRole(CustomRoles.Summoned)) return true;
        if (player.HasSpecificSubRole(CustomRoles.Summoned) && target.Is(CustomRoles.Summoner)) return true;

        // Summoned can see other Summoned players if enabled
        if (KnowSummonedRoles.GetBool() &&
            player.HasSpecificSubRole(CustomRoles.Summoned) &&
            target.HasSpecificSubRole(CustomRoles.Summoned))
            return true;

        return false;
    }
    

    private void NotifySummonedHealth(PlayerControl player)
    {
        if (player.Is(CustomRoles.Summoner) || player.HasSpecificSubRole(CustomRoles.Summoned))
        {
            // Notify only Summoner and Summoned players
            var health = Mathf.RoundToInt(SummonedHealth[player.PlayerId]);
            player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Summoned), $"Time Remaining: {health}s"));
        }
    }

    private static void KillSummonedPlayer(PlayerControl target)
    {
        target.SetDeathReason(PlayerState.DeathReason.SummonedExpired);
        target.RpcExileV2(); // Kill the player without leaving a body
        Logger.Info($"{target.GetRealName()} has died because their timer ran out.", "Summoner");
    }

    private void ResetHealth(byte playerId)
    {
        SummonedHealth.Remove(playerId);
        LastUpdateTimes.Remove(playerId);
    }


    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.Is(CustomRoles.Summoned) && (target.Is(CustomRoles.Summoner) || target.HasSpecificSubRole(CustomRoles.Summoned)))
        {
            string errorMessage = "You cannot kill the Summoner or other summoned players!";
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Summoner), errorMessage));
            return false; // Cancel the kill
        }

        return true; // Allow other kills
    }

    public override void AfterMeetingTasks()
    {
        base.AfterMeetingTasks();

        // Reset Summoning flag to allow reviving in the next meeting
        HasSummonedThisMeeting = false;

        // Handle pending revives queued during the meeting
        if (PendingRevives.Count > 0)
        {
            foreach (var (player, delay) in PendingRevives.ToList())
            {
                PerformRevive(player, delay);
            }
            PendingRevives.Clear();
        }

        // Process Summoned players
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (SummonedTimers.TryGetValue(player.PlayerId, out var remainingTime))
            {
                if (player.Data.IsDead)
                {
                    Logger.Info($"Player {player.GetRealName()} with remaining time will not automatically revive. Summoner must manually summon them again.", "Summoner");
                    SummonedTimers.Remove(player.PlayerId); // Remove the expired timer
                }
                else
                {
                    Logger.Warn($"Player {player.GetRealName()} is alive but had a timer. Timer will continue.", "Summoner");
                }
            }
        }
    }





    public override string GetLowerTextOthers(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (target == null || !isForHud) return string.Empty;

        // Check if roles should be visible
        if (KnowRole(seer, target))
        {
            if (target.Is(CustomRoles.Summoner))
                return ColorString(GetRoleColor(CustomRoles.Summoner), "Summoner");
            if (target.HasSpecificSubRole(CustomRoles.Summoned))
                return ColorString(GetRoleColor(CustomRoles.Summoned), "Summoned");
        }

        // Default behavior
        return string.Empty;
    }
}


