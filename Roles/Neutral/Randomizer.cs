// using TOHE.Roles.Core;
// using TOHE.Roles.Crewmate;
// using UnityEngine;
// using static TOHE.Options;
// using static TOHE.Utils;

// namespace TOHE.Roles.Neutral;

// internal class Randomizer : RoleBase
// {
//     //===========================SETUP================================\\
//     private const int Id = 82000; // Unique ID for Randomizer
//     public static readonly HashSet<byte> playerIdList = [];
//     private static readonly Dictionary<byte, List<CustomRoles>> KeptAddons = [];
//     public override bool IsDesyncRole => true;
//     public override bool IsExperimental => true;
//     public override CustomRoles ThisRoleBase => CustomRoles.Impostor; // Base role remains Neutral
//     public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralChaos;
//     public override CustomRoles Role => CustomRoles.Randomizer;
//     //================================================================\\
    
//     private static OptionItem ChanceCrew;
//     private static OptionItem ChanceImpostor;
//     private static OptionItem ChanceNeutral;
//     private static OptionItem ChanceCoven;
//     public static OptionItem CanGetNecronomicon;
//     private static OptionItem OnlyEnabledRoles;
//     private static OptionItem AllowGhostRoles;
//     private static OptionItem MinAddOns;
//     private static OptionItem MaxAddOns;


//     public override bool CanUseKillButton(PlayerControl pc) => !pc.Is(CustomRoles.Randomizer);


//     public override void SetupCustomOption()
//     {
//         SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Randomizer);

//         // Team Chances
//         ChanceCrew = IntegerOptionItem.Create(Id + 10, "Randomizer.ChanceCrew", new(0, 100, 5), 25, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Randomizer])
//          .SetValueFormat(OptionFormat.Percent);
//         ChanceImpostor = IntegerOptionItem.Create(Id + 11, "Randomizer.ChanceImpostor", new(0, 100, 5), 25, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Randomizer])
//          .SetValueFormat(OptionFormat.Percent);
//         ChanceNeutral = IntegerOptionItem.Create(Id + 12, "Randomizer.ChanceNeutral", new(0, 100, 5), 25, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Randomizer])
//          .SetValueFormat(OptionFormat.Percent);
//         ChanceCoven = IntegerOptionItem.Create(Id + 13, "Randomizer.ChanceCoven", new(0, 100, 5), 25, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Randomizer])
//          .SetValueFormat(OptionFormat.Percent);
//         CanGetNecronomicon = BooleanOptionItem.Create(Id + 15, "Randomizer.CanGetNecronomicon", true, TabGroup.NeutralRoles, false).SetParent(ChanceCoven);
//         OnlyEnabledRoles = BooleanOptionItem.Create(Id + 14, "Randomizer.OnlyEnabledRoles", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Randomizer]);

//         AllowGhostRoles = BooleanOptionItem.Create(Id + 20, "Randomizer.AllowGhostRoles", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Randomizer]);

//         MinAddOns = IntegerOptionItem.Create(Id + 30, "Randomizer.MinAddOns", new(0, 100, 1), 0, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Randomizer]);

//         MaxAddOns = IntegerOptionItem.Create(Id + 31, "Randomizer.MaxAddOns", new(0, 100, 1), 3, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Randomizer]);
//     }

//     public override void Init()
//     {
//         playerIdList.Clear();
//         KeptAddons.Clear();

//         int crewChance = ChanceCrew.GetInt();
//         int impostorChance = ChanceImpostor.GetInt();
//         int neutralChance = ChanceNeutral.GetInt();
//         int covenChance = ChanceCoven.GetInt();

//         int totalChance = crewChance + impostorChance + neutralChance + covenChance;

//         // Check if total chances exceed 100%
//         if (totalChance > 100)
//         {
//             Logger.Warn("Total team chances exceed 100%. Overlap resolution will be applied during role assignment.", "Randomizer");
//         }
//         else if (totalChance == 0)
//         {
//             Logger.Warn("All team chances are set to 0. Using default equal distribution.", "Randomizer");
//         }

