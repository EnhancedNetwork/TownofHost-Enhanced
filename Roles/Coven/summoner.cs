using Hazel;
using TOHE.Modules;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Translator;

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
    private static OptionItem HasAbilityUses;
    private static OptionItem MaxSummonsAllowed;
    public static bool HasWon { get; private set; } = false;

    private readonly Dictionary<byte, RoleBase> SummonedOriginalRoles = new();

    private readonly List<byte> SummonedPlayerIds = new List<byte>();

    public static readonly Dictionary<byte, int> SummonedKillCounts = new();
    private static readonly Dictionary<byte, CustomRoles> SavedStates = new();
    public static readonly Dictionary<byte, float> SummonedTimers = new();
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
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        // Check if the seer is Summoner and alive, and the seen player is dead
        if (!seer.IsAlive() || seen.IsAlive()) return string.Empty;

        return ColorString(GetRoleColor(CustomRoles.Summoner), $" {seen.Data.PlayerId}");
    }
    public override bool CanUseKillButton(PlayerControl pc) => HasNecronomicon(pc);

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
        if (HasAbilityUses.GetBool() && pc.GetAbilityUseLimit() <= 0)
        {
            pc.Notify("You have reached the maximum number of summons.");
            return true;
        }

        if (targetPlayer.IsAlive())
        {
            Logger.Warn($"{targetPlayer.GetRealName()} is already alive.", "Summoner");
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
            pc.RpcRemoveAbilityUse();
        }

        summonerInstance.HasSummonedThisMeeting = true;

        // Send global message
        string summonMessage = RevealSummonedPlayer.GetBool()
            ? $"The SUMMONER has brought {targetPlayer.GetRealName()} back to life! \nKillers beware!"
            : "The SUMMONER has brought someone back to life! \nKillers beware!";
        AddMsg(summonMessage, byte.MaxValue, "Summoner Announcement");

        // Send private message to the summoned player if hidden
        if (!RevealSummonedPlayer.GetBool())
        {
            SendMessage(
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


        string[] decoyCommands = { "/summon" };
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

    private static void AddMsg(string message, byte playerId, string title)
    {


        // Send the actual message
        SendMessage(
            ColorString(GetRoleColor(CustomRoles.Summoner), title) + "\n" + message,
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
        return _Player.GetAbilityUseLimit() > 0; // Check remaining summons
    }

    public void SummonPlayer(PlayerControl targetPlayer)
    {
        if (!CanSummon()) return;

        if (targetPlayer == null || targetPlayer.Data == null || targetPlayer.Data.IsDead)
        {
            RevivePlayer(targetPlayer); // Revive first
            SaveOriginalRole(targetPlayer); // Save their original role
            SummonedPlayerIds.Add(targetPlayer.PlayerId); // Track the player ID

            // Assign the Summoned role
            targetPlayer.RpcChangeRoleBasis(CustomRoles.Summoned);
            targetPlayer.RpcSetCustomRole(CustomRoles.Summoned);

            // Initialize role and tasks
            var playerState = Main.PlayerStates[targetPlayer.PlayerId];
            playerState.InitTask(targetPlayer); // Initialize tasks
            targetPlayer.SyncSettings(); // Ensure UI updates
            LastUpdateTimes[targetPlayer.PlayerId] = GetTimeStamp(); // Initialize timer
            SummonedHealth[targetPlayer.PlayerId] = GetDeathTimer(targetPlayer.PlayerId); // Set death timer

            NotifySummonedHealth((PlayerControl)targetPlayer.PlayerId); // Notify the player

            _Player.RpcRemoveAbilityUse();
            Logger.Info($"Summoned player {targetPlayer.PlayerId}. Remaining summons: {_Player.GetAbilityUseLimit()}", "Summoner");
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




    private void RestoreOriginalRole(PlayerControl player)
    {
        if (SummonedOriginalRoles.TryGetValue(player.PlayerId, out RoleBase originalRole))
        {
            player.RpcChangeRoleBasis(originalRole.ThisRoleBase);
            player.RpcSetCustomRole(originalRole.Role);
            Logger.Info($"Restored player {player.PlayerId} to their original role: {originalRole}.", "Summoner");
        }
    }







    public void OnRoleRemove(byte playerId)
    {
        foreach (var summonedId in SummonedPlayerIds.ToList()) // Use ToList to avoid modifying the collection while iterating
        {
            PlayerControl summonedPlayer = Main.AllPlayerControls.FirstOrDefault(p => p.PlayerId == summonedId);
            if (summonedPlayer != null)
            {
                // Restore the player's original role
                RestoreOriginalRole(summonedPlayer);

                // Teleport the player and handle their death
                summonedPlayer.RpcTeleport(ExtendedPlayerControl.GetBlackRoomPosition());
                summonedPlayer.RpcMurderPlayer(summonedPlayer);
            }
        }

        Logger.Info($"Summoner with player ID {playerId} removed, and all summoned players reset.", "Summoner");
    }


    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (!target.Is(CustomRoles.Summoner)) return;

        Logger.Info($"Summoner {target.PlayerId} has died. Resetting summoned players.", "Summoner");

        foreach (var summonedId in SummonedPlayerIds.ToList())
        {
            PlayerControl summonedPlayer = Main.AllPlayerControls.FirstOrDefault(p => p.PlayerId == summonedId);
            if (summonedPlayer != null)
            {
                // Restore the player's original role
                RestoreOriginalRole(summonedPlayer);

                // Teleport them to the black room
                summonedPlayer.RpcTeleport(ExtendedPlayerControl.GetBlackRoomPosition());

                // Force sync their death
                summonedPlayer.RpcMurderPlayer(summonedPlayer);
            }
        }

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
    /* Unsure why this is needed at all
    public override void OnMurderPlayerAsKiller(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (!killer.Is(CustomRoles.Summoner)) return;



        // Reset the kill cooldown to NecroKillCooldownOption if Summoner has the Necronomicon
        if (HasNecronomicon(killer))
        {
            killer.SetKillCooldown(NecroKillCooldownOption.GetFloat());
            Logger.Info($"Summoner {killer.PlayerId} reset kill cooldown to {NecroKillCooldownOption.GetFloat()} seconds.", "Summoner");
        }

        Logger.Info($"Summoner {killer.PlayerId} killed player {target.PlayerId}.", "Summoner");
    }
    */


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

            if (killer.GetRoleClass() is Summoned summoned)
            {
                summoned.NumKills = SummonedKillCounts[killer.PlayerId];
            }
        }
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

        _ = new LateTask(() =>
        {
            if (targetPlayer.IsAlive()) return;

            // Revive the player
            targetPlayer.RpcRevive();

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
            playerState.InitTask(targetPlayer);
            playerState.ResetSubRoles();

            targetPlayer.ResetKillCooldown();
            targetPlayer.SyncSettings();

            Logger.Info($"Player {targetPlayer.PlayerId} revived and assigned Summoned role.", "Summoner");

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
        return DeathTimerOption?.GetFloat() ?? 40f; // Default to 40 seconds if not set
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
            player.Notify(ColorString(GetRoleColor(CustomRoles.Summoned), $"Time Remaining: {health}s"));
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
                return CustomRoles.Summoner.ToColoredString();
            if (target.Is(CustomRoles.Summoned))
                return CustomRoles.Summoned.ToColoredString();
        }

        // Default behavior
        return string.Empty;
    }
}




