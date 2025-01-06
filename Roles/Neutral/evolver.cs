using Hazel;
using InnerNet;
using System.Text.RegularExpressions;
using static TOHE.Options;
using static TOHE.Translator;
using TOHE.Roles.Core;
using UnityEngine;
using System;

namespace TOHE.Roles.Neutral
{
    internal class Evolver : RoleBase
    {
        //===========================SETUP================================\\
        private const int Id = 64000;
        public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Evolver);
        public override bool IsExperimental => true;
        public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
        public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
        //==================================================================\\

      
        public static OptionItem MinEvolutionsForWin;
        private static readonly Dictionary<byte, bool> PurchaseDone = new();
  
        private static readonly Dictionary<byte, Evolver> evolverCache = new();
        private static int evolverPoints = 0;
        private int purchasedUpgrades = 0;
        private bool hasNewMegaPoint = false;
        private float catchCooldown = 0.0f;
        public int EvolverPoints = new();
        private int voteLevel = 0;
        private static PlayerControl evolverPlayer;
        private int cooldownLevel = 0;
        private int voteUpgradeLevel = 0; // Starting level
        private int megaPoints = 0;
        private static int requiredPoints;
        private float baseCatchChance = 0.5f; // 50% base chance
        private bool isImmortalityActive = false;
        private const float MEGA_POINT_CHANCE = 0.05f;
        private const float IMMORTALITY_CATCH_CHANCE_DEBUFF = 0.5f;
        private const float IMMORTALITY_CATCH_COOLDOWN_MULTIPLIER = 1.5f;
        private float baseCatchCooldown = 20f;
        private int basePointsPerCatch = 1; // 1 point per successful catch
        private readonly int maxVoteLevel = 4;         // Upgrade level variables for Evolver's perks
        private int catchChanceLevel = 0;           // Tracks the level of the catch chance upgrade
        private int cooldownReductionLevel = 0;     // Tracks the level of the cooldown reduction upgrade
        private int pointsOnCatchLevel = 0;         // Tracks the level of points gained per catch
        private readonly int[] catchChanceUpgradeCosts = { 2, 4, 6, 9, 12 };
        private readonly int[] pointsOnCatchUpgradeCosts = { 3, 6, 10, 20 };
        private readonly int[] cooldownReductionUpgradeCosts = { 1, 5, 9, 14 };
        private readonly int[] voteUpgradeCosts = { 4, 9, 12, 17 };




        // These methods retrieve modified values based on upgrades
        private float GetCatchChance()
        {
            float catchChanceIncrease = catchChanceLevel * 0.1f;
            float cooldownReductionPenalty = cooldownReductionLevel * 0.05f;

            // Apply immortality catch chance debuff if active
            float adjustedCatchChance = baseCatchChance + catchChanceIncrease - cooldownReductionPenalty;
            if (isImmortalityActive)
            {
                adjustedCatchChance *= IMMORTALITY_CATCH_CHANCE_DEBUFF;
            }

            return Mathf.Clamp(adjustedCatchChance, 0.1f, 1f); // Clamps the value between 0.1 and 1
        }
        public void AddEvolutionPoint()
        {
            EvolverPoints++;
            Logger.Info($"Evolver Points Updated: {EvolverPoints}", "Evolver");
        }
        public override string GetProgressText(byte playerId, bool comms)
        {
            int minUpgrades = MinEvolutionsForWin.GetInt();
            if (minUpgrades == 0) return string.Empty;

            if (Main.PlayerStates[playerId].RoleClass is not Evolver ev) return string.Empty;
            int upgrades = ev.purchasedUpgrades;
            Color color = upgrades >= minUpgrades ? Color.green : Color.red;
            return Utils.ColorString(color, $"({upgrades}/{minUpgrades})");

        }
        public int GetPurchasedUpgrades() // Public method to access the value
        {
            return purchasedUpgrades;
        }

        private float GetCooldownReduction()
        {
            float baseCooldown = 25f; // 25 seconds base cooldown
            float cooldownReductionPerLevel = 2f; // 2 seconds per level
            return baseCooldown - (cooldownReductionLevel * cooldownReductionPerLevel);
        }