//         Logger.Info($"Initialized Randomizer with team chances - Crewmate: {crewChance}%, Impostor: {impostorChance}%, Neutral: {neutralChance}%, Coven: {covenChance}%.", "Randomizer");
//     }

//     public override void Add(byte playerId)
//     {
//         if (playerIdList.Contains(playerId)) return; // Avoid duplicates

//         playerIdList.Add(playerId);
//         KeptAddons.Add(playerId, []);
//         var pc = GetPlayerById(playerId);
//         if (pc == null) return;

//         // Set Randomizer role
//         var playerState = Main.PlayerStates[playerId];
//         playerState.IsRandomizer = true;

//         if (pc.GetCustomRole() != CustomRoles.Randomizer)
//         {
//             pc.RpcChangeRoleBasis(ThisRoleBase);
//             pc.RpcSetCustomRole(CustomRoles.Randomizer);
//         }

//         // Notify player
//         pc.Notify($"You are the {CustomRoles.Randomizer.ToColoredString()}! Your role will change after each meeting.");
//     }

//     private void AssignRandomRole(byte playerId)
//     {
//         var pc = GetPlayerById(playerId);
//         if (pc == null) return;

//         // Reset subroles before assigning a new role
//         ResetSubRoles(playerId);

//         // Get the player's state
//         var playerState = Main.PlayerStates[playerId];
//         if (playerState == null) return;

//         // Determine team lock if not already applied
//         if (!playerState.TeamLockApplied)
//         {
//             CustomRoles randomRole = GetRandomRoleAcrossAllTeams(playerId);

//             // Lock the team based on the role
//             if (randomRole.IsCrewmate())
//             {
//                 playerState.IsRandCrewmateTeam = true;
//                 playerState.LockedRoleType = Custom_RoleType.CrewmateBasic; // Lock to Crewmate type
//                 Logger.Info($"Randomizer locked to Crewmate team.", "Randomizer");
//             }
//             else if (randomRole.IsImpostorTeam())
//             {
//                 playerState.IsRandImpostorTeam = true;
//                 playerState.LockedRoleType = Custom_RoleType.ImpostorVanilla; // Lock to Impostor type
//                 Logger.Info($"Randomizer locked to Impostor team.", "Randomizer");
//             }
//             else if (randomRole.IsNeutral())
//             {
//                 playerState.IsRandNeutralTeam = true;
//                 playerState.LockedRoleType = Custom_RoleType.NeutralChaos; // Lock to Neutral type
//                 Logger.Info($"Randomizer locked to Neutral team.", "Randomizer");
//             }
//             else if (randomRole.IsCoven())
//             {
//                 playerState.IsRandCovenTeam = true;
//                 playerState.LockedRoleType = Custom_RoleType.CovenKilling; // Lock to Neutral type
//                 Logger.Info($"Randomizer locked to Coven team.", "Randomizer");
//             }

//             // Apply the role
//             pc.RpcChangeRoleBasis(randomRole); // Update basis to match the new role
//             pc.RpcSetCustomRole(randomRole);  // Set the random role
//             pc.GetRoleClass()?.OnAdd(playerId); // Initialize the new role logic
//             pc.SyncSettings(); // Ensures the player's UI reflects the changes

//             // Initialize tasks and abilities
//             playerState.InitTask(pc);

//             Logger.Info($"Randomizer assigned initial role {randomRole} to player {pc.name}", "Randomizer");
//         }
//         else
//         {
//             // If team is already locked, get a new random role
//             CustomRoles randomRole = GetRandomRoleAcrossAllTeams(playerId);

//             // Apply the new role while maintaining the locked team
//             pc.RpcChangeRoleBasis(randomRole); // Update basis to match the new role
//             pc.RpcSetCustomRole(randomRole);  // Set the random role
//             pc.GetRoleClass()?.OnAdd(playerId); // Initialize the new role logic
//             pc.SyncSettings(); // Ensures the player's UI reflects the changes

