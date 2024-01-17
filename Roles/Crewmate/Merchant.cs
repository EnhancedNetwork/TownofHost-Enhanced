using System.Collections.Generic;
using System.Linq;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate
{
    internal class Merchant
    {
        private static readonly int Id = 8800;
        private static readonly List<byte> playerIdList = [];
        public static bool IsEnable = false;

        public static Dictionary<byte, int> addonsSold = [];
        public static Dictionary<byte, List<byte>> bribedKiller = [];

        private static List<CustomRoles> addons = [];

        private static readonly List<CustomRoles> helpfulAddons =
        [
            CustomRoles.Watcher,
            CustomRoles.Seer,
            CustomRoles.Bait,
            CustomRoles.Cyber,
            CustomRoles.Trapper,
            CustomRoles.Brakar, // Tiebreaker
            CustomRoles.Necroview,
            CustomRoles.Bewilder,
            CustomRoles.Burst,
            CustomRoles.Sleuth,
            CustomRoles.Autopsy,
            CustomRoles.Lucky
        ];

        private static readonly List<CustomRoles> harmfulAddons =
        [
            CustomRoles.Oblivious,
            //CustomRoles.Sunglasses,
            CustomRoles.VoidBallot,
            CustomRoles.Fragile,
            CustomRoles.Unreportable, // Disregarded
            CustomRoles.Unlucky
        ];

        private static readonly List<CustomRoles> neutralAddons =
        [
            CustomRoles.Guesser,
            CustomRoles.Diseased,
            CustomRoles.Antidote,
            CustomRoles.Aware,
            CustomRoles.Gravestone,
            //CustomRoles.Glow,
            CustomRoles.Onbound,
            CustomRoles.Stubborn,
            CustomRoles.Rebound,
        ];

        private static OptionItem OptionMaxSell;
        private static OptionItem OptionMoneyPerSell;
        private static OptionItem OptionMoneyRequiredToBribe;
        private static OptionItem OptionNotifyBribery;
        private static OptionItem OptionCanTargetCrew;
        private static OptionItem OptionCanTargetImpostor;
        private static OptionItem OptionCanTargetNeutral;
        private static OptionItem OptionCanSellHelpful;
        private static OptionItem OptionCanSellHarmful;
        private static OptionItem OptionCanSellNeutral;
        private static OptionItem OptionSellOnlyHarmfulToEvil;
        private static OptionItem OptionSellOnlyHelpfulToCrew;
        private static OptionItem OptionSellOnlyEnabledAddons;

        private static int GetCurrentAmountOfMoney(byte playerId)
        {
            return (addonsSold[playerId] * OptionMoneyPerSell.GetInt()) - (bribedKiller[playerId].Count * OptionMoneyRequiredToBribe.GetInt());
        }

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Merchant);
            OptionMaxSell = IntegerOptionItem.Create(Id + 2, "MerchantMaxSell", new(1, 99, 1), 5, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Merchant]).SetValueFormat(OptionFormat.Times);
            OptionMoneyPerSell = IntegerOptionItem.Create(Id + 3, "MerchantMoneyPerSell", new(1, 99, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Merchant]).SetValueFormat(OptionFormat.Times);
            OptionMoneyRequiredToBribe = IntegerOptionItem.Create(Id + 4, "MerchantMoneyRequiredToBribe", new(1, 99, 1), 5, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Merchant]).SetValueFormat(OptionFormat.Times);
            OptionNotifyBribery = BooleanOptionItem.Create(Id + 5, "MerchantNotifyBribery", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Merchant]);
            OptionCanTargetCrew = BooleanOptionItem.Create(Id + 6, "MerchantTargetCrew", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Merchant]);
            OptionCanTargetImpostor = BooleanOptionItem.Create(Id + 7, "MerchantTargetImpostor", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Merchant]);
            OptionCanTargetNeutral = BooleanOptionItem.Create(Id + 8, "MerchantTargetNeutral", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Merchant]);
            OptionCanSellHelpful = BooleanOptionItem.Create(Id + 9, "MerchantSellHelpful", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Merchant]);
            OptionCanSellHarmful = BooleanOptionItem.Create(Id + 10, "MerchantSellHarmful", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Merchant]);
            OptionCanSellNeutral = BooleanOptionItem.Create(Id + 11, "MerchantSellMixed", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Merchant]);
            OptionSellOnlyHarmfulToEvil = BooleanOptionItem.Create(Id + 13, "MerchantSellHarmfulToEvil", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Merchant]);
            OptionSellOnlyHelpfulToCrew = BooleanOptionItem.Create(Id + 14, "MerchantSellHelpfulToCrew", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Merchant]);
            OptionSellOnlyEnabledAddons = BooleanOptionItem.Create(Id + 15, "MerchantSellOnlyEnabledAddons",false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Merchant]);

            OverrideTasksData.Create(Id + 16, TabGroup.CrewmateRoles, CustomRoles.Merchant);
        }
        public static void Init()
        {
            playerIdList.Clear();
            IsEnable = false;

            addons = [];
            addonsSold = [];
            bribedKiller = [];

            if (OptionCanSellHelpful.GetBool())
            {
                addons.AddRange(helpfulAddons);
            }

            if (OptionCanSellHarmful.GetBool())
            {
                addons.AddRange(harmfulAddons);
            }

            if (OptionCanSellNeutral.GetBool())
            {
                addons.AddRange(neutralAddons);
            }
            if (OptionSellOnlyEnabledAddons.GetBool())
            { 
                addons = addons.Where(role => role.GetMode() != 0).ToList();
            }
        }

        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            addonsSold.Add(playerId, 0);
            bribedKiller.Add(playerId, []);
            IsEnable = true;
        }
        public static void Remove(byte playerId)
        {
            playerIdList.Remove(playerId);
            addonsSold.Remove(playerId);
            bribedKiller.Remove(playerId);
        }

        public static void OnTaskFinished(PlayerControl player)
        {
            if (!player.IsAlive() || !player.Is(CustomRoles.Merchant) || (addonsSold[player.PlayerId] >= OptionMaxSell.GetInt()))
            {
                return;
            }
            if (addons.Count == 0)
            {
                player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Merchant), GetString("MerchantAddonSellFail")));
                Logger.Info("No addons to sell.", "Merchant");
                return;
            }

            var rd = IRandom.Instance;
            CustomRoles addon = addons[rd.Next(0, addons.Count)];
            
            List<PlayerControl> AllAlivePlayer =
                Main.AllAlivePlayerControls.Where(x =>
                    x.PlayerId != player.PlayerId
                    &&
                    !x.Is(addon)
                    &&
                    CustomRolesHelper.CheckAddonConfilct(addon, x)
                    &&
                    (Cleanser.CleansedCanGetAddon.GetBool() || (!Cleanser.CleansedCanGetAddon.GetBool() && !x.Is(CustomRoles.Cleansed)))
                    &&
                    (!x.Is(CustomRoles.Stubborn))
                    &&
                    (
                        (OptionCanTargetCrew.GetBool() && x.GetCustomRole().IsCrewmate()) 
                        ||
                        (OptionCanTargetImpostor.GetBool() && x.GetCustomRole().IsImpostor())
                        ||
                        (OptionCanTargetNeutral.GetBool() && x.GetCustomRole().IsNeutral())
                    )
                ).ToList();

            if (AllAlivePlayer.Count > 0)
            {
                bool helpfulAddon = helpfulAddons.Contains(addon);
                bool harmfulAddon = !helpfulAddon;

                if (helpfulAddon && OptionSellOnlyHarmfulToEvil.GetBool())
                {
                    AllAlivePlayer = AllAlivePlayer.Where(a => a.GetCustomRole().IsCrewmate()).ToList();
                }

                if (harmfulAddon && OptionSellOnlyHelpfulToCrew.GetBool())
                {
                    AllAlivePlayer = AllAlivePlayer.Where(a =>
                        a.GetCustomRole().IsImpostor()
                        ||
                        a.GetCustomRole().IsNeutral()
                        
                    ).ToList();
                }

                if (AllAlivePlayer.Count == 0)
                {
                    player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Merchant), GetString("MerchantAddonSellFail")));
                    Logger.Info("All Alive Player Count = 0", "Merchant");
                    return;
                }

                PlayerControl target = AllAlivePlayer[rd.Next(0, AllAlivePlayer.Count)];

                target.RpcSetCustomRole(addon);
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Merchant), GetString("MerchantAddonSell")));
                player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Merchant), GetString("MerchantAddonDelivered")));

                addonsSold[player.PlayerId] += 1;
            }
        }

        public static bool OnClientMurder(PlayerControl killer, PlayerControl target)
        {
            if (bribedKiller[target.PlayerId].Contains(killer.PlayerId))
            {
                NotifyBribery(killer, target);
                return true;
            }

            if (GetCurrentAmountOfMoney(target.PlayerId) >= OptionMoneyRequiredToBribe.GetInt())
            {
                NotifyBribery(killer, target);
                bribedKiller[target.PlayerId].Add(killer.PlayerId);
                return true;
            }

            return false;
        }

        public static bool IsBribedKiller(PlayerControl killer, PlayerControl target)
        {
            return bribedKiller[target.PlayerId].Contains(killer.PlayerId);
        }

        private static void NotifyBribery(PlayerControl killer, PlayerControl target)
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Merchant), GetString("BribedByMerchant")));

            if (OptionNotifyBribery.GetBool())
            {
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Merchant), GetString("MerchantKillAttemptBribed")));
            }
        }
    }
}
