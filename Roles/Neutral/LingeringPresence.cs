using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using InnerNet;
using System;
using TOHE.Roles.Core;
using UnityEngine;
using UnityEngine.Playables;
using static TOHE.Options;


namespace TOHE.Roles.Neutral
{
    internal class LingeringPresence : RoleBase
    {
        //===========================SETUP================================\\
        private const int Id = 63500;
        public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.LingeringPresence);

        public override bool IsExperimental => true;
        public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
        public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
        //==================================================================\\

        private static OptionItem SoulDrainRange;
        private static OptionItem SoulDrainSpeed;
        private static OptionItem TaskRechargeAmount;
        private static OptionItem KillRechargeAmount;
        private static OptionItem WinConditionOption;
        public static OptionItem LingeringPresenceShortTasks;
        public static OptionItem LingeringPresenceLongTasks;


        private static CustomWinner SelectedWinCondition;

        private static readonly Dictionary<byte, float> SoulMeters = new();
        private static readonly Dictionary<byte, long> LastUpdateTimes = new();
        private static readonly Dictionary<int, string> TeamDescriptions = new()
{
    { 0, "Crewmates" },
    { 1, "Neutral" },
    { 2, "Imposter" },
    { 3, "Random" }
};

        // Method to retrieve the descriptive name
        public static string GetWinConditionDescription(int optionValue)
        {
            return TeamDescriptions.TryGetValue(optionValue, out var description) ? description : "Unknown";
        }
#pragma warning disable IDE0052 // Remove unread private members
        private static bool taskResetPatched = false;
#pragma warning restore IDE0052 // Remove unread private members
#pragma warning disable IDE0052 // Remove unread private members
        private static string deathReasonMessage = string.Empty;
#pragma warning restore IDE0052 // Remove unread private members
        private static bool patched = false;
        private static OptionItem SoulDrainTickRate;
#pragma warning disable IDE0052 // Remove unread private members
        private static bool IsReviving = false;
#pragma warning restore IDE0052 // Remove unread private members
        private byte lingeringPlayerId;
        private Dictionary<byte, float> TimeSinceLastTick = new();

        public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute) => !ForRecompute;

        public override void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.LingeringPresence);

            SoulDrainRange = FloatOptionItem.Create(Id + 10, "SoulDrainRange", new(1f, 10f, 0.5f), 5f, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.LingeringPresence])
                .SetValueFormat(OptionFormat.Multiplier);
            SoulDrainSpeed = FloatOptionItem.Create(Id + 11, "SoulDrainSpeed", new(1f, 10f, 0.5f), 2f, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.LingeringPresence])
                .SetValueFormat(OptionFormat.Seconds);
            TaskRechargeAmount = FloatOptionItem.Create(Id + 12, "TaskRechargeAmount", new(1f, 100f, 1f), 25f, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.LingeringPresence])
                .SetValueFormat(OptionFormat.Health);
            KillRechargeAmount = FloatOptionItem.Create(Id + 13, "KillRechargeAmount", new(1f, 100f, 1f), 50f, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.LingeringPresence])
                .SetValueFormat(OptionFormat.Health);
            WinConditionOption = StringOptionItem.Create(Id + 20, "Lingering Presence Win Condition", new[] { "Crewmates", "Neutral", "Impostor", "Random" }, 0, TabGroup.NeutralRoles, false)
    .SetParent(CustomRoleSpawnChances[CustomRoles.LingeringPresence]);


            SoulDrainTickRate = FloatOptionItem.Create(Id + 14, "SoulDrainTickRate", new(0.1f, 5f, 0.1f), 1f, TabGroup.NeutralRoles, false)
       .SetParent(CustomRoleSpawnChances[CustomRoles.LingeringPresence])
       .SetValueFormat(OptionFormat.Seconds);
            OverrideTasksData.Create(Id + 15, TabGroup.NeutralRoles, CustomRoles.LingeringPresence);


        }
        public override void Init()
        {

                    SoulMeters.Clear();
            LastUpdateTimes.Clear();
            IsReviving = false;
            lingeringPlayerId = byte.MaxValue;
            taskResetPatched = false;
            deathReasonMessage = string.Empty;
            SelectedWinCondition = (CustomWinner)WinConditionOption.GetInt();
            if (SelectedWinCondition == CustomWinner.Random)
            {
                SelectedWinCondition = AssignRandomTeam();
                Logger.Info($"Lingering Presence assigned random team: {SelectedWinCondition}", "LingeringPresence");
            }


            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player.Is(CustomRoles.LingeringPresence))
                {
                    KillLingeringPresence(player, player);
                }
            }
        }