//             // Initialize tasks and abilities
//             playerState.InitTask(pc);

//             Logger.Info($"Randomizer assigned new role {randomRole} to player {pc.name} (team locked to {playerState.LockedRoleType})", "Randomizer");
//         }
//     }


//     public static void RandomizerWinCondition(PlayerControl pc)
//     {
//         if (pc == null) return;

//         var playerState = Main.PlayerStates[pc.PlayerId];
//         if (!playerState.IsRandomizer || !playerState.TeamLockApplied)
//         {
//             Logger.Warn($"Randomizer {pc.name} has no team lock applied or is not a Randomizer. Skipping win condition check.", "Randomizer");
//             return;
//         }

//         // Alive players' win conditions
//         switch (playerState.RandomizerWinCondition)
//         {
//             case Custom_Team.Crewmate:
//                 if (CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate && pc.IsAlive())
//                 {
//                     CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
//                     CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Randomizer);
//                     Logger.Info($"Randomizer {pc.name} (alive) wins with the Crewmate team.", "Randomizer");
//                 }
//                 break;

//             case Custom_Team.Coven:
//                 if (CustomWinnerHolder.WinnerTeam == CustomWinner.Coven && pc.IsAlive())
//                 {
//                     CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
//                     CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Randomizer);
//                     Logger.Info($"Randomizer {pc.name} (alive) wins with the Coven team.", "Randomizer");
//                 }
//                 break;

//             case Custom_Team.Impostor:
//                 if (CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor && pc.IsAlive())
//                 {
//                     CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
//                     CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Randomizer);
//                     Logger.Info($"Randomizer {pc.name} (alive) wins with the Impostor team.", "Randomizer");
//                 }
//                 break;

//             case Custom_Team.Neutral:
//                 if (pc.IsAlive())
//                 {
//                     CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
//                     CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Randomizer);
//                     Logger.Info($"Randomizer {pc.name} wins as a Neutral player (alive).", "Randomizer");
//                 }
//                 break;

//             default:
//                 Logger.Warn($"Randomizer {pc.name} has an unknown or invalid win condition: {playerState.RandomizerWinCondition}.", "Randomizer");
//                 break;
//         }

//         // Dead players' win conditions
//         if (!pc.IsAlive())
//         {
//             if (playerState.RandomizerWinCondition == Custom_Team.Crewmate && CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate)
//             {
//                 CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
//                 CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Randomizer);
//                 Logger.Info($"Randomizer {pc.name} (dead) wins with the Crewmate team.", "Randomizer");
//             }
//             else if (playerState.RandomizerWinCondition == Custom_Team.Impostor && CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor)
//             {
//                 CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
//                 CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Randomizer);
//                 Logger.Info($"Randomizer {pc.name} (dead) wins with the Impostor team.", "Randomizer");
//             }
//             else if (playerState.RandomizerWinCondition == Custom_Team.Coven && CustomWinnerHolder.WinnerTeam == CustomWinner.Coven)
//             {
//                 CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
//                 CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Randomizer);
//                 Logger.Info($"Randomizer {pc.name} (dead) wins with the Coven team.", "Randomizer");
//             }
//         }
//     }

//     private static Custom_Team DetermineTeam()
//     {
//         int crewChance = ChanceCrew.GetInt();
//         int impostorChance = ChanceImpostor.GetInt();
//         int neutralChance = ChanceNeutral.GetInt();
//         int covenChance = ChanceCoven.GetInt();


//         int totalChance = crewChance + impostorChance + neutralChance + covenChance;

//         // Handle all chances set to 0 or if total chance is 0
//         if (totalChance == 0)
//         {
//             Logger.Warn("All team chances are set to 0. Running default overlap with equal chances.", "DetermineTeam");
//             return ResolveOverlap([Custom_Team.Crewmate, Custom_Team.Impostor, Custom_Team.Neutral, Custom_Team.Coven]);
//         }

//         var rand = IRandom.Instance;
//         int roll = rand.Next(0, totalChance);

