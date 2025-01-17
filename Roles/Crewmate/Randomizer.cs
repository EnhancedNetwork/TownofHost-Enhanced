using System;
using TOHE.Modules;
using TOHE.Roles.AddOns.Common;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Randomizer : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 7500;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateBasic;
    //==================================================================\\

    public static OptionItem BecomeBaitDelayNotify;
    public static OptionItem BecomeBaitDelayMin;
    public static OptionItem BecomeBaitDelayMax;
    public static OptionItem BecomeTrapperBlockMoveTime;


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


        private static void AssignRandomRole(byte playerId)
        {
            var pc = Utils.GetPlayerById(playerId);
            if (pc == null) return;

        var Fg = IRandom.Instance;
        int Randomizer = Fg.Next(1, 5);

        if (Randomizer == 1)
        {
            if (isSuicide)
            {
                if (target.GetRealKiller() != null)
                {
                    if (!target.GetRealKiller().IsAlive()) return;
                    killer = target.GetRealKiller();
                }
            }

            if (killer.PlayerId == target.PlayerId) return;

            if (killer.Is(CustomRoles.KillingMachine)
                || (killer.Is(CustomRoles.Oblivious) && Oblivious.ObliviousBaitImmune.GetBool()))
                return;

            if (!isSuicide || (target.GetRealKiller()?.GetCustomRole() is CustomRoles.Swooper or CustomRoles.Wraith) || !killer.Is(CustomRoles.KillingMachine) || !killer.Is(CustomRoles.Oblivious) || (killer.Is(CustomRoles.Oblivious) && !Oblivious.ObliviousBaitImmune.GetBool()))
            {
                killer.RPCPlayCustomSound("Congrats");
                target.RPCPlayCustomSound("Congrats");

                float delay;
                if (BecomeBaitDelayMax.GetFloat() < BecomeBaitDelayMin.GetFloat())
                {
                    delay = 0f;
                }
                else
                {
                    delay = IRandom.Instance.Next((int)BecomeBaitDelayMin.GetFloat(), (int)BecomeBaitDelayMax.GetFloat() + 1);
                }
                delay = Math.Max(delay, 0.15f);
                if (delay > 0.15f && BecomeBaitDelayNotify.GetBool())
                {
                    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Bait), string.Format(GetString("KillBaitNotify"), (int)delay)), delay);
                }

                Logger.Info($"{killer.GetNameWithRole()} 击杀了萧暮触发自动报告 => {target.GetNameWithRole()}", "Randomizer");

                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Randomizer), GetString("YouKillRandomizer1")));

                _ = new LateTask(() =>
                {
                    if (GameStates.IsInTask) killer.CmdReportDeadBody(target.Data);
                }, delay, "Bait Self Report");
            }
        }
        else if (Randomizer == 2)
        {
            Logger.Info($"{killer.GetNameWithRole()} 击杀了萧暮触发暂时无法移动 => {target.GetNameWithRole()}", "Randomizer");

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Randomizer), GetString("YouKillRandomizer2")));
            var tmpSpeed = Main.AllPlayerSpeed[killer.PlayerId];
            Main.AllPlayerSpeed[killer.PlayerId] = Main.MinSpeed;
            ReportDeadBodyPatch.CanReport[killer.PlayerId] = false;
            killer.MarkDirtySettings();

            _ = new LateTask(() =>
            {
                Main.AllPlayerSpeed[killer.PlayerId] = Main.AllPlayerSpeed[killer.PlayerId] - Main.MinSpeed + tmpSpeed;
                ReportDeadBodyPatch.CanReport[killer.PlayerId] = true;
                killer.MarkDirtySettings();
                RPC.PlaySoundRPC(killer.PlayerId, Sounds.TaskComplete);
            }, BecomeTrapperBlockMoveTime.GetFloat(), "Trapper BlockMove");
        }
        else if (Randomizer == 3)
        {
            Logger.Info($"{killer.GetNameWithRole()} 击杀了萧暮触发凶手CD变成600 => {target.GetNameWithRole()}", "Randomizer");
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Randomizer), GetString("YouKillRandomizer3")));
            Main.AllPlayerKillCooldown[killer.PlayerId] = 600f;
            killer.SyncSettings();
        }
        else if (Randomizer == 4)
        {
            Logger.Info($"{killer.GetNameWithRole()} 击杀了萧暮触发随机复仇 => {target.GetNameWithRole()}", "Randomizer");
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Randomizer), GetString("YouKillRandomizer4")));
            {
                var pcList = Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId && target.RpcCheckAndMurder(x, true)).ToList();
                var pc = pcList[IRandom.Instance.Next(0, pcList.Count)];
                if (!pc.IsTransformedNeutralApocalypse())
                {
                    pc.SetDeathReason(PlayerState.DeathReason.Revenge);
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








        private static void ResetSubRoles(byte playerId)
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
    }
}