        private int GetPointsPerCatch()
        {
            return basePointsPerCatch + pointsOnCatchLevel;
        }
        private float GetCatchCooldown()
        {
            float cooldownIncrease = catchChanceLevel * 5f;
            float cooldownReduction = cooldownReductionLevel * 10f;

            // Apply immortality cooldown multiplier if active
            float adjustedCooldown = baseCatchCooldown + cooldownIncrease - cooldownReduction;
            if (isImmortalityActive)
            {
                adjustedCooldown *= IMMORTALITY_CATCH_COOLDOWN_MULTIPLIER;
            }

            return Mathf.Max(adjustedCooldown, 5f); // Ensures a minimum cooldown of 5 seconds
        }
        public void UpgradeCooldownReduction()
        {
            cooldownReductionLevel++;
            // Notify the player of the new cooldown only when they purchase an upgrade
            Utils.SendMessage("Cooldown reduction upgraded! New cooldown: {GetCatchCooldown():F1} seconds", PlayerControl.LocalPlayer.PlayerId);




        }
        public int GetEvolverPoints()
        {
            return evolverPoints;
        }
        public static void Reset()
        {
            evolverCache.Clear();
            foreach (var pc in Main.AllPlayerControls)
            {
                if (pc.GetCustomRole() == CustomRoles.Evolver)
                {
                    var points = Evolver.evolverPoints;
                }
            }
        }


        // Options for evolver role
        public override void SetupCustomOption()
        {
            SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Evolver, 1, zeroOne: false);