//         // Check for overlapping chances
//         List<Custom_Team> overlappingTeams = [];

//         if (roll < crewChance) overlappingTeams.Add(Custom_Team.Crewmate);
//         if (roll < crewChance + impostorChance && roll >= crewChance) overlappingTeams.Add(Custom_Team.Impostor);
//         if (roll < crewChance + impostorChance + covenChance && roll >= crewChance + impostorChance) overlappingTeams.Add(Custom_Team.Coven);
//         if (roll >= crewChance + impostorChance + covenChance) overlappingTeams.Add(Custom_Team.Neutral);

//         // Handle overlap dynamically
//         if (overlappingTeams.Count > 1)
//         {
//             Logger.Warn($"Chance overlap detected for teams: {string.Join(", ", overlappingTeams)}. Resolving overlap...", "DetermineTeam");
//             return ResolveOverlap(overlappingTeams);
//         }

//         // Return the determined team if no overlap
//         if (roll < crewChance) return Custom_Team.Crewmate;
//         if (roll < crewChance + impostorChance) return Custom_Team.Impostor;
//         if (roll < crewChance + impostorChance + covenChance) return Custom_Team.Coven;
//         return Custom_Team.Neutral;
//     }
//     private static Custom_Team ResolveOverlap(IEnumerable<Custom_Team> overlappingTeams)
//     {
//         var teams = overlappingTeams.ToList();
//         int roll = IRandom.Instance.Next(0, teams.Count);

//         Logger.Info($"Resolved overlap. Selected team: {teams[roll]}", "ResolveOverlap");
//         return teams[roll];
//     }

//     private static void NotifyRoleChange(PlayerControl pc, CustomRoles newRole)
//     {
//         var playerState = Main.PlayerStates[pc.PlayerId];

//         // Get a list of add-ons for the player
//         var addOns = string.Join(", ", playerState.SubRoles.Select(addOn => GetRoleName(addOn)));

//         // Notify Randomizer about its role and add-ons
//         string message = $"You are still the Randomizer! Your current role is {GetRoleName(newRole)}";
//         if (!string.IsNullOrEmpty(addOns))
//         {
//             message += $" with the following add-ons: {addOns}.";
//         }

//         pc.Notify(ColorString(GetRoleColor(CustomRoles.Randomizer), message));
//     }
//     private static CustomRoles GetGhostRole(byte playerId)
//     {
//         var GhostRolesList = CustomRolesHelper.AllRoles.Where(role => role.IsGhostRole() && (OnlyEnabledRoles.GetBool() ? role.IsEnable() : true)).ToList();
//         if (!GhostRolesList.Any())
//         {
//             Logger.Warn("No ghost roles available. Defaulting to a fallback role.", "Randomizer");
//             return CustomRoles.CrewmateTOHE; // Default fallback if the list is empty
//         }

//         // Select a random ghost role
//         CustomRoles selectedRole = GhostRolesList[IRandom.Instance.Next(0, GhostRolesList.Count)];
//         Logger.Info($"Assigned ghost role {selectedRole} to player ID {playerId}.", "Randomizer");
//         return selectedRole;
//     }

//     private static CustomRoles GetRandomRoleAcrossAllTeams(byte playerId)
//     {
//         var pc = GetPlayerById(playerId);
//         if (pc == null) return CustomRoles.CrewmateTOHE; // Default fallback

//         var playerState = Main.PlayerStates[playerId];

//         // Determine team based on percentage chances
//         var team = DetermineTeam();

