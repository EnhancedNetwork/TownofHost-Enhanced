using System;
using AmongUs.GameOptions;
using Hazel;
using TOHE.Modules;
using TOHE.Modules.ChatManager;
using TOHE.Patches;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Coven;

internal class Summoner : CovenManager
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Summoner;
    private const int Id = 32700;
    public override bool IsDesyncRole => true;
    public override bool IsExperimental => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenPower;
    //================================================================\\


    public static OptionItem SummonedKillRequirement;
    private static OptionItem ReviveDelayOption;
    public static OptionItem DeathTimerOption;
    public static OptionItem KnowSummonedRoles;
    public static OptionItem KillCooldownOption;
    public static OptionItem NecroKillCooldownOption;
    private static OptionItem RevealSummonedPlayer;
    private static OptionItem AllowSummoningRevivedPlayers;
    private static OptionItem ResummonTakesUse;
    public static OptionItem SummonedKillsCountToSummoner;
    public static OptionItem NoMeetingWhileSummoned;
    private static OptionItem HasAbilityUses;
    private static OptionItem MaxSummonsAllowed;

    private static readonly Dictionary<byte, RoleBase> SummonedOriginalRoles = [];

    private static readonly Dictionary<byte, HashSet<byte>> SummonedPlayerIds = [];

    public static readonly Dictionary<byte, int> SummonedKillCounts = [];
    public static readonly Dictionary<byte, float> SummonedHealth = [];
    private static readonly List<(PlayerControl, float)> PendingRevives = [];
    public static readonly Dictionary<byte, long> LastUpdateTimes = [];
    private static readonly Dictionary<byte, bool> HasSummonedThisMeeting = [];

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = NecroKillCooldownOption.GetFloat();
    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, Role, 1, zeroOne: false);

        // Revive Delay
        ReviveDelayOption = FloatOptionItem.Create(Id + 10, "SummonerSettings.ReviveDelay", new(1f, 30f, 1f), 5f, TabGroup.CovenRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Summoner])
        .SetValueFormat(OptionFormat.Seconds);

        // Death Timer
        DeathTimerOption = FloatOptionItem.Create(Id + 11, "SummonerSettings.SummonDuration", new(5f, 120f, 5f), 30f, TabGroup.CovenRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Summoner])
        .SetValueFormat(OptionFormat.Seconds);

        // Kill Cooldown
        KillCooldownOption = FloatOptionItem.Create(Id + 12, "SummonerSettings.SummonedKillCooldown", new(0f, 60f, 1f), 15f, TabGroup.CovenRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Summoner])
        .SetValueFormat(OptionFormat.Seconds);

        NecroKillCooldownOption = FloatOptionItem.Create(Id + 19, "SummonerSettings.SummonerKillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.CovenRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Summoner])
        .SetValueFormat(OptionFormat.Seconds);

        KnowSummonedRoles = BooleanOptionItem.Create(Id + 13, "SummonerSettings.SummonedKnowsCoven", true, TabGroup.CovenRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Summoner]);

        RevealSummonedPlayer = BooleanOptionItem.Create(Id + 14, "SummonerSettings.RevealSummoned", true, TabGroup.CovenRoles, false)
       .SetParent(CustomRoleSpawnChances[CustomRoles.Summoner]);

        SummonedKillRequirement = IntegerOptionItem.Create(Id + 15, "SummonerSettings.SummonedKillRequirement", new(0, 2, 1), 0, TabGroup.CovenRoles, false)
       .SetParent(CustomRoleSpawnChances[CustomRoles.Summoner])
       .SetValueFormat(OptionFormat.Times);

        AllowSummoningRevivedPlayers = BooleanOptionItem.Create(Id + 16, "SummonerSettings.AllowResummon", false, TabGroup.CovenRoles, false)
       .SetParent(CustomRoleSpawnChances[CustomRoles.Summoner]);
        ResummonTakesUse = BooleanOptionItem.Create(Id + 22, "SummonerSettings.ResummonTakesUse", false, TabGroup.CovenRoles, false)
       .SetParent(AllowSummoningRevivedPlayers);

        SummonedKillsCountToSummoner = BooleanOptionItem.Create(Id + 20, "SummonerSettings.SummonedKillsCountToSummoner", false, TabGroup.CovenRoles, false)
       .SetParent(CustomRoleSpawnChances[CustomRoles.Summoner]);

        NoMeetingWhileSummoned = BooleanOptionItem.Create(Id + 21, "SummonerSettings.NoMeetingWhileSummoned", false, TabGroup.CovenRoles, false)
       .SetParent(CustomRoleSpawnChances[CustomRoles.Summoner]);

        HasAbilityUses = BooleanOptionItem.Create(Id + 17, "SummonerSettings.HasAbilityUses", true, TabGroup.CovenRoles, false)
       .SetParent(CustomRoleSpawnChances[CustomRoles.Summoner]);

        MaxSummonsAllowed = IntegerOptionItem.Create(Id + 18, GeneralOption.SkillLimitTimes, new(3, 15, 1), 5, TabGroup.CovenRoles, false)
       .SetParent(HasAbilityUses).SetValueFormat(OptionFormat.Times);
    }

    public override void Add(byte playerId)
    {
        base.Add(playerId);
        playerId.SetAbilityUseLimit(MaxSummonsAllowed.GetInt());
        CustomRoleManager.CheckDeadBodyOthers.Add(OnPlayerDead);
        HasSummonedThisMeeting[playerId] = false;
    }

    public override void Init()
    {
        SummonedHealth.Clear();
        LastUpdateTimes.Clear();
        SummonedOriginalRoles.Clear();
        SummonedPlayerIds.Clear();
        PendingRevives.Clear();
        SummonedKillCounts.Clear();
        HasSummonedThisMeeting.Clear();
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        // Check if the seer is Summoner and alive, and the seen player is dead
        if (!seer.IsAlive() || seen.IsAlive()) return string.Empty;

        return ColorString(GetRoleColor(CustomRoles.Summoner), $" {seen.Data.PlayerId}");
    }
    public override bool CanUseKillButton(PlayerControl pc) => HasNecronomicon(pc);
    public override bool OnCheckStartMeeting(PlayerControl reporter)
    {
        if (!NoMeetingWhileSummoned.GetBool()) return true; // Skip if the option is disabled
        if (Main.EnumerateAlivePlayerControls().Any(player => player.Is(CustomRoles.Summoned)))
        {
            reporter.Notify(ColorString(GetRoleColor(CustomRoles.Summoner), GetString("Summoner.NoMeetingWhileSummoned")), 20f);
            return false; // Prevent starting the meeting
        }
        return true;
    }

    public static void SummonCommand(PlayerControl pc, string commandKey, string msg, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            ChatCommands.RequestCommandProcessingFromHost(msg, commandKey);
            return;
        }

        if (!GameStates.IsMeeting || pc == null || GameStates.IsExilling) return; // Only during meetings
        if (!pc.Is(CustomRoles.Summoner)) return;

        if (!pc.IsAlive())
        {
            Logger.Warn("Summoner is dead and cannot use commands.", "Summoner");
            SendMessage(GetString("Summoner.SummonerDead"), pc.PlayerId, CustomRoles.Summoner.ToColoredString().ToUpper());
            return;
        }

        if (!byte.TryParse(msg, out var targetId))
        {
            Logger.Warn("Invalid target ID for /summon command.", "Summoner");
            SendMessage(GetString("Summoner.InvalidID"), pc.PlayerId, CustomRoles.Summoner.ToColoredString().ToUpper());
            return;
        }

        PlayerControl targetPlayer = Main.EnumeratePlayerControls().FirstOrDefault(player => player.PlayerId == targetId);

        if (targetPlayer == null)
        {
            Logger.Warn("Target player is invalid or does not exist.", "Summoner");
            SendMessage(GetString("Summoner.NullPlayer"), pc.PlayerId, CustomRoles.Summoner.ToColoredString().ToUpper());
            return;
        }

        if (!SummonedPlayerIds.ContainsKey(pc.PlayerId))
            SummonedPlayerIds[pc.PlayerId] = [];

        bool isAlreadySummoned = SummonedPlayerIds[pc.PlayerId].Contains(targetPlayer.PlayerId);
        bool allowResummoning = AllowSummoningRevivedPlayers.GetBool();

        // Handle the case where reviving already summoned players is allowed
        if (allowResummoning && isAlreadySummoned)
        {
            // Add to pending revives list without using a summon or marking the meeting as used
            RevivePlayer(pc, targetPlayer);
            Logger.Info($"Player {targetPlayer.PlayerId} (already Summoned) added to pending revives.", "Summoner");

            if (ResummonTakesUse.GetBool() && HasAbilityUses.GetBool())
            {
                // Only decrement ability use if the option is enabled
                pc.RpcRemoveAbilityUse();
                Logger.Info($"Decremented ability use for {pc.PlayerId} due to resummoning.", "Summoner");
            }

            // Send global message only if required
            if (RevealSummonedPlayer.GetBool())
            {
                SendMessage(string.Format(GetString("Summoner.SummonAnnouncementAgain"), targetPlayer.GetRealName()), byte.MaxValue, CustomRoles.Summoner.ToColoredString().ToUpper());
            }

            return;
        }

        // Check summon use limit
        if (HasAbilityUses.GetBool() && pc.GetAbilityUseLimit() <= 0)
        {
            SendMessage(GetString("Summoner.NoUses"), pc.PlayerId, CustomRoles.Summoner.ToColoredString().ToUpper());
            return;
        }

        if (targetPlayer == pc)
        {
            Logger.Warn($"{targetPlayer.GetRealName()} tried to summon self, lol", "Summoner");
            SendMessage(GetString("Summoner.SummonSelf"), pc.PlayerId, CustomRoles.Summoner.ToColoredString().ToUpper());
            return;
        }

        if (targetPlayer.IsAlive())
        {
            Logger.Warn($"{targetPlayer.GetRealName()} is already alive.", "Summoner");
            SendMessage(GetString("Summoner.PlayerAlive"), pc.PlayerId, CustomRoles.Summoner.ToColoredString().ToUpper());
            return;
        }

        // Check if Summoner has already summoned this meeting
        if (HasSummonedThisMeeting[pc.PlayerId])
        {
            Logger.Warn("Summoner has already summoned a player this meeting.", "Summoner");
            SendMessage(GetString("Summoner.SummonedThisMeeting"), pc.PlayerId, CustomRoles.Summoner.ToColoredString().ToUpper());
            return;
        }

        // Check if Summoner has already summoned this meeting
        if (!allowResummoning && isAlreadySummoned)
        {
            Logger.Warn($"Summoner tried summoning {targetPlayer.GetRealName()} but settings disallow it", "Summoner");
            SendMessage(string.Format(GetString("Summoner.AlreadySummoned"), targetPlayer.GetRealName()), pc.PlayerId, CustomRoles.Summoner.ToColoredString().ToUpper());
            return;
        }

        // Normal summoning logic
        RevivePlayer(pc, targetPlayer);

        // Increment summon count if allowed
        if (!isAlreadySummoned || !allowResummoning)
        {
            pc.RpcRemoveAbilityUse();
        }

        HasSummonedThisMeeting[pc.PlayerId] = true;

        // Send global message
        string summonMessage = RevealSummonedPlayer.GetBool()
            ? string.Format(GetString("Summoner.SummonAnnouncement"), targetPlayer.GetRealName())
            : GetString("Summoner.SummonAnnoucementNameless");
        SendMessage(summonMessage, byte.MaxValue, CustomRoles.Summoner.ToColoredString().ToUpper());

        // Send private message to the summoned player if hidden
        if (!RevealSummonedPlayer.GetBool())
        {
            SendMessage(GetString("Summoned.Notification"), targetPlayer.PlayerId, CustomRoles.Summoner.ToColoredString().ToUpper());
        }

        Logger.Info($"Summoner {pc.PlayerId} has summoned player {targetPlayer.PlayerId}. System message sent: {summonMessage}", "Summoner");
    }

    public static void RevivePlayer(PlayerControl summoner, PlayerControl targetPlayer)
    {
        if (targetPlayer == null || targetPlayer.Data == null || !targetPlayer.Data.IsDead || targetPlayer.IsAlive())
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
            PerformRevive(summoner, targetPlayer, reviveDelay);
        }
    }
    public static void CancelPendingRevives()
    {
        foreach (var (player, delay) in PendingRevives.ToList())
        {
            PendingRevives.Remove((player, delay));
        }
    }
    private static void SaveOriginalRole(PlayerControl player)
    {
        if (!SummonedOriginalRoles.ContainsKey(player.PlayerId))
        {
            var originalRole = player.GetRoleClass();
            SummonedOriginalRoles[player.PlayerId] = originalRole;
            Logger.Info($"Saved original role for player {player.PlayerId}: {originalRole}.", "Summoner");
        }
    }
    public static void RestoreOriginalRole(PlayerControl player)
    {
        if (SummonedOriginalRoles.TryGetValue(player.PlayerId, out RoleBase originalRole))
        {
            if (player.IsAlive()) player.RpcChangeRoleBasis(originalRole.ThisRoleBase);
            else
            {
                if (originalRole.HasTasks(player.Data, originalRole.Role, false))
                {
                    player.RpcSetRoleType(RoleTypes.CrewmateGhost, true);
                }
                else
                {
                    player.RpcSetRoleType(RoleTypes.ImpostorGhost, false);
                }
            }
            player.RpcSetCustomRole(originalRole.Role);
            Logger.Info($"Restored player {player.PlayerId} to their original role: {originalRole}.", "Summoner");
        }
    }

    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (!target.Is(CustomRoles.Summoner)) return;

        Logger.Info($"Summoner {target.PlayerId} has died. Resetting summoned players.", "Summoner");

        if (!SummonedPlayerIds.ContainsKey(_Player.PlayerId))
            SummonedPlayerIds[_Player.PlayerId] = [];

        foreach (var summonedId in SummonedPlayerIds[_Player.PlayerId].ToList())
        {
            // PlayerControl summonedPlayer = Main.EnumeratePlayerControls().FirstOrDefault(p => p.PlayerId == summonedId);
            PlayerControl summonedPlayer = GetPlayerById(summonedId);
            if (summonedPlayer != null && summonedPlayer.Is(CustomRoles.Summoned))
            {
                // Restore the player's original role
                RestoreOriginalRole(summonedPlayer);

                // Force sync their death
                Summoned.KillSummonedPlayer(summonedPlayer);
                SummonedHealth.Remove(summonedPlayer.PlayerId); // Remove them from the timer list
                LastUpdateTimes.Remove(summonedPlayer.PlayerId); // Remove the timestamp entry
            }
        }

        // Cancel any pending revives
        CancelPendingRevives();
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        if (!killer.Is(CustomRoles.Summoner)) return true;
        if (Main.PlayerStates[killer.PlayerId].IsRandomizer || Main.PlayerStates[target.PlayerId].IsRandomizer) return true; // Skip if the killer is a Randomizer

        // Prevent killing summoned players or other coven members
        if (target.Is(CustomRoles.Summoned) || target.IsPlayerCovenTeam())
        {
            killer.Notify(ColorString(GetRoleColor(CustomRoles.Summoner), GetString("CovenDontKillOtherCoven")));
            return false; // Cancel the kill
        }

        return true; // Allow the kill otherwise
    }
    public override string GetProgressText(byte playerId, bool comms)
    {

        if (!HasAbilityUses.GetBool()) return string.Empty;

        int maxSummons = MaxSummonsAllowed.GetInt();
        float remainingSummons = playerId.GetAbilityUseLimit();
        Color color = remainingSummons > 0 ? Color.green : Color.red;

        return ColorString(color, $"{remainingSummons}/{maxSummons}");
    }

    private void OnPlayerDead(PlayerControl killer, PlayerControl deadPlayer, bool inMeeting)
    {
        if (killer.Is(CustomRoles.Summoned))
        {
            SummonedKillCounts[killer.PlayerId]++;
            if (SummonedKillsCountToSummoner.GetBool())
            {
                deadPlayer.SetRealKiller(_Player);
            }
        }
        if (deadPlayer.Is(CustomRoles.Summoned))
        {
            RestoreOriginalRole(deadPlayer);
        }
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo bodyInfo)
    {
        foreach (var player in Main.EnumeratePlayerControls())
        {
            if (player.Is(CustomRoles.Summoned))
            {
                Logger.Info($"Meeting started, killing summoned player {player.GetRealName()}.", "Summoner");
                Summoned.KillSummonedPlayer(player);
                _ = new LateTask(() =>
                {
                    RestoreOriginalRole(player); // Restore their original role on report
                }, .2f, "SummonerRestoreROle");
                SummonedHealth.Remove(player.PlayerId); // Remove them from the timer list
                LastUpdateTimes.Remove(player.PlayerId); // Remove the timestamp entry
            }
        }
    }

    private static void PerformRevive(PlayerControl summoner, PlayerControl targetPlayer, float reviveDelay)
    {
        if (targetPlayer.IsAlive()) return;

        _ = new LateTask(() =>
        {
            if (targetPlayer.IsAlive()) return;

            // Revive the player
            targetPlayer.RpcRevive();
            _ = new LateTask(() =>
            {
                targetPlayer.RpcRandomVentTeleport();
            }, 1f, "SummonerReviveTeleport");

            // Save their original role
            SaveOriginalRole(targetPlayer);

            // Assign Summoned role
            targetPlayer.RpcChangeRoleBasis(CustomRoles.Summoned);
            targetPlayer.RpcSetCustomRole(CustomRoles.Summoned);

            if (!SummonedPlayerIds.ContainsKey(summoner.PlayerId))
                SummonedPlayerIds[summoner.PlayerId] = [];

            SummonedPlayerIds[summoner.PlayerId].Add(targetPlayer.PlayerId);
            if (!SummonedKillCounts.ContainsKey(targetPlayer.PlayerId))
            {
                SummonedKillCounts[targetPlayer.PlayerId] = 0;
            }

            // Initialize tasks and cooldowns
            var playerState = Main.PlayerStates[targetPlayer.PlayerId];
            //playerState.InitTask(targetPlayer);
            playerState.ResetSubRoles();

            targetPlayer.SetKillCooldownV3(KillCooldownOption.GetFloat());
            targetPlayer.ResetKillCooldown();
            targetPlayer.SyncSettings();

            Logger.Info($"Player {targetPlayer.PlayerId} revived and assigned Summoned role.", "Summoner");

        }, reviveDelay, "SummonerRevive");
    }
    public static bool CheckWinCondition(byte playerId)
    {
        if (SummonedKillCounts.ContainsKey(playerId))
        {
            if (SummonedKillRequirement.GetInt() == 0)
            {
                return true;
            }
            if (SummonedKillCounts[playerId] >= SummonedKillRequirement.GetInt())
            {
                return true;
            }
        }
        return false;
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

    public override void AfterMeetingTasks()
    {
        // Reset the summoning flag for the next meeting
        HasSummonedThisMeeting[_Player.PlayerId] = false;

        // Process all pending revives
        foreach (var (player, delay) in PendingRevives.ToList())
        {
            PerformRevive(_Player, player, delay);
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
                return CustomRoles.Summoner.ToColoredString();
            if (target.Is(CustomRoles.Summoned))
                return CustomRoles.Summoned.ToColoredString();
        }

        // Default behavior
        return string.Empty;
    }
}
internal class Summoned : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Summoned;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //================================================================\\


    // Dictionaries to track remaining time and last update time for each player
    private readonly Dictionary<byte, int> PlayerDie = new(); // Tracks remaining time (in seconds)
    private readonly Dictionary<byte, long> LastTime = new(); // Tracks the last update time (timestamp)

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Summoner.KillCooldownOption.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;

    public override void Add(byte playerId)
    {
        base.Add(playerId);
        // Initialize timer for the summoned player
        if (!PlayerDie.ContainsKey(playerId))
        {
            int deathTimer = Mathf.RoundToInt(Summoner.DeathTimerOption?.GetFloat() ?? 40f);
            PlayerDie[playerId] = deathTimer;
        }

        LastTime[playerId] = GetTimeStamp(); // Set the initial timestamp
        NotifySummonedHealth(playerId); // Notify player of their timer
    }

    public override void Init()
    {
        base.Init(); // Call base logic
        PlayerDie.Clear();
        LastTime.Clear();
    }


    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (lowLoad || GameStates.IsMeeting || player.Data.IsDead || !player.IsAlive()) return; // Skip if low-load or during meetings

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
                    foreach (var pc in Main.EnumeratePlayerControls())
                    {
                        if (pc.GetRoleClass() is Summoner summonerInstance)
                        {
                            _ = new LateTask(() =>
                            {
                                Summoner.RestoreOriginalRole(player); // Restore their original role on report
                            }, .2f, "SummonerRestoreROle");
                        }
                    }
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
    private static void NotifySummonedHealth(byte playerId)
    {
        if (Summoner.SummonedHealth.TryGetValue(playerId, out var timeRemaining) && timeRemaining > 0)
        {
            var player = Main.EnumeratePlayerControls().FirstOrDefault(p => p.PlayerId == playerId);
            if (player != null)
            {
                player.Notify(ColorString(GetRoleColor(CustomRoles.Summoned), string.Format(GetString("Summoned.TimeRemaining"), timeRemaining)));
            }
        }
    }

    // Kill the summoned player when their timer runs out

    public static void KillSummonedPlayer(PlayerControl player)
    {
        if (player == null || player.Data == null) return;

        // Set the player's death reason and mark them as dead
        player.SetDeathReason(PlayerState.DeathReason.Expired);

        // Use RpcExileV2 to remove the player without leaving a body
        player.RpcExileV2();
        player.Data.IsDead = true;
        player.Data.MarkDirty();
        Main.PlayerStates[player.PlayerId].SetDead();
        
        /*
        player.RpcTeleport(ExtendedPlayerControl.GetBlackRoomPosition());
        player.RpcMurderPlayer(player);
        */
        Logger.Info($"Killing summoned player {player.GetRealName()}", "Summoned");
    }

    public override string GetProgressText(byte playerId, bool comms)
    {
        int killRequirement = Summoner.SummonedKillRequirement.GetInt();
        if (killRequirement == 0) return string.Empty;

        if (Main.PlayerStates[playerId].RoleClass is not Summoned summonedInstance) return string.Empty;

        int kills = Summoner.SummonedKillCounts[playerId];
        Color color = kills >= killRequirement ? Color.green : Color.red;

        return ColorString(color, $"({kills}/{killRequirement})");
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.Is(CustomRoles.Summoned) && (target.Is(CustomRoles.Summoner) || target.Is(CustomRoles.Summoned) || target.IsPlayerCovenTeam()))
        {
            killer.Notify(ColorString(GetRoleColor(CustomRoles.Summoner), GetString("CovenDontKillOtherCoven")));
            return false; // Cancel the kill
        }

        return true; // Allow other kills
    }
    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        foreach (var pc in Main.EnumeratePlayerControls())
        {
            if (pc.GetRoleClass() is Summoner summonerInstance)
            {
                _ = new LateTask(() =>
                {
                    Summoner.RestoreOriginalRole(pc); // Restore their original role on report
                }, .2f, "SummonerRestoreROle");
            }
        }
        Summoner.SummonedHealth.Remove(target.PlayerId); // Remove them from the timer list
        Summoner.LastUpdateTimes.Remove(target.PlayerId); // Remove the timestamp entry
    }
}