            // Minimum upgrades required to win
            MinEvolutionsForWin = IntegerOptionItem.Create(Id + 10, "Evolver_MinUpgradesToWin", new(0, 15, 1), 3,
                TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Evolver]);
                
        }




        public override bool CanUseKillButton(PlayerControl pc) => true;

        public override void SetKillCooldown(byte id)
        {
            Main.AllPlayerKillCooldown[id] = GetCatchCooldown(); // Sets the Evolver's cooldown using their current upgraded cooldown
        }



        public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
        {


            // Run the catch attempt if the target is valid
            if (target != _Player)
            {

                SendSkillRPC(); // Sync ability usage if necessary

                AttemptCatch(killer, target); // Run the catch mechanic

                killer.SetKillCooldown(); // Resets the cooldown for the Evolver (the one who used the ability)

                return false; // Prevent the actual kill

            }

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Evolver), GetString("EvolverInvalidTarget")));
            return false; // Always return false to block unintended kills
        }


        public override void SetAbilityButtonText(HudManager hud, byte id)
        {
            hud.KillButton.OverrideText(GetString("EvolverCatchText"));
        }

        public override void Init()
        {
            

            if (MinEvolutionsForWin == null)
            {
                Logger.Error("MinEvolutionsForWin is not initialized.", "Evolver");
                return; // Prevent further initialization to avoid crashing.
            }
            PurchaseDone.Clear();
            evolverPoints = 0;
           

        }

        public override void Add(byte playerId)
        {
            PurchaseDone[playerId] = false;
         
            }
        

        //===========================COMMANDS==============================\\
        public static bool EvolverCheckMsg(PlayerControl pc, string msg, bool isUI = false, bool isSystemMessage = false)
        {
            if (isSystemMessage || !AmongUsClient.Instance.AmHost) return false; // Skip if system message or not host

            // Skip messages tagged as "<shop>" to prevent reprocessing
            if (msg.StartsWith("<shop>")) return false;

            var originMsg = msg;
            Logger.Info($"Received command: {msg} from {pc.PlayerId}, Host: {AmongUsClient.Instance.AmHost}", "Evolver");

            if (!GameStates.IsMeeting || pc == null || GameStates.IsExilling) return false;
            if (!pc.Is(CustomRoles.Evolver) || !(pc.GetRoleClass() is Evolver evolverInstance)) return false;

            msg = msg.ToLower().Trim();
            bool isShop = false, isBuy = false;
            string error = string.Empty;

            // Check for "/shop" or "/buy" commands
            if (CheckCommand(ref msg, "shop")) isShop = true;
            else if (CheckCommand(ref msg, "buy")) isBuy = true;
            else return false;

            if (!pc.IsAlive())
            {
                pc.ShowInfoMessage(isUI, "You cannot use commands when dead!");
                Logger.Info("Command failed: Player is dead.", "Evolver");
                return true;
            }

            if (isShop)
            {
                Evolver.ShowShopOptions(pc);
                Logger.Info("Showing shop options and exiting.", "Evolver");
                return true;
            }

            if (isBuy && MsgToPlayerAndRole(msg, pc, out int effectId, out error))
            {
                evolverInstance.PurchaseUpgrade(pc, effectId);
                SendRPC(1, effectId);
                Logger.Info($"Processed buy command: effectId {effectId}", "Evolver");
                return true;
            }

            // Send error message if something went wrong
            Utils.SendMessage(error, pc.PlayerId);
            Logger.Info("Invalid option or error message sent.", "Evolver");
            return true;
        }



        public static void SendMessage(string message, byte playerId, bool isSystemMessage = false)
        {
            // Use the isSystemMessage flag to tag the message or handle it in a way that avoids re-parsing
            if (isSystemMessage)
            {
                message = "<system>" + message;  // Prefix or otherwise tag as a system message
            }

            // Rest of the message sending code
        }

        public void SetCatchCooldown(float cooldown, PlayerControl player, int cooldownLevel)
        {
            // Apply cooldown logic here
            // Example: Main.AllPlayerKillCooldown[player.PlayerId] = cooldown;
            Utils.SendMessage($"Catch cooldown set to {cooldown} seconds.", player.PlayerId);
        }
        private void AttemptCatch(PlayerControl killer, PlayerControl target)
        {
            bool isCatchSuccessful = UnityEngine.Random.value < GetCatchChance();

            if (isCatchSuccessful)
            {
                int pointsGained = GetPointsPerCatch();
                evolverPoints += pointsGained;

                // Regular success message with current total points after catch
                var variables = new Dictionary<string, string>
        {
            { "points", pointsGained.ToString() },
            { "totalPoints", evolverPoints.ToString() }
        };

                string successMessage = GetString("EvolverCatchSuccess", variables) + $" You now have {evolverPoints} points.";
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Evolver), successMessage));

                // Separate check for MEGA evolution point chance to avoid overlap
                if (UnityEngine.Random.value <= MEGA_POINT_CHANCE)
                {
                    megaPoints++;
                    hasNewMegaPoint = true;  // Set flag to true

                    string megaPointMessage = GetString("EvolverMegaPointGain");
                    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Evolver), megaPointMessage));
                }
            }
            else
            {
               

                string failureMessage = GetString("EvolverCatchFailure");
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Evolver), failureMessage));
            }

            // Trigger shield animation without killing and set cooldown
            if (!DisableShieldAnimations.GetBool())
            {
                killer.RpcGuardAndKill(target);
            }
        }




        private float GetUpgradeCatchChance()
        {
            // Example: +10% per upgrade level
            return catchChanceLevel * 0.1f;
        }

        private float GetUpgradeCooldownReduction()
        {
            // Example: -2 seconds per upgrade level
            return cooldownReductionLevel * 2f;
        }

        private int GetUpgradePointsOnCatch()
        {
            // Example: +1 point per upgrade level
            return pointsOnCatchLevel;
        }

        // Inside ShowShopOptions method
        private static void ShowShopOptions(PlayerControl player)
        {
            Evolver evolverInstance = player.GetRoleClass() as Evolver;
            if (evolverInstance == null) return;

            string shopMenu = "<shop>" +
                              " \n/buy 1: Increase catch chance (" +
                              $"{evolverInstance.catchChanceLevel}/{evolverInstance.catchChanceUpgradeCosts.Length}) " +
                              $"[{evolverInstance.catchChanceUpgradeCosts[Mathf.Min(evolverInstance.catchChanceLevel, evolverInstance.catchChanceUpgradeCosts.Length - 1)]} points]\n" +
                              "/buy 2: Increase points on catch (" +
                              $"{evolverInstance.pointsOnCatchLevel}/{evolverInstance.pointsOnCatchUpgradeCosts.Length}) " +
                              $"[{evolverInstance.pointsOnCatchUpgradeCosts[Mathf.Min(evolverInstance.pointsOnCatchLevel, evolverInstance.pointsOnCatchUpgradeCosts.Length - 1)]} points]\n" +
                              "/buy 3: Decrease catch cooldown (" +
                              $"{evolverInstance.cooldownReductionLevel}/{evolverInstance.cooldownReductionUpgradeCosts.Length}) " +
                              $"[{evolverInstance.cooldownReductionUpgradeCosts[Mathf.Min(evolverInstance.cooldownReductionLevel, evolverInstance.cooldownReductionUpgradeCosts.Length - 1)]} points]\n" +
                              "/buy 4: Increase votes (" +
                              $"{evolverInstance.voteLevel}/{evolverInstance.maxVoteLevel}) " +
                              $"[{evolverInstance.voteUpgradeCosts[Mathf.Min(evolverInstance.voteLevel, evolverInstance.maxVoteLevel - 1)]} points]\n";

            if (!evolverInstance.isImmortalityActive)
            {
                shopMenu += "/buy 5: Immortality Shield (0/1) [20 points]\n";
            }
            else
            {
                shopMenu += "/buy 5: Immortality Shield (1/1) [Purchased]\n";
            }


            int evolverPoints = GetEvolverPoints(player.PlayerId);
            shopMenu += $"\nYou currently have {evolverPoints} points.";

            Utils.SendMessage(shopMenu, player.PlayerId);
        }




        //===========================RPC METHODS==============================\\
        public static void SendRPC(int operate, int effectId = -1)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
            writer.WriteNetObject(PlayerControl.LocalPlayer);
            writer.Write(operate);
            if (operate == 1) writer.Write(effectId); // Send effect ID if it's a purchase
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
        {
            int operate = reader.ReadInt32();
            if (operate == 1)
            {
                int effectId = reader.ReadInt32();
                ApplyEffect(pc, effectId); // Apply the upgrade effect to the Evolver player
            }
        }

        //===========================HELPER METHODS==============================\\
        private static bool MsgToPlayerAndRole(string msg, PlayerControl player, out int effectId, out string error)
        {
            if (msg.StartsWith("/"))
                msg = msg.Replace("/", string.Empty);

            Regex r = new("\\d+");
            MatchCollection mc = r.Matches(msg);
            string result = string.Empty;
            for (int i = 0; i < mc.Count; i++)
                result += mc[i];

            if (int.TryParse(result, out int num))
            {
                if (num < 1 || num > 6)
                {
                    effectId = -1;
                    error = "/buy 1: Increase catch chance\n" +
                        "/buy 2: Increase points on catch\n" +
                        "/buy 3: Health increase\n" +
                        "/buy 4: Increase votes\n" +
                        "/buy 5: Decrease catch cooldown\n" +
                        "/buy 6: Immortality\n" +
                        $"\nYou currently have {evolverPoints} points.";

                    return false;
                }
                effectId = num;
                error = string.Empty;
                return true;
            }
            else
            {
                effectId = -1;

                // Build the shop options message with current points
                int evolverPoints = GetEvolverPoints(player.PlayerId);
                error = "/buy 1: Increase catch chance\n" +
                        "/buy 2: Increase points on catch\n" +
                        "/buy 3: Health increase\n" +
                        "/buy 4: Increase votes\n" +
                        "/buy 5: Decrease catch cooldown\n" +
                        "/buy 6: Immortality\n" +
                        $"\nYou currently have {evolverPoints} points.";

                return false;
            }

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
        public override int AddRealVotesNum(PlayerVoteArea ps)
        {
            return voteUpgradeLevel; // Each level grants an additional vote
        }

        public override void AddVisualVotes(PlayerVoteArea votedPlayer, ref List<MeetingHud.VoterState> statesList)
        {
            var additionalVotes = voteUpgradeLevel;

            for (var i = 0; i < additionalVotes; i++)
            {
                statesList.Add(new MeetingHud.VoterState()
                {
                    VoterId = votedPlayer.TargetPlayerId,
                    VotedForId = votedPlayer.VotedFor
                });
            }
        }

        public void BuyImmortality(PlayerControl player)
        {
            // Check if ability is already bought
            if (isImmortalityActive) return;

            // Activate immortality shield
            isImmortalityActive = true;

            Utils.SendMessage("You have gained an immortality shield, but your abilities have been weakened!", player.PlayerId);
        }


        // Method to get the adjusted catch chance based on immortality status


        public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
        {
            // Check if target is Evolver and has shield active
            if (target.Is(CustomRoles.Evolver) && isImmortalityActive)
            {
                // Block the attack and notify the player
                Utils.SendMessage("Your immortality shield protected you from an attack!", target.PlayerId);
                return false; // Cancels the kill
            }

          
            // Standard behavior if no shield or reflection is active
            return base.OnCheckMurderAsTarget(killer, target);
        }








        public void PurchaseUpgrade(PlayerControl player, int effectId)
        {
            int currentPoints = GetEvolverPoints(player.PlayerId);

            switch (effectId)
            {
                case 1: // Increase Catch Chance
                    Logger.Info($"Attempting to upgrade Catch Chance: current level = {catchChanceLevel}, max level = {catchChanceUpgradeCosts.Length}", "Evolver", false, 0, "", false);
                    if (catchChanceLevel < catchChanceUpgradeCosts.Length &&
                        currentPoints >= catchChanceUpgradeCosts[catchChanceLevel])
                    {
                        DeductPoints(player.PlayerId, catchChanceUpgradeCosts[catchChanceLevel]);
                        catchChanceLevel++;
                        purchasedUpgrades++;
                        Utils.SendMessage($"Catch chance upgraded to {GetCatchChance() * 100}%! Cooldown is now {GetCatchCooldown()} seconds.", player.PlayerId);
                    }
                    else
                    {
                        Utils.SendMessage("Not enough points or max level reached for catch chance upgrade.", player.PlayerId);
                    }
                    break;

                case 2: // Increase Points on Catch
                    Logger.Info($"Attempting to upgrade Points on Catch: current level = {pointsOnCatchLevel}, max level = {pointsOnCatchUpgradeCosts.Length}", "Evolver", false, 0, "", false);
                    if (pointsOnCatchLevel < pointsOnCatchUpgradeCosts.Length &&
                        currentPoints >= pointsOnCatchUpgradeCosts[pointsOnCatchLevel])
                    {
                        DeductPoints(player.PlayerId, pointsOnCatchUpgradeCosts[pointsOnCatchLevel]);
                        pointsOnCatchLevel++;
                        purchasedUpgrades++;
                        Utils.SendMessage($"Points on catch upgraded to {GetPointsPerCatch()} points.", player.PlayerId);
                    }
                    else
                    {
                        Utils.SendMessage("Not enough points or max level reached for points on catch upgrade.", player.PlayerId);
                    }
                    break;

                case 3: // Decrease Catch Cooldown
                    Logger.Info($"Attempting to upgrade Catch Cooldown: current level = {cooldownReductionLevel}, max level = {cooldownReductionUpgradeCosts.Length}", "Evolver", false, 0, "", false);
                    if (cooldownReductionLevel < cooldownReductionUpgradeCosts.Length &&
                        currentPoints >= cooldownReductionUpgradeCosts[cooldownReductionLevel])
                    {
                        DeductPoints(player.PlayerId, cooldownReductionUpgradeCosts[cooldownReductionLevel]);
                        cooldownReductionLevel++;
                        purchasedUpgrades++;
                        Utils.SendMessage($"Catch cooldown reduced to {GetCatchCooldown()} seconds. Current catch chance is {GetCatchChance() * 100}%.", player.PlayerId);
                    }
                    else
                    {
                        Utils.SendMessage("Not enough points or max level reached for cooldown reduction upgrade.", player.PlayerId);
                    }
                    break;

                case 4: // Increase Votes
                    Logger.Info($"Attempting to upgrade Votes: current level = {voteUpgradeLevel}, max level = {voteUpgradeCosts.Length}", "Evolver", false, 0, "", false);

                    if (voteUpgradeLevel < voteUpgradeCosts.Length &&
        currentPoints >= voteUpgradeCosts[voteUpgradeLevel])
                    {
                        DeductPoints(player.PlayerId, voteUpgradeCosts[voteUpgradeLevel]);
                        voteUpgradeLevel++;
                        purchasedUpgrades++;
                        Utils.SendMessage($"Vote count increased to {GetVoteCount()}!", player.PlayerId);
                    }
                    else
                    {
                        Utils.SendMessage("Not enough points or max level reached for vote count upgrade.", player.PlayerId);
                    }
                    break;

                case 5: // Immortality Shield
                    Logger.Info("Attempting to purchase Immortality Shield", "Evolver", false, 0, "", false);
                    if (!isImmortalityActive && currentPoints >= 20)
                    {
                        DeductPoints(player.PlayerId, 20);
                        BuyImmortality(player); // Pass the player object here
                        purchasedUpgrades++;
                        Utils.SendMessage("Immortality shield purchased! You are now shielded from attacks, but catch chance and cooldown are affected.", player.PlayerId);
                    }
                    else
                    {
                        Utils.SendMessage("Not enough points or Immortality Shield already purchased.", player.PlayerId);
                    }
                    break;

               
                 



                default:
                    Utils.SendMessage("Invalid upgrade option.", player.PlayerId);
                    break;
            }
        }

        private int GetVoteCount()
        {
            // Base vote count is 1, and each level adds an additional vote
            return 1 + voteUpgradeLevel; // Assuming 0 upgrades means 1 vote, level 4 means 5 votes
        }





        // Other methods like EnableReflectionAbility() and EnableReflexiveCooldownReduction() will follow a similar structure

        private static void ApplyEffect(PlayerControl player, int effectId)
        {
            switch (effectId)
            {
                case 1:
                    Utils.SendMessage("Increased catch chance applied!", player.PlayerId);
                    // Apply catch chance increase logic
                    break;
                case 2:
                    Utils.SendMessage("Increased points on catch applied!", player.PlayerId);
                    // Apply points increase logic
                    break;
                case 3:
                    Utils.SendMessage("Health increase applied!", player.PlayerId);
                    // Apply health increase logic
                    break;
                case 4:
                    Utils.SendMessage("Increased votes applied!", player.PlayerId);
                    // Apply vote increase logic
                    break;
                case 5:
                    Utils.SendMessage("Decreased catch cooldown applied!", player.PlayerId);
                    // Apply cooldown reduction logic
                    break;
                case 6:
                    Utils.SendMessage("Immortality applied!", player.PlayerId);
                    // Apply immortality logic
                    break;
            }
        }


        private static int GetUpgradeCost(int effectId) => 1; // Example cost, can vary per upgrade
        private static int GetEvolverPoints(byte playerId) => evolverPoints;
        private static void DeductPoints(byte playerId, int cost) => evolverPoints -= cost;

        //===========================MEGA upgrades===========================================================================================================================================================================================================================================\\
        public void AttemptMegaPointGain(PlayerControl player)
        {
            if (megaPoints >= 1) return; // Limit to 1 MEGA point for simplicity

            if (UnityEngine.Random.value <= MEGA_POINT_CHANCE)
            {
                megaPoints++;
                Utils.SendMessage("You earned a MEGA evolution point! It will be automatically converted to normal points in the next meeting.", player.PlayerId);
                hasNewMegaPoint = true; // Set flag to notify during meeting
            }
        }

        public override void OnMeetingHudStart(PlayerControl pc)
        {
            if (hasNewMegaPoint)
            {
                hasNewMegaPoint = false; // Reset the flag after notifying

                if (megaPoints > 0)
                {
                    // Convert MEGA points to normal points
                    int pointsToAdd = megaPoints * 5; // Conversion rate: 1 MEGA point = 5 normal points
                    AddNormalPoints(pc.PlayerId, pointsToAdd);
                    megaPoints = 0; // Clear MEGA points after conversion

                    Utils.SendMessage($"Your MEGA evolution point has been converted to {pointsToAdd} normal points!", pc.PlayerId);
                }
            }
        }

        private void AddNormalPoints(byte playerId, int points)
        {
            evolverPoints += points;
            Utils.SendMessage($"You have been awarded {points} normal points!", playerId);
        }

        public static void ClearEvolverCache()
        {
            evolverCache.Clear();
        }

       
    }
}