//         List<CustomRoles> availableRoles = team switch
//         {
//             Custom_Team.Crewmate => CustomRolesHelper.AllRoles.Where(role => role.IsCrewmate() && !role.IsAdditionRole() && !role.IsGhostRole() && (OnlyEnabledRoles.GetBool() ? role.IsEnable() : true)).ToList(),
//             Custom_Team.Impostor => CustomRolesHelper.AllRoles.Where(role => role.IsImpostor() && !role.IsAdditionRole() && !role.IsGhostRole() && (OnlyEnabledRoles.GetBool() ? role.IsEnable() : true)).ToList(),
//             Custom_Team.Neutral => CustomRolesHelper.AllRoles.Where(role => role.IsNeutral() && !role.IsAdditionRole() && !role.IsGhostRole() && (OnlyEnabledRoles.GetBool() ? role.IsEnable() : true) && role is not CustomRoles.Randomizer or CustomRoles.Lawyer).ToList(),
//             Custom_Team.Coven => CustomRolesHelper.AllRoles.Where(role => role.IsCoven() && !role.IsAdditionRole() && !role.IsGhostRole() && (OnlyEnabledRoles.GetBool() ? role.IsEnable() : true)).ToList(),
//             _ => [] // Default empty list
//         };
//         if (availableRoles.Contains(CustomRoles.Randomizer))
//         {
//             Logger.Error("Available roles list somehow contained Randomizer, removing it...", "Randomizer");
//             availableRoles.Remove(CustomRoles.Randomizer);
//         }
//         if (!availableRoles.Any())
//         {
//             Logger.Error("Available roles list is empty for the determined team. Defaulting to Crewmate.", "Randomizer");
//             return CustomRoles.CrewmateTOHE; // Fallback role
//         }

//         // Select a random role from the available roles
//         var selectedRole = availableRoles[IRandom.Instance.Next(0, availableRoles.Count)];

//         // Lock the team if not already locked
//         if (!playerState.TeamLockApplied)
//         {
//             playerState.TeamLockApplied = true;
//             playerState.IsRandCrewmateTeam = team == Custom_Team.Crewmate;
//             playerState.IsRandImpostorTeam = team == Custom_Team.Impostor;
//             playerState.IsRandNeutralTeam = team == Custom_Team.Neutral;
//             playerState.IsRandCovenTeam = team == Custom_Team.Coven;

//             Logger.Info($"Randomizer locked to {team} team.", "Randomizer");
//         }

//         Logger.Info($"Randomizer assigned role: {selectedRole}", "Randomizer");
//         return selectedRole;
//     }
//     public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
//     {

//         var playerState = Main.PlayerStates[target.PlayerId];
//         var isRandomizer = playerState.IsRandomizer;

//         if (!isRandomizer)
//         {
//             Logger.Info($"Player {target.name} is not Randomizer. Skipping ghost role assignment.", "Randomizer");
//             return false; // Continue with default behavior
//         }

//         Logger.Info($"Randomizer {target.name} is being killed by {killer.name}. Checking ghost role settings...", "Randomizer");

//         // Check if ghost roles are allowed
//         if (AllowGhostRoles.GetBool())
//         {
//             Logger.Info($"Ghost roles are enabled. Assigning GuardianAngel base to {target.name}.", "Randomizer");

//             // Assign GuardianAngel base and link the role
//             var newGhostRole = GetGhostRole(target.PlayerId);
//             target.RpcChangeRoleBasis(CustomRoles.GuardianAngel); // Change to GuardianAngel base
//             target.RpcSetCustomRole(newGhostRole); // Assign the selected ghost role
//             target.GetRoleClass()?.OnAdd(target.PlayerId); // Initialize the role class
//             target.SyncSettings(); // Sync role settings
//             playerState.IsDead = true; // Ensure the player is marked as a ghost

//             Logger.Info($"Assigned ghost role {newGhostRole} to Randomizer {target.name}.", "Randomizer");
//             return true; // Prevent further processing of the murder
//         }
//         else
//         {
//             Logger.Info($"Ghost roles are disabled. Keeping Randomizer {target.name} as a standard dead player.", "Randomizer");
//             return true; // Allow the murder to proceed without ghost role assignment
//         }
//     }


//     public static void RandomizerGhost(PlayerControl pc)
//     {
//         var playerState = Main.PlayerStates[pc.PlayerId];



//         Logger.Info($"Applying RandomizerGhost logic to {pc.name}.", "Randomizer");

//         // Teleport player outside the map
//         pc.RpcTeleport(ExtendedPlayerControl.GetBlackRoomPosition());

