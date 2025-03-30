using AmongUs.GameOptions;
using Hazel;
using TOHE.Modules;
using TOHE.Modules.ChatManager;
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
    private const int Id = 31800;
    public override bool IsDesyncRole => true;
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
    public static OptionItem SummonedKillsCountToSummoner;
    public static OptionItem NoMeetingWhileSummoned;
    private static OptionItem HasAbilityUses;
    private static OptionItem MaxSummonsAllowed;

    private readonly Dictionary<byte, RoleBase> SummonedOriginalRoles = new();

    private readonly List<byte> SummonedPlayerIds = new List<byte>();

    public static readonly Dictionary<byte, int> SummonedKillCounts = new();
    public static readonly Dictionary<byte, float> SummonedHealth = new();
    private static List<(PlayerControl, float)> PendingRevives = new();
    public static readonly Dictionary<byte, long> LastUpdateTimes = new();

    private bool HasSummonedThisMeeting = false;
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
        KillCooldownOption = FloatOptionItem.Create(Id + 12, "SummonerSettings.SummonedKillCooldown", new(5f, 60f, 1f), 15f, TabGroup.CovenRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Summoner])
        .SetValueFormat(OptionFormat.Seconds);

        NecroKillCooldownOption = FloatOptionItem.Create(Id + 19, "SummonerSettings.SummonerKillCooldown", new(5f, 60f, 1f), 15f, TabGroup.CovenRoles, false)
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
        var playerState = Main.PlayerStates[playerId];
        playerState.SetMainRole(CustomRoles.Summoner);
        playerState.IsSummoner = true;
        playerId.SetAbilityUseLimit(MaxSummonsAllowed.GetInt());
        CustomRoleManager.CheckDeadBodyOthers.Add(OnPlayerDead);
    }

    public override void Init()
    {
        SummonedHealth.Clear();
        LastUpdateTimes.Clear();
        SummonedOriginalRoles.Clear();
        SummonedPlayerIds.Clear();
        PendingRevives.Clear();
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
        if (Main.AllPlayerControls.Any(player => player.Is(CustomRoles.Summoned)))
        {
            reporter.Notify(ColorString(GetRoleColor(CustomRoles.Summoner), GetString("Summoner.NoMeetingWhileSummoned")), 20f);
            return false; // Prevent starting the meeting
        }
        return true;
    }
    public static bool SummonerCheckMsg(PlayerControl pc, string msg, bool isUI = false)
    {
        if (!AmongUsClient.Instance.AmHost) return false; // Skip if system message or not host
        if (!GameStates.IsMeeting || pc == null || GameStates.IsExilling) return false; // Only during meetings
        if (!pc.Is(CustomRoles.Summoner) || !(pc.GetRoleClass() is Summoner summonerInstance)) return false;

        msg = msg.ToLower().Trim();
        Logger.Info($"Received command: {msg} from {pc.PlayerId}, Host: {AmongUsClient.Instance.AmHost}", "Summoner");

        if (!CheckCommand(ref msg, "summon|sm")) return false;

        // Always try hiding the message if the command is intercepted
        HideSummonCommand();
        ChatManager.SendPreviousMessagesToAll();

        if (!pc.IsAlive())
        {
            Logger.Warn("Summoner is dead and cannot use commands.", "Summoner");
            SendMessage(GetString("Summoner.SummonerDead"), pc.PlayerId, CustomRoles.Summoner.ToColoredString().ToUpper());
            return true;
        }

        if (!byte.TryParse(msg, out var targetId))
        {
            Logger.Warn("Invalid target ID for /summon command.", "Summoner");
            SendMessage(GetString("Summoner.InvalidID"), pc.PlayerId, CustomRoles.Summoner.ToColoredString().ToUpper());
            return true;
        }

        PlayerControl targetPlayer = Main.AllPlayerControls.FirstOrDefault(player => player.PlayerId == targetId);

        if (targetPlayer == null)
        {
            Logger.Warn("Target player is invalid or does not exist.", "Summoner");
            SendMessage(GetString("Summoner.NullPlayer"), pc.PlayerId, CustomRoles.Summoner.ToColoredString().ToUpper());
            return true;
        }

        bool isAlreadySummoned = summonerInstance.SummonedPlayerIds.Contains(targetPlayer.PlayerId);
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
                SendMessage(string.Format(GetString("Summoner.SummonAnnouncementAgain"), targetPlayer.GetRealName()), byte.MaxValue, CustomRoles.Summoner.ToColoredString().ToUpper());
            }

            return true;
        }

        // Check summon use limit
        if (HasAbilityUses.GetBool() && pc.GetAbilityUseLimit() <= 0)
        {
            SendMessage(GetString("Summoner.NoUses"), pc.PlayerId, CustomRoles.Summoner.ToColoredString().ToUpper());
            return true;
        }

        if (targetPlayer.IsAlive())
        {
            Logger.Warn($"{targetPlayer.GetRealName()} is already alive.", "Summoner");
            SendMessage(GetString("Summoner.PlayerAlive"), pc.PlayerId, CustomRoles.Summoner.ToColoredString().ToUpper());
            return true;
        }

        // Check if Summoner has already summoned this meeting
        if (summonerInstance.HasSummonedThisMeeting)
        {
            Logger.Warn("Summoner has already summoned a player this meeting.", "Summoner");
            SendMessage(GetString("Summoner.SummonedThisMeeting"), pc.PlayerId, CustomRoles.Summoner.ToColoredString().ToUpper());
            return true;
        }

        // Normal summoning logic
        HideSummonCommand();
        ChatManager.SendPreviousMessagesToAll();
        summonerInstance.RevivePlayer(targetPlayer);

        // Increment summon count if allowed
        if (!isAlreadySummoned || !allowResummoning)
        {
            pc.RpcRemoveAbilityUse();
        }

        summonerInstance.HasSummonedThisMeeting = true;

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
        return true; // Suppress the command message
    }

    private static void HideSummonCommand()
    {
        ChatUpdatePatch.DoBlockChat = true;
        if (ChatManager.quickChatSpamMode != QuickChatSpamMode.QuickChatSpam_Disabled)
        {
            ChatManager.SendQuickChatSpam();
            ChatUpdatePatch.DoBlockChat = false;
            return;
        }

        string[] decoyCommands = { "/summon", "/sm" };
        var random = IRandom.Instance;

        for (int i = 0; i < 20; i++)
        {
            string decoyMessage = decoyCommands[random.Next(0, decoyCommands.Length)];


            var randomPlayer = Main.AllAlivePlayerControls.RandomElement();

            // Add the decoy message to the chat
            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(randomPlayer, decoyMessage);


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


    public static bool CheckCommand(ref string msg, string command)
    {
        var comList = command.Split('|');
        foreach (var comm in comList)
        {
            if (msg.StartsWith("/" + comm))
            {
                msg = msg.Replace("/" + comm, string.Empty).Trim();
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
    public void CancelPendingRevives()
    {
        foreach (var (player, delay) in PendingRevives.ToList())
        {
            PendingRevives.Remove((player, delay));
        }
    }
    private void SaveOriginalRole(PlayerControl player)
    {
        if (!SummonedOriginalRoles.ContainsKey(player.PlayerId))
        {
            var originalRole = player.GetRoleClass();
            SummonedOriginalRoles[player.PlayerId] = originalRole;
            Logger.Info($"Saved original role for player {player.PlayerId}: {originalRole}.", "Summoner");
        }
    }
    public void RestoreOriginalRole(PlayerControl player)
    {
        if (SummonedOriginalRoles.TryGetValue(player.PlayerId, out RoleBase originalRole))
        {
            if (player.IsAlive()) player.RpcChangeRoleBasis(originalRole.ThisRoleBase);
            else { 
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

        foreach (var summonedId in SummonedPlayerIds.ToList())
        {
            // PlayerControl summonedPlayer = Main.AllPlayerControls.FirstOrDefault(p => p.PlayerId == summonedId);
            PlayerControl summonedPlayer = GetPlayerById(summonedId);
            if (summonedPlayer != null)
            {
                // Restore the player's original role
                RestoreOriginalRole(summonedPlayer);

                // Teleport them to the black room
                summonedPlayer.RpcTeleport(ExtendedPlayerControl.GetBlackRoomPosition());

                // Force sync their death
                Summoned.KillSummonedPlayer(summonedPlayer);

            }
        }

        // Cancel any pending revives
        CancelPendingRevives();

        // Clear tracking
        SummonedPlayerIds.Clear();
        SummonedOriginalRoles.Clear();
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!killer.Is(CustomRoles.Summoner)) return true;

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
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player.Is(CustomRoles.Summoned) && player.IsAlive())
            {
                Summoned.KillSummonedPlayer(player);
                RestoreOriginalRole(player); // Restore their original role on report
            }
        }
    }

    private void PerformRevive(PlayerControl targetPlayer, float reviveDelay)
    {
        if (targetPlayer.IsAlive()) return;

        _ = new LateTask(() =>
        {
            if (targetPlayer.IsAlive()) return;

            // Revive the player
            targetPlayer.RpcRevive();
            targetPlayer.RpcRandomVentTeleport();

            // Save their original role
            SaveOriginalRole(targetPlayer);

            // Assign Summoned role
            targetPlayer.RpcChangeRoleBasis(CustomRoles.Summoned);
            targetPlayer.RpcSetCustomRole(CustomRoles.Summoned);
            SummonedPlayerIds.Add(targetPlayer.PlayerId);
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
        var playerState = Main.PlayerStates[playerId];
        playerState.SetMainRole(CustomRoles.Summoned);
        playerState.IsSummoned = true;

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
        if ((lowLoad || GameStates.IsMeeting) || player.Data.IsDead) return; // Skip if low-load or during meetings

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
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (pc.GetRoleClass() is Summoner summonerInstance)
                        {
                            summonerInstance.RestoreOriginalRole(player);
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
    private void NotifySummonedHealth(byte playerId)
    {
        if (Summoner.SummonedHealth.TryGetValue(playerId, out var timeRemaining) && timeRemaining > 0)
        {
            var player = Main.AllPlayerControls.FirstOrDefault(p => p.PlayerId == playerId);
            if (player != null)
            {
                player.Notify(ColorString(GetRoleColor(CustomRoles.Summoned), $"Time Remaining: {timeRemaining}s"));
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
        foreach (var pc in Main.AllPlayerControls)
        {
            if (pc.GetRoleClass() is Summoner summonerInstance)
            {
                summonerInstance.RestoreOriginalRole(target);
            }
        }
        Summoner.SummonedHealth.Remove(target.PlayerId); // Remove them from the timer list
        Summoner.LastUpdateTimes.Remove(target.PlayerId); // Remove the timestamp entry
    }
}





