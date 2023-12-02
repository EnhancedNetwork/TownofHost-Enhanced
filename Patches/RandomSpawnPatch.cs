using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TOHE;

class RandomSpawn
{
    [HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.SnapTo), typeof(Vector2), typeof(ushort))]
    public class CustomNetworkTransformPatch
    {
        public static Dictionary<byte, int> NumOfTP = new();
        public static void Postfix(CustomNetworkTransform __instance, [HarmonyArgument(0)] Vector2 position)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (position == new Vector2(-25f, 40f)) return; //If it's the first spring, RETURN
            if (GameStates.IsInTask)
            {
                var player = Main.AllPlayerControls.FirstOrDefault(p => p.NetTransform == __instance);
                if (player == null)
                {
                    Logger.Warn("Player is null", "RandomSpawn");
                    return;
                }

                //return if player GM
                if (player.Is(CustomRoles.GM))
                {
                    return;
                }

                NumOfTP[player.PlayerId]++;

                if (NumOfTP[player.PlayerId] == 1)
                {
                    //return if the map is not airship
                    if (Main.NormalOptions.MapId != 4)
                    {
                        return;
                    }

                    // Reset cooldown player
                    player.RpcResetAbilityCooldown();

                    //return if random spawn is off
                    if (!Options.RandomSpawn.GetBool())
                    {
                        return;
                    }

                    new AirshipSpawnMap().RandomTeleport(player);
                }
            }
        }
    }
    public abstract class SpawnMap
    {
        public virtual void RandomTeleport(PlayerControl player)
        {
            var selectRand = (Options.SpawnRandomLocation.GetBool() && Options.SpawnRandomVents.GetBool()) ? IRandom.Instance.Next(0, 101) 
                : Options.SpawnRandomLocation.GetBool() ? 50
                : Options.SpawnRandomVents.GetBool() ? 51 : -1; // -1: Not Random Spawn

            if (selectRand == -1) return;

            if (selectRand >= 0 && selectRand <= 50)
            {
                var location = GetLocation();
                Logger.Info($"{player.Data.PlayerName}:{location}", "Spawn Random Location");
                player.RpcTeleport(location);
            }
            if (selectRand >= 51 && selectRand <= 101)
            {
                Logger.Info($"{player.Data.PlayerName}", "Spawn Random Vent");
                player.RpcRandomVentTeleport();
            }
        }
        public abstract Vector2 GetLocation();
    }

    public class SkeldSpawnMap : SpawnMap
    {
        public Dictionary<string, Vector2> positions = new()
        {
            ["Cafeteria"] = new Vector2 (-1.0f, 3.0f),
            ["Weapons"] = new Vector2 (9.3f, 1.0f),
            ["O2"] = new Vector2 (6.5f, -3.8f),
            ["Navigation"] = new Vector2 (16.5f, -4.8f),
            ["Shields"] = new Vector2 (9.3f, -12.3f),
            ["Communications"] = new Vector2 (4.0f, -15.5f),
            ["Storage"] = new Vector2 (-1.5f, -15.5f),
            ["Admin"] = new Vector2 (4.5f, -7.9f),
            ["Electrical"] = new Vector2 (-7.5f, -8.8f),
            ["LowerEngine"] = new Vector2 (-17.0f, -13.5f),
            ["UpperEngine"] = new Vector2 (-17.0f, -1.3f),
            ["Security"] = new Vector2 (-13.5f, -5.5f),
            ["Reactor"] = new Vector2 (-20.5f, -5.5f),
            ["MedBay"] = new Vector2 (-9.0f, -4.0f)
        };
        public override Vector2 GetLocation()
        {
            return positions.ToArray().OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;
        }
    }
    public class MiraHQSpawnMap : SpawnMap
    {
        public Dictionary<string, Vector2> positions = new()
        {
            ["Cafeteria"] = new Vector2 (25.5f, 2.0f),
            ["Balcony"] = new Vector2 (24.0f, -2.0f),
            ["Storage"] = new Vector2 (19.5f, 4.0f),
            ["ThreeWay"] = new Vector2 (17.8f, 11.5f),
            ["Communications"] = new Vector2 (15.3f, 3.8f),
            ["MedBay"] = new Vector2 (15.5f, -0.5f),
            ["LockerRoom"] = new Vector2 (9.0f, 1.0f),
            ["Decontamination"] = new Vector2 (6.1f, 6.0f),
            ["Laboratory"] = new Vector2 (9.5f, 12.0f),
            ["Reactor"] = new Vector2 (2.5f, 10.5f),
            ["Launchpad"] = new Vector2 (-4.5f, 2.0f),
            ["Admin"] = new Vector2 (21.0f, 17.5f),
            ["Office"] = new Vector2 (15.0f, 19.0f),
            ["Greenhouse"] = new Vector2 (17.8f, 23.0f)
        };
        public override Vector2 GetLocation()
        {
            return positions.ToArray().OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;
        }
    }
    public class PolusSpawnMap : SpawnMap
    {
        public Dictionary<string, Vector2> positions = new()
        {
            ["Office1"] = new Vector2 (19.5f, -18.0f),
            ["Office2"] = new Vector2 (26.0f, -17.0f),
            ["Admin"] = new Vector2 (24.0f, -22.5f),
            ["Communications"] = new Vector2 (12.5f, -16.0f),
            ["Weapons"] = new Vector2 (12.0f, -23.5f),
            ["BoilerRoom"] = new Vector2 (2.3f, -24.0f),
            ["O2"] = new Vector2 (2.0f, -17.5f),
            ["Electrical"] = new Vector2 (9.5f, -12.5f),
            ["Security"] = new Vector2 (3.0f, -12.0f),
            ["Dropship"] = new Vector2 (16.7f, -3.0f),
            ["Storage"] = new Vector2 (20.5f, -12.0f),
            ["Rocket"] = new Vector2 (26.7f, -8.5f),
            ["Laboratory"] = new Vector2 (36.5f, -7.5f),
            ["Toilet"] = new Vector2 (34.0f, -10.0f),
            ["SpecimenRoom"] = new Vector2 (36.5f, -22.0f)
        };
        public override Vector2 GetLocation()
        {
            return positions.ToArray().OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;
        }
    }

    public class DleksSpawnMap : SpawnMap
    {
        public Dictionary<string, Vector2> positions = new SkeldSpawnMap().positions
            .ToDictionary(e => e.Key, e => new Vector2(-e.Value.x, e.Value.y));
        public override Vector2 GetLocation()
        {
            return positions.ToArray().OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;
        }
    }
    public class AirshipSpawnMap : SpawnMap
    {
        public Dictionary<string, Vector2> positions = new()
        {
            ["Brig"] = new Vector2 (-0.7f, 8.5f),
            ["Engine"] = new Vector2 (-0.7f, -1.0f),
            ["Kitchen"] = new Vector2 (-7.0f, -11.5f),
            ["CargoBay"] = new Vector2 (33.5f, -1.5f),
            ["Records"] = new Vector2 (20.0f, 10.5f),
            ["MainHall"] = new Vector2 (15.5f, 0.0f),
            ["NapRoom"] = new Vector2 (6.3f, 2.5f),
            ["MeetingRoom"] = new Vector2 (17.1f, 14.9f),
            ["GapRoom"] = new Vector2 (12.0f, 8.5f),
            ["Vault"] = new Vector2 (-8.9f, 12.2f),
            ["Communications"] = new Vector2 (-13.3f, 1.3f),
            ["Cockpit"] = new Vector2 (-23.5f, -1.6f),
            ["Armory"] = new Vector2 (-10.3f, -5.9f),
            ["ViewingDeck"] = new Vector2 (-13.7f, -12.6f),
            ["Security"] = new Vector2 (5.8f, -10.8f),
            ["Electrical"] = new Vector2 (16.3f, -8.8f),
            ["Medical"] = new Vector2 (29.0f, -6.2f),
            ["Toilet"] = new Vector2 (30.9f, 6.8f),
            ["Showers"] = new Vector2 (21.2f, -0.8f)
        };
        public override Vector2 GetLocation()
        {
            return Options.AirshipAdditionalSpawn.GetBool()
                ? positions.ToArray().OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value
                : positions.ToArray()[0..6].OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;
        }
    }
    public class FungleSpawnMap : SpawnMap
    {
        public Dictionary<string, Vector2> positions = new()
        {
            ["FirstSpawn"] = new Vector2(-9.8f, 3.4f),
            ["Dropship"] = new Vector2(-7.8f, 10.6f),
            ["Cafeteria"] = new Vector2(-16.4f, 7.3f),
            ["SplashZone"] = new Vector2(-15.6f, -1.8f),
            ["Shore"] = new Vector2(-22.8f, -0.6f),
            ["Kitchen"] = new Vector2(-15.5f, -7.5f),
            ["Dock"] = new Vector2(-23.1f, -7.0f),
            ["Storage"] = new Vector2(1.7f, 4.4f),
            ["MeetingRoom"] = new Vector2(-3.0f, -2.6f),
            ["TheDorm"] = new Vector2(2.6f, -1.3f),
            ["Laboratory"] = new Vector2(-4.3f, -8.6f),
            ["Jungle"] = new Vector2(0.8f, -11.7f),
            ["Greenhouse"] = new Vector2(9.3f, -9.8f),
            ["Reactor"] = new Vector2(22.3f, -7.0f),
            ["Lookout"] = new Vector2(9.5f, 1.2f),
            ["MiningPit"] = new Vector2(12.6f, 9.8f),
            ["UpperEngine"] = new Vector2(22.4f, 3.4f),
            ["Communications"] = new Vector2(22.2f, 13.7f)
        };
        public override Vector2 GetLocation()
        {
            return positions.ToArray().OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;
        }
    }
}