//         // Schedule a task to "kill" the player
//         _ = new LateTask(
//             () =>
//             {
//                 // Kill the player
//                 pc.RpcMurderPlayer(pc);
//                 playerState.IsDead = true;

//                 // Sync death information
//                 pc.SetRealKiller(pc); // Set self as killer (or null if unnecessary)
//                 Logger.Info($"{pc.name} has been killed and marked as a ghost Randomizer.", "Randomizer");

//                 // Teleport back to spawn
//                 _ = new LateTask(
//                     () =>
//                     {
//                         Vector3 spawnPosition = new Vector3(0, 0, 0);
//                         pc.RpcTeleport(spawnPosition);
//                         Logger.Info($"{pc.name} teleported back to spawn as a ghost Randomizer.", "Randomizer");
//                     },
//                     1f, // Delay to ensure smooth transition
//                     "TeleportBackToSpawn"
//                 );
//             },
//             0.5f, // Delay before killing
//             "RandomizerKillTask"
//         );
//     }

//     public static string RandomizerReminder(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
//     {
//         if (Main.PlayerStates[seen.PlayerId].IsRandomizer && !seen.Is(CustomRoles.Randomizer) && !seer.IsAlive())
//         {
//             return $"<size=1.5><i>{CustomRoles.Randomizer.ToColoredString()}</i></size>";
//         }
//         return string.Empty;
//     }

//     public static void UnAfterMeetingTasks()
//     {
//         if (playerIdList == null) return;
//         foreach (var playerId in playerIdList.ToList())
//         {

//             var pc = GetPlayerById(playerId);
//             if (pc == null) continue;
//             Logger.Info($"Randomizer {pc.name} Randomizer AfterMeetingTasks is running", "Randomizer");
            
//             foreach (var addOn in pc.GetCustomSubRoles().Where(x => (x.IsBetrayalAddonV2() || x == CustomRoles.Lovers) && !KeptAddons[playerId].Contains(x)))
//             {
//                 Logger.Info($"Randomizer {pc.name} keeps addon {addOn}", "Randomizer");
//                 KeptAddons[playerId].Add(addOn);
//             }
//             // Reset subroles
//             // Main.PlayerStates[playerId].ResetSubRoles();
//             foreach (var addon in pc.GetCustomSubRoles().ToArray().Except(KeptAddons[playerId]))
//             {
//                 Main.PlayerStates[playerId].RemoveSubRole(addon);
//             }
//             pc.GetRoleClass()?.OnRemove(pc.PlayerId);

//             // Determine role assignment based on player's alive status
//             CustomRoles newRole;
//             if (!pc.IsAlive())
//             {
//                 if (AllowGhostRoles.GetBool()) // Ghost roles are enabled
//                 {
//                     Logger.Info($"Randomizer {pc.name} is dead. Assigning a new ghost role.", "Randomizer");

//                     // Assign ghost role
//                     newRole = GetGhostRole(pc.PlayerId);
//                     pc.RpcChangeRoleBasis(CustomRoles.GuardianAngel); // Set base role
//                     pc.RpcSetCustomRole(newRole); // Set the custom ghost role
//                     pc.GetRoleClass()?.OnAdd(pc.PlayerId); // Initialize role
//                     pc.SyncSettings(); // Sync the role settings
//                     RandomizerGhost(pc);
//                     // Explicitly set dead state
//                     Main.PlayerStates[pc.PlayerId].IsDead = true; // Mark as dead

//                     Logger.Info($"Randomizer {pc.name} has been marked as dead and assigned the ghost role: {newRole}.", "Randomizer");
//                 }
//                 else
//                 {
//                     // Ghost roles are disabled, do nothing to avoid reviving
//                     Logger.Info($"Randomizer {pc.name} is dead. Ghost roles are disabled. No role changes applied.", "Randomizer");
//                     continue;
//                 }
//             }
//             else if (pc.IsAlive()) // Player is alive, assign a normal role
//             {
//                 Logger.Info($"Randomizer {pc.name} is alive. Assigning a normal role.", "Randomizer");
//                 newRole = GetRandomRoleAcrossAllTeams(playerId); // Assign from the normal role pool
//             }
//             else
//             {
//                 Logger.Warn($"Randomizer {pc.name} could not be assigned a role. Defaulting to Crewmate.", "Randomizer");
//                 newRole = CustomRoles.CrewmateTOHE; // Fallback in case of unexpected condition
//             }