#pragma warning disable IDE0044 // Add readonly modifier
        private Custom_RoleType selectedTeam;
#pragma warning restore IDE0044 // Add readonly modifier


        public enum TeamOption
        {
            Crewmates = 0,
            Neutral = 1,
            Imposter = 2,
            Random = 3
        }
        private CustomWinner AssignRandomTeam()
        {
            CustomWinner[] possibleTeams = { CustomWinner.Crewmate, CustomWinner.Impostor, CustomWinner.Neutrals };
            var selectedTeam = possibleTeams[UnityEngine.Random.Range(0, possibleTeams.Length)];
            Logger.Info($"[LingeringPresence] Random team assigned: {selectedTeam}");
            return selectedTeam;
        }








        public static void LingeringPresenceWinCondition(PlayerControl player)
        {
            var playerState = Main.PlayerStates[player.PlayerId];
            if (!playerState.IsLingeringPresence)
            {
                Logger.Warn($"[LingeringPresence] {player.GetNameWithRole()} is not Lingering Presence. Skipping win condition check.");
                return;
            }

            var winCondition = (CustomWinner)WinConditionOption.GetInt();

            switch (winCondition)
            {
                case CustomWinner.Crewmate:
                    if (CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate)
                    {
                        CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                        Logger.Info($"[LingeringPresence] {player.GetNameWithRole()} wins with the Crewmate team.");
                    }
                    break;

                case CustomWinner.Impostor:
                    if (CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor)
                    {
                        CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                        Logger.Info($"[LingeringPresence] {player.GetNameWithRole()} wins with the Impostor team.");
                    }
                    break;

                case CustomWinner.Neutrals:
                    if (IsWinningWithNeutral(player))
                    {
                        CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                        Logger.Info($"[LingeringPresence] {player.GetNameWithRole()} wins with the Neutral team.");
                    }
                    else
                    {
                        Logger.Warn($"[LingeringPresence] {player.GetNameWithRole()} does not meet Neutral win condition.");
                    }
                    break;


                default:
                    Logger.Warn($"[LingeringPresence] {player.GetNameWithRole()} does not meet any win condition.");
                    break;
            }
        }







        public static void OnMeetingCalled()
        {
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player.Is(CustomRoles.LingeringPresence) && player.IsAlive())
                {
                    // Trigger Lingering Presence's custom death mechanics during meetings
                    if (KillLingeringPresence(PlayerControl.LocalPlayer, player))
                    {
                        // Schedule ResetTasks to be called shortly after death handling completes
                        new LateTask(() => LingeringPresence.Instance?.ResetTasks(player), 0.5f, "LingeringPresence ResetTasks After Death");
                    }
                }
            }
        }




        public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo bodyInfo)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player.Is(CustomRoles.LingeringPresence) && player.IsAlive())
                {
                    // Trigger Lingering Presence's custom death mechanics when a body is reported
                    KillLingeringPresence(PlayerControl.LocalPlayer, player);
                }
            }
        }


        public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
        {
            if (target.Is(CustomRoles.LingeringPresence))
            {
                // Ignore protection checks for Lingering Presence
                return KillLingeringPresence(killer, target);
            }

            return true; // For other roles, proceed with default behavior
        }


        public static bool KillLingeringPresence(PlayerControl killer, PlayerControl target)
        {
            try
            {
                if (killer == null || target == null || !target.IsAlive()) return false;

                // Set custom death properties specific to Lingering Presence
                target.SetDeathReason(PlayerState.DeathReason.FadedAway); // Customize death reason
                target.RpcExileV2(); // Exile without leaving a body

                if (Main.PlayerStates.ContainsKey(target.PlayerId))
                {
                    Main.PlayerStates[target.PlayerId].SetDead();
                }

                target.Data.IsDead = true;
                target.SetRealKiller(killer);
                killer.ResetKillCooldown();

                Logger.Info($"{target.GetNameWithRole()} was killed by {killer?.GetNameWithRole()} due to Lingering Presence mechanics (no body)", "LingeringPresence");

                // Successfully handled death; return true
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in KillLingeringPresence: {ex.Message}", "LingeringPresence");
            }

            return false;
        }



        public static void SyncRoleSkillReader(MessageReader reader)
        {
            try
            {
                var pc = reader.ReadNetObject<PlayerControl>();

                if (pc != null)
                {
                    // Read task-related data directly
                    pc.GetRoleClass()?.ReceiveRPC(reader, pc);
                }
            }
            catch (Exception error)
            {
                Logger.Error($"Error in SyncRoleSkillReader RPC: {error}", "SyncRoleSkillReader");
            }
        }








        private string GetWinConditionMessage(CustomWinner winCondition)
        {
            switch (winCondition)
            {
                case CustomWinner.Crewmate: return "You win with the Crewmates!";
                case CustomWinner.Impostor: return "You win with the Impostors!";
                case CustomWinner.Neutrals: return "You win with Neutral roles!";
                default: return "Your team will be randomly assigned!";
            }
        }

        public override void Add(byte playerId)
        {
            // Notify Lingering Presence of their team at game start
            string teamMessage = GetWinConditionMessage((CustomWinner)WinConditionOption.GetInt());
            Utils.SendMessage(teamMessage, playerId);

            PlayerControl player = null;

            // Locate the PlayerControl object for the given playerId
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc.PlayerId == playerId)
                {
                    player = pc;
                    break;
                }
            }

            if (player == null)
            {
                Logger.Error($"Player with ID {playerId} not found for Lingering Presence role.", "LingeringPresence");
                return;
            }

            

            // Initialize soul meters for other players
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (!pc.Is(CustomRoles.LingeringPresence))
                {
                    SoulMeters[pc.PlayerId] = 100f;
                }
               

                // Reset camera list if necessary
                if (!Main.ResetCamPlayerList.Contains(playerId))
                {
                    Main.ResetCamPlayerList.Add(playerId);
                }

                // Only apply OnFixedUpdate for the host
                if (AmongUsClient.Instance.AmHost)
                {
                    CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdate);
                }
            }
        } 


        public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
        {
            if (player == null)

                // Sync task data for modded clients
                SendRPC();

            if (patched)
            {
                ResetTasks(player);
                patched = false; // Reset patched to prevent repeated task resets
            }
            // Check if tasks are complete
            var taskState = player.GetPlayerTaskState();
            if (taskState.IsTaskFinished && player.Data.IsDead)
            {
                Logger.Info("Revival conditions met. Attempting to revive Lingering Presence.", "LingeringPresence");
                ReviveLingeringPresence(player);
                new LateTask(() => ResetTasks(player), 0.5f, "LingeringPresence ResetTasks");

            }

            return true;
        }




        // Sync task status across clients
        private void SendRPC()
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
            var player = Utils.GetPlayerById(lingeringPlayerId);
            var taskState = player?.GetPlayerTaskState();
            writer.WriteNetObject(player);
            writer.Write(taskState?.AllTasksCount ?? 0);
            writer.Write(taskState?.CompletedTasksCount ?? 0);
            writer.Write(lingeringPlayerId);

            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }





        public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
        {
            int allTasksCount = reader.ReadInt32();
            int completedTasksCount = reader.ReadInt32();
            byte playerId = reader.ReadByte();

            var taskState = Utils.GetPlayerById(playerId)?.GetPlayerTaskState();
            if (taskState != null)
            {
                taskState.AllTasksCount = allTasksCount;
                taskState.CompletedTasksCount = completedTasksCount;
            }

            Logger.Info($"Lingering Presence tasks synced for player {playerId}: {completedTasksCount}/{allTasksCount} completed.", "LingeringPresence");
        }







        private static void ReviveLingeringPresence(PlayerControl player)
        {
            if (!player.Data.IsDead) return;

            IsReviving = true;

            // Teleport the player to the spawn position
            Vector3 spawnPosition = new Vector3(0, 0, 0);
            player.RpcTeleport(spawnPosition);

            // Set the player's role back to LingeringPresence and revive
            player.RpcSetRole((AmongUs.GameOptions.RoleTypes)CustomRoles.LingeringPresence);
            player.RpcRevive();

            Logger.Info($"{player.GetNameWithRole()} has been revived at spawn.", "LingeringPresence");



            IsReviving = false;
        }





        public static LingeringPresence Instance { get; private set; }

        public LingeringPresence()
        {
            Instance = this;
        }
        private void SetShortTasksToAdd()
        {

        }

        // Reset tasks for LingeringPresence upon revival
        public void ResetTasks(PlayerControl player)
        {
            // Ensure additional task-related variables are reset, even if no extra tasks are added
            SetShortTasksToAdd(); // Placeholder for consistency; adjust as needed

            var taskState = player.GetPlayerTaskState();
            player.Data.RpcSetTasks(new Il2CppStructArray<byte>(0)); // Clear task list to allow reassignment
            taskState.CompletedTasksCount = 0;

            // Call necessary functions for resetting visuals and states
            player.RpcGuardAndKill();
            player.Notify("Your tasks have been reset!");

            Logger.Info($"{player.GetRealName()}'s tasks reset.", "LingeringPresence");

            // Remove any visual indicators
            Main.AllPlayerControls.Do(x => TargetArrow.Remove(x.PlayerId, player.PlayerId));

            // Reset any flags or variables that track state for task warnings or notifications


            // Sync updated task state across clients, especially for the host
            SendRPC();
        }










        private void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime)

        {
            if (!player.Is(CustomRoles.LingeringPresence) || player.Data.IsDead) return;

            foreach (var target in PlayerControl.AllPlayerControls)
            {
                if (player.PlayerId == target.PlayerId || target.Data.IsDead) continue;

                var distance = Vector3.Distance(player.transform.position, target.transform.position);

                if (distance <= SoulDrainRange.GetFloat())
                {
                    DrainSoul(player, target);
                }
                else
                {
                    ResetSoulDamage(target.PlayerId);
                }
            }
        }





        private void DrainSoul(PlayerControl presence, PlayerControl target)
        {
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (!LastUpdateTimes.TryGetValue(target.PlayerId, out var lastUpdateTime))
            {
                lastUpdateTime = currentTime;
            }

            var deltaTime = currentTime - lastUpdateTime;
            var drainAmount = SoulDrainSpeed.GetFloat() * deltaTime;

            SoulMeters[target.PlayerId] = Mathf.Clamp(SoulMeters[target.PlayerId] - drainAmount, 0f, 100f);

            ApplyFadingLight(target);

            if (SoulMeters[target.PlayerId] <= 0)
            {
                target.SetDeathReason(PlayerState.DeathReason.FadedAway);
                target.RpcMurderPlayer(target);
            }

            LastUpdateTimes[target.PlayerId] = currentTime;
        }

        private void ApplyFadingLight(PlayerControl target)
        {
            Logger.Info($"Updating FadingLight to {target.GetNameWithRole().RemoveHtmlTags()}", "Lingering Presence");

            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.FadingLight), $"Your soul is at {Mathf.RoundToInt(SoulMeters[target.PlayerId])}%"));
            target.RpcSetCustomRole(CustomRoles.FadingLight);
        }

        private void ResetSoulDamage(byte playerId)
        {
            LastUpdateTimes.Remove(playerId);
        }



        public override string GetMarkOthers(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false)
        {
            if (!seer.Is(CustomRoles.LingeringPresence)) return string.Empty;

            target ??= seer;

            if (SoulMeters.TryGetValue(target.PlayerId, out var soulMeter))
            {
                return Utils.ColorString(Color.white, $"Soul: {Mathf.RoundToInt(soulMeter)}%");
            }

            return string.Empty;
        }
        public override void AfterMeetingTasks()
        {
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player.Is(CustomRoles.LingeringPresence) && player.IsAlive())
                {
                    // Delay the reset to give time for the meeting phase to end completely
                    new LateTask(() => ResetTasks(player), 0.5f, "LingeringPresence ResetTasks After Meeting");
                }
            }
        }

        public static void OnOthersTaskComplete(PlayerControl player)
        {
            // Check if the player has the Fading Light add-on
            if (player.HasSpecificSubRole(CustomRoles.FadingLight))
            {
                // Get the host-set task recharge amount
                float rechargeAmount = TaskRechargeAmount.GetFloat();

                // Increase soul level and clamp it to a maximum of 100%
                SoulMeters[player.PlayerId] = Mathf.Clamp(SoulMeters[player.PlayerId] + rechargeAmount, 0f, 100f);

                // Optionally notify the player of their updated soul level
                player.Notify($"Completing a task has increased your soul to {Mathf.RoundToInt(SoulMeters[player.PlayerId])}%.");
            }
        }
        public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
        {
            if (killer.HasSpecificSubRole(CustomRoles.FadingLight))
            {
                // Retrieve the host-set kill recharge amount
                float rechargeAmount = KillRechargeAmount.GetFloat();

                // Increase the soul level for the killer, capping it at 100%
                SoulMeters[killer.PlayerId] = Mathf.Clamp(SoulMeters[killer.PlayerId] + rechargeAmount, 0f, 100f);

                // Optionally notify the killer of their updated soul level
                killer.Notify($"Killing a player has increased your soul to {Mathf.RoundToInt(SoulMeters[killer.PlayerId])}%.");
            }

            // Proceed with the usual behavior for CheckMurderOnOthersTarget
            return base.CheckMurderOnOthersTarget(killer, target);
        }










        public override void Remove(byte playerId)
        {
            SoulMeters.Remove(playerId);
            LastUpdateTimes.Remove(playerId);
            CustomRoleManager.OnFixedUpdateOthers.Remove(OnFixedUpdate);
        }



        private static bool IsWinningWithNeutral(PlayerControl player)
        {
            // Define viable Neutral roles
            var viableNeutralRoles = new[]
            {
            CustomRoles.Agitater,
    CustomRoles.Arsonist,
    CustomRoles.Baker,
    CustomRoles.Bandit,
    CustomRoles.Berserker,
   CustomRoles. BloodKnight,
    CustomRoles.Collector,
    CustomRoles.Cultist,
    CustomRoles.CursedSoul,
    CustomRoles.Death,
    CustomRoles.Demon,
    CustomRoles.Doomsayer,
    CustomRoles.Doppelganger,
    CustomRoles.Executioner,
    CustomRoles.Famine,
    CustomRoles.Glitch,
    CustomRoles.God,
    CustomRoles.HexMaster,
    CustomRoles.Huntsman,
    CustomRoles.Infectious,
    CustomRoles.Innocent,
    CustomRoles.Jackal,
    CustomRoles.Jester,
    CustomRoles.Jinx,
    CustomRoles.Juggernaut,
    CustomRoles.Medusa,
    CustomRoles.Necromancer,
    CustomRoles.Pelican,
    CustomRoles.Pestilence,
    CustomRoles.Pickpocket,
    CustomRoles.Pirate,
    CustomRoles.PlagueBearer,
    CustomRoles.PlagueDoctor,
    CustomRoles.Poisoner,
    CustomRoles.PotionMaster,
    CustomRoles.Provocateur,
    CustomRoles.PunchingBag,
    CustomRoles.Pyromaniac,
    CustomRoles.Quizmaster,
    CustomRoles.Revolutionist,
    CustomRoles.RuthlessRomantic,
    CustomRoles.Seeker,
    CustomRoles.SerialKiller,
    CustomRoles.Shroud,
    CustomRoles.Sidekick,
    CustomRoles.Solsticer,
    CustomRoles.SoulCollector,
    CustomRoles.Spiritcaller,
    CustomRoles.Stalker,
    CustomRoles.Terrorist,
    CustomRoles.Traitor,
    CustomRoles.Troller,
    CustomRoles.Vector,
    CustomRoles.VengefulRomantic,
    CustomRoles.Virus,
    CustomRoles.Vulture,
    CustomRoles.War,
    CustomRoles.Werewolf,
    CustomRoles.Workaholic,
    CustomRoles.Wraith,
    };

            // Check if the winning team is Neutral and contains a viable role
            return CustomWinnerHolder.WinnerTeam == CustomWinner.Neutrals &&
                   CustomWinnerHolder.WinnerRoles.Any(role => viableNeutralRoles.Contains(role));
        }
    }
}