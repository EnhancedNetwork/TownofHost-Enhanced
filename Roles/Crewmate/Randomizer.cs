using AmongUs.GameOptions;
using Il2CppSystem.Configuration;
using MS.Internal.Xml.XPath;
using TOHE;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static UnityEngine.GraphicsBuffer;


namespace TOHE.Roles.Neutral
{
    internal class Randomizer : RoleBase
    {
        //===========================SETUP================================\\
        private const int Id = 82000; // Unique ID for Randomizer
        public static readonly HashSet<byte> playerIdList = new();
        public static bool HasEnabled => playerIdList.Any();
        public override bool IsDesyncRole => true;

        public override CustomRoles ThisRoleBase => CustomRoles.Crewmate; // Base role remains Neutral
        public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralChaos;
        //================================================================\\

        private static readonly Dictionary<CustomRoles, OptionItem> RoleAvailabilityOptions = new();


        private static OptionItem ChanceCrew;
        private static OptionItem ChanceImpostor;
        private static OptionItem ChanceNeutral;

        private static OptionItem AllowGhostRoles;
        private static OptionItem MinAddOns;
        private static OptionItem MaxAddOns;






        public override void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Randomizer);

            // Add Chance Crew Option
            ChanceCrew = IntegerOptionItem.Create(Id + 10, "ChanceCrew", new(0, 100, 5), 40, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Randomizer])
             .SetValueFormat(OptionFormat.Percent);


            // Add Chance Impostor Option
            ChanceImpostor = IntegerOptionItem.Create(Id + 11, "ChanceImpostor", new(0, 100, 5), 40, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Randomizer])
             .SetValueFormat(OptionFormat.Percent);


            // Add Chance Neutral Option
            ChanceNeutral = IntegerOptionItem.Create(Id + 12, "ChanceNeutral", new(0, 100, 5), 20, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Randomizer])
             .SetValueFormat(OptionFormat.Percent);


            AllowGhostRoles = BooleanOptionItem.Create(Id + 20, "AllowGhostRoles", false, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Randomizer]);


            MinAddOns = IntegerOptionItem.Create(Id + 30, "MinAddOns", new(0, 10, 1), 0, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Randomizer]);

            MaxAddOns = IntegerOptionItem.Create(Id + 31, "MaxAddOns", new(0, 10, 1), 3, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Randomizer]);
        }











        public override void Init()
        {
            playerIdList.Clear();

            int crewChance = ChanceCrew.GetInt();
            int impostorChance = ChanceImpostor.GetInt();
            int neutralChance = ChanceNeutral.GetInt();

            int totalChance = crewChance + impostorChance + neutralChance;

            // Check if total chances exceed 100%
            if (totalChance > 100)
            {
                Logger.Warn("Total team chances exceed 100%. Overlap resolution will be applied during role assignment.", "Randomizer");
            }
            else if (totalChance == 0)
            {
                Logger.Warn("All team chances are set to 0. Using default equal distribution.", "Randomizer");
            }

            Logger.Info($"Initialized Randomizer with team chances - Crewmate: {crewChance}%, Impostor: {impostorChance}%, Neutral: {neutralChance}%.", "Randomizer");
        }







        public override void Add(byte playerId)
        {
            if (playerIdList.Contains(playerId)) return; // Avoid duplicates

            playerIdList.Add(playerId);



            var pc = Utils.GetPlayerById(playerId);
            if (pc == null) return;

            // Set Randomizer role
            var playerState = Main.PlayerStates[playerId];
            playerState.SetMainRole(CustomRoles.Randomizer);
            playerState.IsRandomizer = true;

            if (pc.GetCustomRole() != CustomRoles.Randomizer)
            {
                pc.RpcChangeRoleBasis(CustomRoles.Crewmate);
                pc.RpcSetCustomRole(CustomRoles.Randomizer);
            }

            // Notify player
            pc.Notify($"You are the <color=#{Utils.GetRoleColor(CustomRoles.Randomizer)}>Randomizer</color>! Your role will change after each meeting.");
        }


        private void AssignRandomRole(byte playerId)
        {
            var pc = Utils.GetPlayerById(playerId);
            if (pc == null) return;

            // Reset subroles before assigning a new role
            ResetSubRoles(playerId);

            // Get the player's state
            var playerState = Main.PlayerStates[playerId];
            if (playerState == null) return;

            // Determine team lock if not already applied
            if (!playerState.TeamLockApplied)
            {
                CustomRoles randomRole = GetRandomRoleAcrossAllTeams(playerId);

                // Lock the team based on the role
                if (randomRole.IsCrewmate())
                {
                    playerState.IsCrewmateTeam = true;
                    playerState.LockedRoleType = Custom_RoleType.CrewmateBasic; // Lock to Crewmate type
                    Logger.Info($"Randomizer locked to Crewmate team.", "Randomizer");
                }
                else if (randomRole.IsImpostorTeam())
                {
                    playerState.IsImpostorTeam = true;
                    playerState.LockedRoleType = Custom_RoleType.ImpostorVanilla; // Lock to Impostor type
                    Logger.Info($"Randomizer locked to Impostor team.", "Randomizer");
                }
                else if (randomRole.IsNeutral())
                {
                    playerState.IsNeutralTeam = true;
                    playerState.LockedRoleType = Custom_RoleType.NeutralChaos; // Lock to Neutral type
                    Logger.Info($"Randomizer locked to Neutral team.", "Randomizer");
                }

                // Apply the role
                pc.RpcChangeRoleBasis(randomRole); // Update basis to match the new role
                pc.RpcSetCustomRole(randomRole);  // Set the random role
                pc.GetRoleClass()?.OnAdd(playerId); // Initialize the new role logic
                pc.SyncSettings(); // Ensures the player's UI reflects the changes

                // Initialize tasks and abilities
                playerState.InitTask(pc);

                Logger.Info($"Randomizer assigned initial role {randomRole} to player {pc.name}", "Randomizer");
            }
            else
            {
                // If team is already locked, get a new random role
                CustomRoles randomRole = GetRandomRoleAcrossAllTeams(playerId);

                // Apply the new role while maintaining the locked team
                pc.RpcChangeRoleBasis(randomRole); // Update basis to match the new role
                pc.RpcSetCustomRole(randomRole);  // Set the random role
                pc.GetRoleClass()?.OnAdd(playerId); // Initialize the new role logic
                pc.SyncSettings(); // Ensures the player's UI reflects the changes

                // Initialize tasks and abilities
                playerState.InitTask(pc);

                Logger.Info($"Randomizer assigned new role {randomRole} to player {pc.name} (team locked to {playerState.LockedRoleType})", "Randomizer");
            }
        }


        public static void RandomizerWinCondition(PlayerControl pc)
        {
            if (pc == null) return;

            var playerState = Main.PlayerStates[pc.PlayerId];
            if (!playerState.IsRandomizer || !playerState.TeamLockApplied)
            {
                Logger.Warn($"Randomizer {pc.name} has no team lock applied or is not a Randomizer. Skipping win condition check.", "Randomizer");
                return;
            }

            // Alive players' win conditions
            switch (playerState.RandomizerWinCondition)
            {
                case Custom_Team.Crewmate:
                    if (CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate && pc.IsAlive())
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Randomizer);
                        Logger.Info($"Randomizer {pc.name} (alive) wins with the Crewmate team.", "Randomizer");
                    }
                    break;

                case Custom_Team.Impostor:
                    if (CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor && pc.IsAlive())
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Randomizer);
                        Logger.Info($"Randomizer {pc.name} (alive) wins with the Impostor team.", "Randomizer");
                    }
                    break;

                case Custom_Team.Neutral:
                    if (pc.IsAlive())
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Randomizer);
                        Logger.Info($"Randomizer {pc.name} wins as a Neutral player (alive).", "Randomizer");
                    }
                    break;

                default:
                    Logger.Warn($"Randomizer {pc.name} has an unknown or invalid win condition: {playerState.RandomizerWinCondition}.", "Randomizer");
                    break;
            }

            // Dead players' win conditions
            if (!pc.IsAlive())
            {
                if (playerState.LockedTeam == Custom_Team.Crewmate && CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate)
                {
                    CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                    CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Randomizer);
                    Logger.Info($"Randomizer {pc.name} (dead) wins with the Crewmate team.", "Randomizer");
                }
                else if (playerState.LockedTeam == Custom_Team.Impostor && CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor)
                {
                    CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                    CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Randomizer);
                    Logger.Info($"Randomizer {pc.name} (dead) wins with the Impostor team.", "Randomizer");
                }
            }
        }










        private static Custom_Team DetermineTeam()
        {
            int crewChance = ChanceCrew.GetInt();
            int impostorChance = ChanceImpostor.GetInt();
            int neutralChance = ChanceNeutral.GetInt();

            int totalChance = crewChance + impostorChance + neutralChance;

            // Handle all chances set to 0 or if total chance is 0
            if (totalChance == 0)
            {
                Logger.Warn("All team chances are set to 0. Running default overlap with equal chances.", "DetermineTeam");
                return ResolveOverlap(new[] { Custom_Team.Crewmate, Custom_Team.Impostor, Custom_Team.Neutral });
            }

            int roll = UnityEngine.Random.Range(0, totalChance);

            // Check for overlapping chances
            List<Custom_Team> overlappingTeams = new();

            if (roll < crewChance) overlappingTeams.Add(Custom_Team.Crewmate);
            if (roll < crewChance + impostorChance && roll >= crewChance) overlappingTeams.Add(Custom_Team.Impostor);
            if (roll >= crewChance + impostorChance) overlappingTeams.Add(Custom_Team.Neutral);

            // Handle overlap dynamically
            if (overlappingTeams.Count > 1)
            {
                Logger.Warn($"Chance overlap detected for teams: {string.Join(", ", overlappingTeams)}. Resolving overlap...", "DetermineTeam");
                return ResolveOverlap(overlappingTeams);
            }

            // Return the determined team if no overlap
            if (roll < crewChance) return Custom_Team.Crewmate;
            if (roll < crewChance + impostorChance) return Custom_Team.Impostor;
            return Custom_Team.Neutral;
        }
        private static Custom_Team ResolveOverlap(IEnumerable<Custom_Team> overlappingTeams)
        {
            var teams = overlappingTeams.ToList();
            int roll = UnityEngine.Random.Range(0, teams.Count);

            Logger.Info($"Resolved overlap. Selected team: {teams[roll]}", "ResolveOverlap");
            return teams[roll];
        }

        private static void NotifyRoleChange(PlayerControl pc, CustomRoles newRole)
        {
            var playerState = Main.PlayerStates[pc.PlayerId];

            // Get a list of add-ons for the player
            var addOns = string.Join(", ", playerState.SubRoles.Select(addOn => Utils.GetRoleName(addOn)));

            // Notify Randomizer about its role and add-ons
            string message = $"You are still the Randomizer! Your current role is {Utils.GetRoleName(newRole)}";
            if (!string.IsNullOrEmpty(addOns))
            {
                message += $" with the following add-ons: {addOns}.";
            }

            pc.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Randomizer), message));
        }
        private static bool IsRoleEnabled(CustomRoles role)
        {
            if (RoleAvailabilityOptions.TryGetValue(role, out var option))
            {
                return option.GetBool();
            }
            return true; // Default to enabled if not explicitly configured
        }
        private static CustomRoles GetGhostRole(byte playerId)
        {
            if (!GhostRolesList.Any())
            {
                Logger.Warn("No ghost roles available. Defaulting to a fallback role.", "Randomizer");
                return CustomRoles.CrewmateTOHE; // Default fallback if the list is empty
            }

            // Select a random ghost role
            CustomRoles selectedRole = GhostRolesList[UnityEngine.Random.Range(0, GhostRolesList.Count)];
            Logger.Info($"Assigned ghost role {selectedRole} to player ID {playerId}.", "Randomizer");
            return selectedRole;
        }


       
        

        
            private static CustomRoles GetRandomRoleAcrossAllTeams(byte playerId)
        {
            var pc = Utils.GetPlayerById(playerId);
            if (pc == null) return CustomRoles.CrewmateTOHE; // Default fallback

            var playerState = Main.PlayerStates[playerId];

            // Determine team based on percentage chances
            var team = DetermineTeam();

            List<CustomRoles> availableRoles = team switch
            {
                Custom_Team.Crewmate => GetAllCrewmateRoles(),
                Custom_Team.Impostor => GetAllImpostorRoles(),
                Custom_Team.Neutral => GetAllNeutralRoles(),
                _ => new List<CustomRoles>() // Default empty list
            };

            if (!availableRoles.Any())
            {
                Logger.Error("Available roles list is empty for the determined team. Defaulting to Crewmate.", "Randomizer");
                return CustomRoles.CrewmateTOHE; // Fallback role
            }

            // Select a random role from the available roles
            var selectedRole = availableRoles[UnityEngine.Random.Range(0, availableRoles.Count)];

            // Lock the team if not already locked
            if (!playerState.TeamLockApplied)
            {
                playerState.TeamLockApplied = true;
                playerState.IsCrewmateTeam = team == Custom_Team.Crewmate;
                playerState.IsImpostorTeam = team == Custom_Team.Impostor;
                playerState.IsNeutralTeam = team == Custom_Team.Neutral;

                Logger.Info($"Randomizer locked to {team} team.", "Randomizer");
            }

            Logger.Info($"Randomizer assigned role: {selectedRole}", "Randomizer");
            return selectedRole;
        }
        public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
        {

            var playerState = Main.PlayerStates[target.PlayerId];
            var isRandomizer = playerState.IsRandomizer;

            if (!isRandomizer)
            {
                Logger.Info($"Player {target.name} is not Randomizer. Skipping ghost role assignment.", "Randomizer");
                return false; // Continue with default behavior
            }

            Logger.Info($"Randomizer {target.name} is being killed by {killer.name}. Checking ghost role settings...", "Randomizer");

            // Check if ghost roles are allowed
            if (AllowGhostRoles.GetBool())
            {
                Logger.Info($"Ghost roles are enabled. Assigning GuardianAngel base to {target.name}.", "Randomizer");
                
                // Assign GuardianAngel base and link the role
                var newGhostRole = GetGhostRole(target.PlayerId);
                target.RpcChangeRoleBasis(CustomRoles.GuardianAngel); // Change to GuardianAngel base
                target.RpcSetCustomRole(newGhostRole); // Assign the selected ghost role
                target.GetRoleClass()?.OnAdd(target.PlayerId); // Initialize the role class
                target.SyncSettings(); // Sync role settings
                playerState.IsDead = true; // Ensure the player is marked as a ghost

                Logger.Info($"Assigned ghost role {newGhostRole} to Randomizer {target.name}.", "Randomizer");
                return true; // Prevent further processing of the murder
            }
            else
            {
                Logger.Info($"Ghost roles are disabled. Keeping Randomizer {target.name} as a standard dead player.", "Randomizer");
                return true; // Allow the murder to proceed without ghost role assignment
            }
        }


        public static void RandomizerGhost(PlayerControl pc)
        {
            var playerState = Main.PlayerStates[pc.PlayerId];

          

            Logger.Info($"Applying RandomizerGhost logic to {pc.name}.", "Randomizer");

            // Teleport player outside the map
            pc.RpcTeleport(ExtendedPlayerControl.GetBlackRoomPosition());

            // Schedule a task to "kill" the player
            _ = new LateTask(
                () =>
                {
                    // Kill the player
                    pc.RpcMurderPlayer(pc);
                    playerState.IsDead = true;

                    // Sync death information
                    pc.SetRealKiller(pc); // Set self as killer (or null if unnecessary)
                    Logger.Info($"{pc.name} has been killed and marked as a ghost Randomizer.", "Randomizer");

                    // Teleport back to spawn
                    _ = new LateTask(
                        () =>
                        {
                            Vector3 spawnPosition = new Vector3(0, 0, 0);
                            pc.RpcTeleport(spawnPosition);
                            Logger.Info($"{pc.name} teleported back to spawn as a ghost Randomizer.", "Randomizer");
                        },
                        1f, // Delay to ensure smooth transition
                        "TeleportBackToSpawn"
                    );
                },
                0.5f, // Delay before killing
                "RandomizerKillTask"
            );
        }


        public static void AfterMeetingTasks()
        {
            foreach (var playerId in playerIdList.ToList())
            {
                var pc = Utils.GetPlayerById(playerId);
                if (pc == null) continue;

                // Reset subroles
                Main.PlayerStates[playerId].ResetSubRoles();
                pc.GetRoleClass()?.OnRemove(pc.PlayerId);

                // Determine role assignment based on player's alive status
                CustomRoles newRole;
                if (!pc.IsAlive())
                {
                    if (AllowGhostRoles.GetBool()) // Ghost roles are enabled
                    {
                        Logger.Info($"Randomizer {pc.name} is dead. Assigning a new ghost role.", "Randomizer");

                        // Assign ghost role
                        newRole = GetGhostRole(pc.PlayerId);
                        pc.RpcChangeRoleBasis(CustomRoles.GuardianAngel); // Set base role
                        pc.RpcSetCustomRole(newRole); // Set the custom ghost role
                        pc.GetRoleClass()?.OnAdd(pc.PlayerId); // Initialize role
                        pc.SyncSettings(); // Sync the role settings
                        RandomizerGhost(pc);
                        // Explicitly set dead state
                        Main.PlayerStates[pc.PlayerId].IsDead = true; // Mark as dead
                    

                        Logger.Info($"Randomizer {pc.name} has been marked as dead and assigned the ghost role: {newRole}.", "Randomizer");
                    }


                    else
                    {
                        // Ghost roles are disabled, do nothing to avoid reviving
                        Logger.Info($"Randomizer {pc.name} is dead. Ghost roles are disabled. No role changes applied.", "Randomizer");
                        continue;
                    }
                }
                else if (pc.IsAlive()) // Player is alive, assign a normal role
                {
                    Logger.Info($"Randomizer {pc.name} is alive. Assigning a normal role.", "Randomizer");
                    newRole = GetRandomRoleAcrossAllTeams(playerId); // Assign from the normal role pool
                }
                else
                {
                    Logger.Warn($"Randomizer {pc.name} could not be assigned a role. Defaulting to Crewmate.", "Randomizer");
                    newRole = CustomRoles.CrewmateTOHE; // Fallback in case of unexpected condition
                }

                Logger.Info($"Assigning role {newRole} to player {pc.name}", "Randomizer");

                // Update the player's role
                pc.RpcChangeRoleBasis(newRole); // Update the role basis
                pc.RpcSetCustomRole(newRole);  // Set the actual role

                // Preserve Randomizer flag
                var playerState = Main.PlayerStates[playerId];
                playerState.IsRandomizer = true;

                // Notify the player after the role is finalized
                NotifyRoleChange(pc, newRole);

                // Assign random add-ons (only for alive players)
                if (pc.IsAlive())
                {
                    // Retrieve the min and max add-on settings
                    int minAddOns = MinAddOns.GetInt();
                    int maxAddOns = MaxAddOns.GetInt();

                    // Ensure maxAddOns is not less than minAddOns
                    if (maxAddOns < minAddOns)
                    {
                        Logger.Warn($"Max Add-Ons ({maxAddOns}) is less than Min Add-Ons ({minAddOns}). Using Min Add-Ons.", "Randomizer");
                        maxAddOns = minAddOns;
                    }

                    // Randomly determine the number of add-ons to assign
                    int addOnCount = UnityEngine.Random.Range(minAddOns, maxAddOns + 1);
                    List<CustomRoles> selectedAddOns = GetAvailableAddOns()
                        .OrderBy(_ => UnityEngine.Random.value)
                        .Take(addOnCount)
                        .ToList();

                    foreach (var addOn in selectedAddOns)
                    {
                        playerState.SetSubRole(addOn, pc);
                        Logger.Info($"Assigned Add-on {addOn} to {pc.name}", "Randomizer");
                    }
                }

                // Sync settings and tasks
                pc.SyncSettings();
                playerState.InitTask(pc);
                pc.GetRoleClass()?.OnAdd(pc.PlayerId);
            }
        }








        private void ResetSubRoles(byte playerId)
        {
            var playerState = Main.PlayerStates[playerId];
            if (playerState == null) return;

            foreach (var subRole in playerState.SubRoles.ToList()) // Use ToList() to avoid modification during enumeration
            {
                playerState.RemoveSubRole(subRole);
            }

            playerState.SubRoles.Clear(); // Ensure the SubRoles list is empty
        }
        public override void Remove(byte playerId)
        {
            var playerState = Main.PlayerStates[playerId];
            if (playerState != null)
                playerState.IsRandomizer = false; // Clear Randomizer flag
            playerState.TeamLockApplied = false; // Reset team lock
            playerState.IsCrewmateTeam = false;
            playerState.IsImpostorTeam = false;
            playerState.IsNeutralTeam = false;
        }

        // Define role lists inside the Randomizer class
        private static List<CustomRoles> GetAllCrewmateRoles()
        {
            return new List<CustomRoles>
            {
                CustomRoles.Addict,
    CustomRoles.Alchemist,
    CustomRoles.Bastion,
    CustomRoles.Benefactor,
    CustomRoles.Bodyguard,
    CustomRoles.Captain,
    CustomRoles.Celebrity,
    CustomRoles.Cleanser,
    CustomRoles.Coroner,
    CustomRoles.Crusader,
    CustomRoles.Deceiver,
    CustomRoles.Deputy,
    CustomRoles.Detective,
    CustomRoles.Dictator,
    CustomRoles.Enigma,
    CustomRoles.FortuneTeller,
    CustomRoles.Grenadier,
    CustomRoles.Guardian,
    CustomRoles.GuessMaster,
    CustomRoles.Inspector,
    CustomRoles.Investigator,
    CustomRoles.Jailer,
    CustomRoles.Judge,
    CustomRoles.Keeper,
    CustomRoles.Knight,
    CustomRoles.LazyGuy,
    CustomRoles.Lighter,
    CustomRoles.Lookout,
    CustomRoles.Marshall,
    CustomRoles.Mayor,
    CustomRoles.Mechanic,
    CustomRoles.Medium,
    CustomRoles.Merchant,
    CustomRoles.Mole,
    CustomRoles.Mortician,
    CustomRoles.NiceGuesser,
    CustomRoles.Observer,
    CustomRoles.Oracle,
    CustomRoles.Overseer,
    CustomRoles.Pacifist,
    CustomRoles.Psychic,
    CustomRoles.Reverie,
    CustomRoles.Sheriff,
    CustomRoles.Snitch,
    CustomRoles.Spiritualist,
    CustomRoles.Spy,
    CustomRoles.Swapper,
    CustomRoles.TaskManager,
    CustomRoles.Telecommunication,
    CustomRoles.TimeManager,
    CustomRoles.TimeMaster,
    CustomRoles.Tracefinder,
    CustomRoles.Transporter,
    CustomRoles.Ventguard,
    CustomRoles.Veteran,
    CustomRoles.Vigilante,
    CustomRoles.Witness,
                // Add other Crewmate roles here
            };
        }

        private static List<CustomRoles> GetAllNeutralRoles()
        {
            return new List<CustomRoles>
            {
                        CustomRoles.Arsonist,
        CustomRoles.Arsonist,
     CustomRoles.Bandit,

     CustomRoles.BloodKnight,
     CustomRoles.Collector,
     CustomRoles.Demon,
     CustomRoles.Doomsayer,
     CustomRoles.Executioner,
     CustomRoles.Evolver,
     CustomRoles.Follower,
     CustomRoles.Glitch,
     CustomRoles.God,
     CustomRoles.Hater,
     CustomRoles.HexMaster,
     CustomRoles.Huntsman,
    CustomRoles.Jester,
    CustomRoles.Jinx,
    CustomRoles.Juggernaut,
     CustomRoles.Maverick,
    CustomRoles.Medusa,
    CustomRoles.Necromancer,
    CustomRoles.Opportunist,
    CustomRoles.Pelican,
     CustomRoles.Pickpocket,
     CustomRoles.Pirate,
     CustomRoles.Pixie,
    CustomRoles.PlagueDoctor,
    CustomRoles.Poisoner,
    CustomRoles.PotionMaster,
     CustomRoles.PunchingBag,
     CustomRoles.Pursuer,
     CustomRoles.Pyromaniac,
     CustomRoles.Quizmaster,
     CustomRoles.Revolutionist,
     CustomRoles.RuthlessRomantic,
     CustomRoles.SchrodingersCat,
     CustomRoles.Seeker,
    CustomRoles.SerialKiller,
    CustomRoles.Shaman,
    CustomRoles.Shroud,
    CustomRoles.Sidekick,
    CustomRoles.Solsticer,
    CustomRoles.Stalker,
    CustomRoles.Sunnyboy,
    CustomRoles.Taskinator,
    CustomRoles.Terrorist,
    CustomRoles.Traitor,
    CustomRoles.Troller,
    CustomRoles.Vector,
    CustomRoles.VengefulRomantic,
     CustomRoles.Vulture,
     CustomRoles.Werewolf,
     CustomRoles.Workaholic,
    
                // Add other Neutral roles here
            };
        }

        private static List<CustomRoles> GetAllImpostorRoles()
        {
            return new List<CustomRoles>
    {
        CustomRoles.Consigliere,
        CustomRoles.Crewpostor,
        CustomRoles.Cleaner,
         CustomRoles.Anonymous,
    CustomRoles.AntiAdminer,
    CustomRoles.Arrogance,
    CustomRoles.Bard,
    CustomRoles.Blackmailer,
    CustomRoles.Bomber,
    CustomRoles.BountyHunter,
    CustomRoles.Butcher,
    CustomRoles.Camouflager,
    CustomRoles.Chronomancer,
    CustomRoles.Councillor,
    CustomRoles.CursedWolf,
    CustomRoles.Dazzler,
    CustomRoles.Deathpact,
    CustomRoles.Disperser,
    CustomRoles.DoubleAgent,
    CustomRoles.Eraser,
    CustomRoles.Escapist,
    CustomRoles.EvilGuesser,
    CustomRoles.EvilHacker,
    CustomRoles.EvilTracker,
    CustomRoles.Fireworker,
    CustomRoles.Greedy,
    CustomRoles.Hangman,
    CustomRoles.Inhibitor,
    CustomRoles.Kamikaze,
    CustomRoles.KillingMachine,
    CustomRoles.Lightning,
    CustomRoles.Ludopath,
    CustomRoles.Lurker,
    CustomRoles.Mastermind,
    CustomRoles.Miner,
    CustomRoles.Morphling,
    CustomRoles.Ninja,
    CustomRoles.Parasite,
    CustomRoles.Penguin,
    CustomRoles.Pitfall,
    CustomRoles.Puppeteer,
    CustomRoles.QuickShooter,
    CustomRoles.Refugee,
    CustomRoles.RiftMaker,
    CustomRoles.Saboteur,
    CustomRoles.Scavenger,
    CustomRoles.ShapeMaster,
    CustomRoles.Sniper,
    CustomRoles.SoulCatcher,
    CustomRoles.Stealth,
    CustomRoles.YinYanger,
    CustomRoles.Swooper,
    CustomRoles.Trapster,
    CustomRoles.Trickster,
    CustomRoles.Twister,
    CustomRoles.Undertaker,
    CustomRoles.Vampire,
    CustomRoles.Vindicator,
    CustomRoles.Visionary,
    CustomRoles.Warlock,
    CustomRoles.Wildling,
    CustomRoles.Witch,
    CustomRoles.Zombie,
        // Add other existing impostor roles here
    };
        }


        private static List<CustomRoles> GetAvailableAddOns()
        {
            return new List<CustomRoles>
    {

        CustomRoles.Antidote,
        CustomRoles.Autopsy,
        CustomRoles.Avanger,
        CustomRoles.Aware,
        CustomRoles.Bait,
        CustomRoles.Bewilder,
        CustomRoles.Bloodthirst,
        CustomRoles.Burst,
        CustomRoles.Circumvent,
        CustomRoles.Cleansed,
        CustomRoles.Clumsy,
        CustomRoles.Cyber,
        CustomRoles.Diseased,
        CustomRoles.DoubleShot,
        CustomRoles.Eavesdropper,
        CustomRoles.Evader,
        CustomRoles.Flash,
        CustomRoles.Fool,
        CustomRoles.Fragile,
        CustomRoles.Ghoul,
        CustomRoles.Glow,
        CustomRoles.Gravestone,
        CustomRoles.Guesser,
        CustomRoles.Influenced,
        CustomRoles.LastImpostor,
        CustomRoles.Lazy,
        CustomRoles.Loyal,
        CustomRoles.Lucky,
        CustomRoles.Mare,
        CustomRoles.Rebirth,
        CustomRoles.Mimic,
        CustomRoles.Mundane,
        CustomRoles.Necroview,
        CustomRoles.Nimble,
        CustomRoles.Oblivious,
        CustomRoles.Onbound,
        CustomRoles.Overclocked,
        CustomRoles.Paranoia,
        CustomRoles.Prohibited,
        CustomRoles.Radar,
        CustomRoles.Rainbow,
        CustomRoles.Rascal,
        CustomRoles.Reach,
        CustomRoles.Rebound,
        CustomRoles.Spurt,
        CustomRoles.Seer,
        CustomRoles.Silent,
        CustomRoles.Sleuth,
        CustomRoles.Sloth,
        CustomRoles.Statue,
        CustomRoles.Stubborn,
        CustomRoles.Susceptible,
        CustomRoles.Swift,
        CustomRoles.Tiebreaker,
        CustomRoles.Stealer,
        CustomRoles.Torch,
        CustomRoles.Trapper,
        CustomRoles.Tricky,
        CustomRoles.Tired,
        CustomRoles.Unlucky,
        CustomRoles.VoidBallot,
        CustomRoles.Watcher,
        CustomRoles.Workhorse,

    };


        }

        private static readonly List<CustomRoles> GhostRolesList = new()
{
    CustomRoles.Bloodmoon,
    CustomRoles.Minion,
    CustomRoles.Possessor,
    CustomRoles.Ghastly,
    CustomRoles.Hawk,
    CustomRoles.Warden
};
    }
}