//             Logger.Info($"Assigning role {newRole} to player {pc.name}", "Randomizer");

//             // Update the player's role
//             pc.RpcChangeRoleBasis(newRole); // Update the role basis
//             pc.RpcSetCustomRole(newRole);  // Set the actual role
//             pc.SetKillCooldown();
//             pc.ResetKillCooldown(); // Ensure cooldowns are reset for the new role

//             // Preserve Randomizer flag
//             var playerState = Main.PlayerStates[playerId];
//             playerState.IsRandomizer = true;

//             // Notify the player after the role is finalized
//             NotifyRoleChange(pc, newRole);

//             // Assign random add-ons (only for alive players)
//             if (pc.IsAlive())
//             {
//                 // Retrieve the min and max add-on settings
//                 int minAddOns = MinAddOns.GetInt();
//                 int maxAddOns = MaxAddOns.GetInt();

//                 // Ensure maxAddOns is not less than minAddOns
//                 if (maxAddOns < minAddOns)
//                 {
//                     Logger.Warn($"Max Add-Ons ({maxAddOns}) is less than Min Add-Ons ({minAddOns}). Using Min Add-Ons.", "Randomizer");
//                     maxAddOns = minAddOns;
//                 }

//                 // Randomly determine the number of add-ons to assign
//                 int addOnCount = IRandom.Instance.Next(minAddOns, maxAddOns + 1);
//                 List<CustomRoles> selectedAddOns = [.. CustomRolesHelper.AllRoles.Where(role => role.IsAdditionRole() && !role.IsBetrayalAddon() && !AddonBlackList(role) && (!OnlyEnabledRoles.GetBool() || role.IsEnable())).ToList()
//                     .OrderBy(_ => Random.value)
//                     .Take(addOnCount)];
//                 foreach (var addOn in KeptAddons[playerId])
//                 {
//                     selectedAddOns.Add(addOn);
//                 }

//                 foreach (var addOn in selectedAddOns)
//                 {                    
//                     pc.RpcSetCustomRole(addOn, false, false);
//                     Logger.Info($"Assigned Add-on {addOn} to {pc.name}", "Randomizer");
//                 }
//             }

//             // Sync settings and tasks
//             pc.SyncSettings();
//             playerState.InitTask(pc);
//             pc.GetRoleClass()?.OnAdd(pc.PlayerId);
//         }
//     }
//     private static bool AddonBlackList(CustomRoles role)
//     {
//         // Check if the role is in the blacklist
//         return role.IsBetrayalAddonV2() || role is
//             CustomRoles.Lovers or
//             CustomRoles.Cleansed or
//             CustomRoles.Admired;
//     }
//     private void ResetSubRoles(byte playerId)
//     {
//         var playerState = Main.PlayerStates[playerId];
//         if (playerState == null) return;

//         foreach (var subRole in playerState.SubRoles.ToList()) // Use ToList() to avoid modification during enumeration
//         {
//             playerState.RemoveSubRole(subRole);
//         }

//         playerState.SubRoles.Clear(); // Ensure the SubRoles list is empty
//     }
//     public override void Remove(byte playerId)
//     {
//         var playerState = Main.PlayerStates[playerId];
//         if (playerState != null)
//             playerState.IsRandomizer = false; // Clear Randomizer flag
//         playerState.TeamLockApplied = false; // Reset team lock
//         playerState.IsRandCrewmateTeam = false;
//         playerState.IsRandImpostorTeam = false;
//         playerState.IsRandNeutralTeam = false;
//         playerState.IsRandCovenTeam = false;
//     }

// }