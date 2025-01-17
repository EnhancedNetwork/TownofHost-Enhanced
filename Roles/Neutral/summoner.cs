using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Utils;
using Hazel;
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


    public static OptionItem SummonedKillRequirement;
    private static OptionItem ReviveDelayOption;
    public static OptionItem DeathTimerOption;
    public static OptionItem KnowSummonedRoles;
    public static OptionItem KillCooldownOption;
    private static OptionItem RevealSummonedPlayer;
    private static OptionItem AllowSummoningRevivedPlayers;
    private static OptionItem HasAbilityUses;
    private static OptionItem MaxSummonsAllowed;
    public static bool HasWon { get; private set; } = false;
    private static int SummonsUsed = 0;
    public static readonly Dictionary<byte, int> SummonedKillCounts = new();
    private static readonly Dictionary<byte, CustomRoles> SavedStates = new();
    public static readonly Dictionary<byte, float> SummonedTimers = new();
    public static readonly Dictionary<byte, float> SummonedHealth = new();
    private static List<(PlayerControl, float)> PendingRevives = new();
    public static readonly Dictionary<byte, long> LastUpdateTimes = new();

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

        RevealSummonedPlayer = BooleanOptionItem.Create(Id + 14, "Reveal Summoned Player", true, TabGroup.NeutralRoles, false)
       .SetParent(CustomRoleSpawnChances[CustomRoles.Summoner]);

        SummonedKillRequirement = IntegerOptionItem.Create(Id + 15, "Summoned Kill Requirement", new(0, 2, 1), 0, TabGroup.NeutralRoles, false)
       .SetParent(CustomRoleSpawnChances[CustomRoles.Summoner])
       .SetValueFormat(OptionFormat.Times);

        AllowSummoningRevivedPlayers = BooleanOptionItem.Create(Id + 16, "AllowSummoningRevivedPlayers", false, TabGroup.NeutralRoles, false)
       .SetParent(CustomRoleSpawnChances[CustomRoles.Summoner]);

        HasAbilityUses = BooleanOptionItem.Create(Id + 17, "Summoner_HasAbilityUses", true, TabGroup.NeutralRoles, false)
       .SetParent(CustomRoleSpawnChances[CustomRoles.Summoner]);

        MaxSummonsAllowed = IntegerOptionItem.Create(Id + 18, "Summoner_MaxSummonsAllowed", new(3, 15, 1), 5, TabGroup.NeutralRoles, false)
       .SetParent(HasAbilityUses).SetValueFormat(OptionFormat.Times);
    }



    public override void Add(byte playerId)
    {
        base.Add(playerId);
        var playerState = Main.PlayerStates[playerId];
        playerState.SetMainRole(CustomRoles.Summoner);
        playerState.IsSummoner = true;
        playerIdList.Add(playerId);
        SummonsUsed = 0;
    }

    public override void Init()
    {
        SummonedHealth.Clear();
        LastUpdateTimes.Clear();
        playerIdList.Clear();
        SummonsUsed = 0;
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        // Check if the seer is Summoner and alive, and the seen player is dead
        if (!seer.IsAlive() || seen.IsAlive()) return string.Empty;

        return ColorString(GetRoleColor(CustomRoles.Summoner), $" {seen.Data.PlayerId}");
    }

    public static bool SummonerCheckMsg(PlayerControl pc, string msg, bool isUI = false, bool isSystemMessage = false)
    {
        if (isSystemMessage || !AmongUsClient.Instance.AmHost) return false; // Skip if system message or not host
        if (!GameStates.IsMeeting || pc == null || GameStates.IsExilling) return false; // Only during meetings
        if (!pc.Is(CustomRoles.Summoner) || !(pc.GetRoleClass() is Summoner summonerInstance)) return false;

        msg = msg.ToLower().Trim();
        Logger.Info($"Received command: {msg} from {pc.PlayerId}, Host: {AmongUsClient.Instance.AmHost}", "Summoner");

        if (!CheckCommand(ref msg, "summon")) return false;

        // Always try hiding the message if the command is intercepted
        HideSummonCommand(pc);

        if (!pc.IsAlive())
        {
            Logger.Warn("Summoner is dead and cannot use commands.", "Summoner");
            pc.Notify("You cannot summon players while dead.");
            return true;
        }

        if (!byte.TryParse(msg, out var targetId))
        {
            Logger.Warn("Invalid target ID for /summon command.", "Summoner");
            pc.Notify("Invalid target ID! Use /summon <ID>");
            return true;
        }

        PlayerControl targetPlayer = Main.AllPlayerControls.FirstOrDefault(player => player.PlayerId == targetId);

        if (targetPlayer == null)
        {
            Logger.Warn("Target player is invalid or does not exist.", "Summoner");
            pc.Notify("Target player not found.");
            return true;
        }

        bool isAlreadySummoned = targetPlayer.Is(CustomRoles.Summoned);
        bool allowResummoning = AllowSummoningRevivedPlayers.GetBool();

        // Handle the case where reviving already summoned players is allowed
        if (allowResummoning && isAlreadySummoned)
        {
            // Add to pending revives list without using a summon or marking the meeting as used
            summonerInstance.RevivePlayer(targetPlayer);
            Logger.Info($"Player {targetPlayer.PlayerId} (already Summoned) added to pending revives without consuming a summon.", "Summoner");

            // Send global message only if required
            if (RevealSummonedPlayer.GetBool())
            {
                AddMsg($"The SUMMONER has reactivated {targetPlayer.GetRealName()}! \nKillers beware!", byte.MaxValue, "Summoner Announcement");
            }

            return true;
        }

        // Check summon use limit
        if (HasAbilityUses.GetBool() && SummonsUsed >= MaxSummonsAllowed.GetInt())
        {
            pc.Notify("You have reached the maximum number of summons.");
            return true;
        }

        if (targetPlayer.IsAlive())
        {
            Logger.Warn("Target player is already alive.", "Summoner");
            pc.Notify("Target is invalid or already alive.");
            return true;
        }

        // Check if Summoner has already summoned this meeting
        if (summonerInstance.HasSummonedThisMeeting)
        {
            Logger.Warn("Summoner has already summoned a player this meeting.", "Summoner");
            pc.Notify("You can only summon one player per meeting.");
            return true;
        }

        // Normal summoning logic
        HideSummonCommand(pc);
        summonerInstance.RevivePlayer(targetPlayer);

        // Increment summon count if allowed
        if (!isAlreadySummoned || !allowResummoning)
        {
            SummonsUsed++;
        }

        summonerInstance.HasSummonedThisMeeting = true;

        // Send global message
        string summonMessage = Summoner.RevealSummonedPlayer.GetBool()
            ? $"The SUMMONER has brought {targetPlayer.GetRealName()} back to life! \nKillers beware!"
            : "The SUMMONER has brought someone back to life! \nKillers beware!";
        AddMsg(summonMessage, byte.MaxValue, "Summoner Announcement");

        // Send private message to the summoned player if hidden
        if (!Summoner.RevealSummonedPlayer.GetBool())
        {
            Utils.SendMessage(
                "The summoner has chosen to revive you! \nHunt down the killers!",
                targetPlayer.PlayerId
            );
        }

        Logger.Info($"Summoner {pc.PlayerId} has summoned player {targetPlayer.PlayerId}. System message sent: {summonMessage}", "Summoner");
        return true; // Suppress the command message
    }


    private static void HideSummonCommand(PlayerControl summoner)
    {
        ChatUpdatePatch.DoBlockChat = true;

        // Define a set of decoy commands or filler messages
        string[] decoyCommands = { "/summon" };
        var random = IRandom.Instance;

        for (int i = 0; i < 20; i++) // Generate a few decoy messages
        {
            string decoyMessage = decoyCommands[random.Next(0, decoyCommands.Length)];

            // Pick a random player for the decoy sender
            var randomPlayer = Main.AllAlivePlayerControls.RandomElement();

            // Add the decoy message to the chat
            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(randomPlayer, decoyMessage);

            // Send the decoy message via RPC
            var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
            writer.StartMessage(-1);
            writer.StartRpc(randomPlayer.NetId, (byte)RpcCalls.SendChat)
                .Write(decoyMessage)
                .EndRpc();
            writer.EndMessage();
            writer.SendMessage();
        }

        ChatUpdatePatch.DoBlockChat = false;
    }

    private static void AddMsg(string message, byte playerId, string title)
    {


        // Send the actual message
        Utils.SendMessage(
            Utils.ColorString(Utils.GetRoleColor(CustomRoles.Summoner), title) + "\n" + message,
            playerId
        );

        Logger.Info($"Message sent to all: {message} with title {title}", "Summoner");
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

        if (GameStates.IsMeeting)
        {
            // Queue the revive to happen after the meeting
            PendingRevives.Add((targetPlayer, reviveDelay));
            Logger.Info($"Player {targetPlayer.PlayerId} revive queued for after the meeting.", "Summoner");
        }
        else
        {
            // Perform immediate revive if not in a meeting
            PerformRevive(targetPlayer, reviveDelay);
        }
    }
    public bool CanSummon()
    {
        if (!HasAbilityUses.GetBool()) return true; // Unlimited summons if ability uses are disabled
        return SummonsUsed < MaxSummonsAllowed.GetInt(); // Check remaining summons
    }

    public void SummonPlayer(PlayerControl targetPlayer)
    {
        if (!CanSummon())
        {

            return;
        }



        // Add your summoning code here
        RevivePlayer(targetPlayer);

        Logger.Info($"Summoned player {targetPlayer.PlayerId}. Remaining summons: {MaxSummonsAllowed.GetInt() - SummonsUsed}", "Summoner");
    }

    private static void SaveOriginalRole(PlayerControl player)
    {
        // Check if the role is already saved
        if (SavedStates.ContainsKey(player.PlayerId))
        {
            Logger.Warn($"Player {player.PlayerId}'s original role is already saved as {SavedStates[player.PlayerId]}. Skipping save.", "Summoner");
            return;
        }

        // Save the player's original role
        var originalRole = player.GetCustomRole();
        SavedStates[player.PlayerId] = originalRole; // Save only the original role
        Logger.Info($"Player {player.PlayerId}'s original role saved as {originalRole}.", "Summoner");
    }




    private void RestoreOriginalRole(PlayerControl player)
    {
        if (SavedStates.TryGetValue(player.PlayerId, out var originalRole))
        {
            // Change to the original role's basis

            player.RpcChangeRoleBasis(originalRole);

            // Assign the original role
            player.RpcSetCustomRole(originalRole);

            // Reinitialize the role class
            player.GetRoleClass()?.OnAdd(player.PlayerId);

            // Sync role settings with the client
            player.SyncSettings();

            // Ensure the player is marked as a ghost if their original role is ghost-based
            var playerState = Main.PlayerStates[player.PlayerId];
            playerState.IsDead = originalRole.IsGhostRole();

            // Log role restoration
            Logger.Info($"Player {player.PlayerId} restored to original role: {originalRole}, Role Basis: {ThisRoleBase}.", "Summoner");

            // Clean up saved state
            SavedStates.Remove(player.PlayerId);
        }
    }






    public void OnRoleRemove(byte playerId)
    {
        foreach (var summonedId in SavedStates.Keys.ToList())
        {
            PlayerControl summonedPlayer = null;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player.PlayerId == summonedId)
                {
                    summonedPlayer = player;
                    break;
                }
            }

            if (summonedPlayer != null && summonedPlayer.IsAlive())
            {
                summonedPlayer.RpcExileV2(); // Kill the summoned player
                RestoreOriginalRole(summonedPlayer); // Restore original role
            }
        }

        Logger.Info($"Summoner with player ID {playerId} removed, and all summoned players reset.", "Summoner");
    }
    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (!target.Is(CustomRoles.Summoner)) return;

        Logger.Info($"Summoner {target.GetRealName()} has died. Restoring and killing all Summoned players.", "Summoner");

        foreach (var summonedId in SavedStates.Keys.ToList())
        {
            PlayerControl summonedPlayer = Main.AllPlayerControls.FirstOrDefault(p => p.PlayerId == summonedId);
            if (summonedPlayer != null)
            {
                // Restore the original role
                RestoreOriginalRole(summonedPlayer);

                // Kill the summoned player after restoring their role
                summonedPlayer.RpcTeleport(ExtendedPlayerControl.GetBlackRoomPosition());
                summonedPlayer.SetDeathReason(PlayerState.DeathReason.Expired); // Set a custom death reason
                summonedPlayer.SetRealKiller(target); // Set the Summoner as the cause of death
                summonedPlayer.RpcMurderPlayer(summonedPlayer); // Directly kill the player
                Logger.Info($"Summoned player {summonedPlayer.GetRealName()} killed due to Summoner's death.", "Summoner");
            }
        }

        SavedStates.Clear();
    }


    public override string GetProgressText(byte playerId, bool comms)
    {
        // Show nothing if ability uses are disabled
        if (!HasAbilityUses.GetBool()) return string.Empty;

        int maxSummons = MaxSummonsAllowed.GetInt();
        int remainingSummons = maxSummons - SummonsUsed;
        Color color = remainingSummons > 0 ? Color.green : Color.red;

        return Utils.ColorString(color, $"{remainingSummons}/{maxSummons}");
    }




    public static bool CheckSummoned(PlayerControl player)
    {
        return player.Is(CustomRoles.Summoned);
    }


    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo bodyInfo)
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player.Is(CustomRoles.Summoned) && player.IsAlive())
            {

                Summoned.KillSummonedPlayer(player);
            }
        }
    }






    private void PerformRevive(PlayerControl targetPlayer, float reviveDelay)
    {
        if (targetPlayer.IsAlive()) return;

        new LateTask(() =>
        {
            if (targetPlayer.IsAlive()) return;

            // RPC Revive to ensure the player is properly revived
            targetPlayer.RpcRevive();

            // Handle players already in the Summoned role
            // Save the player's current role and replace it with Summoned
            SaveOriginalRole(targetPlayer);

            if (!SavedStates.ContainsKey(targetPlayer.PlayerId))
            {
                SaveOriginalRole(targetPlayer);
            }
            // Update the player's role basis and set the custom role
            targetPlayer.RpcChangeRoleBasis(CustomRoles.Summoned); // Update basis to Summoned
            targetPlayer.RpcSetCustomRole(CustomRoles.Summoned);   // Assign Summoned role

            // Check if the player has an existing kill count
            if (targetPlayer.GetRoleClass() is Summoned summoned)
            {
                if (!SavedStates.ContainsKey(targetPlayer.PlayerId)) // First time being revived
                {
                    summoned.NumKills = 0; // Reset kill count to zero
                    Logger.Info($"Player {targetPlayer.PlayerId} revived for the first time. Kill count reset.", "Summoner");
                }
                else
                {
                    // Restore the existing kill count
                    if (SummonedKillCounts.TryGetValue(targetPlayer.PlayerId, out int existingKills))
                    {
                        summoned.NumKills = existingKills;
                        Logger.Info($"Restored {targetPlayer.PlayerId}'s kill count to {summoned.NumKills}.", "Summoner");
                    }
                }
            }

            // Initialize role logic for the new role
            targetPlayer.GetRoleClass()?.OnAdd(targetPlayer.PlayerId);

            // Initialize tasks and abilities
            var playerState = Main.PlayerStates[targetPlayer.PlayerId];
            playerState.InitTask(targetPlayer); // Initialize tasks
            targetPlayer.ResetKillCooldown();  // Reset kill cooldowns
            playerState.ResetSubRoles();

            // Sync player settings to ensure UI updates
            targetPlayer.SyncSettings();

            Logger.Info($"Player {targetPlayer.PlayerId} revived and properly initialized as Summoned.", "Summoner");
        }, reviveDelay, "SummonerRevive");
    }
    public void SaveSummonedKillCount(PlayerControl summonedPlayer)
    {
        if (summonedPlayer.GetRoleClass() is Summoned summoned)
        {
            SummonedKillCounts[summonedPlayer.PlayerId] = summoned.NumKills;
            Logger.Info($"Saved kill count for {summonedPlayer.PlayerId}: {summoned.NumKills}", "Summoner");
        }
    }


    public static void UpdateWinCondition(bool won)
    {
        HasWon = won;
    }




    public static float GetDeathTimer(byte playerId)
    {
        // Return the player's death timer if it exists, otherwise use the default from Summoner options
        if (SummonedHealth.TryGetValue(playerId, out var deathTime))
        {
            return deathTime;
        }

        // Fallback to the configured default death timer
        return Summoner.DeathTimerOption?.GetFloat() ?? 40f; // Default to 40 seconds if not set
    }







    public static bool KnowRole(PlayerControl player, PlayerControl target)
    {
        // Summoner can see Summoned, Summoned can see Summoner
        if (player.Is(CustomRoles.Summoner) && target.Is(CustomRoles.Summoned)) return true;
        if (player.Is(CustomRoles.Summoned) && target.Is(CustomRoles.Summoner)) return true;

        // Summoned can see other Summoned players if enabled
        if (KnowSummonedRoles.GetBool() &&
            player.Is(CustomRoles.Summoned) &&
            target.Is(CustomRoles.Summoned))
            return true;

        return false;
    }


    public static void NotifySummonedHealth(PlayerControl player)
    {
        if (player.Is(CustomRoles.Summoner) || player.Is(CustomRoles.Summoned))
        {
            var health = Mathf.RoundToInt(SummonedHealth[player.PlayerId]);
            player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Summoned), $"Time Remaining: {health}s"));
        }
    }









    public override void AfterMeetingTasks()
    {
        base.AfterMeetingTasks();

        // Reset the summoning flag for the next meeting
        HasSummonedThisMeeting = false;

        // Process all pending revives
        foreach (var (player, delay) in PendingRevives.ToList())
        {
            PerformRevive(player, delay);
            PendingRevives.Remove((player, delay)); // Remove the processed revive
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
            if (target.Is(CustomRoles.Summoned))
                return ColorString(GetRoleColor(CustomRoles.Summoned), "Summoned");
        }

        // Default behavior
        return string.Empty;
    }
}




