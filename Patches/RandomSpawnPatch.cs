using Hazel;
using System;
using TOHE.Roles.Core;
using TOHE.Roles.Impostor;
using UnityEngine;

namespace TOHE;

// Thanks: https://github.com/tukasa0001/TownOfHost/blob/main/Patches/RandomSpawnPatch.cs
class RandomSpawn
{
    [HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.SnapTo))]
    [HarmonyPatch([typeof(Vector2), typeof(ushort)])]
    public class SnapToPatch
    {
        public static void Prefix(CustomNetworkTransform __instance, [HarmonyArgument(1)] ushort minSid)
        {
            if (AmongUsClient.Instance.AmHost) return;
            if (__instance.myPlayer.PlayerId == 255) return;
            Logger.Info($"Player Id {__instance.myPlayer.PlayerId} - old sequence {__instance.lastSequenceId} - new sequence {minSid}", "SnapToPatch");
        }
    }
    [HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.HandleRpc))]
    public class CustomNetworkTransformHandleRpcPatch
    {
        public static bool Prefix(CustomNetworkTransform __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
        {
            if (!AmongUsClient.Instance.AmHost) return true;

            if (!__instance.isActiveAndEnabled)
            {
                return false;
            }
            if ((RpcCalls)callId == RpcCalls.SnapTo && GameStates.AirshipIsActive)
            {
                var player = __instance.myPlayer;

                // Players haven't spawned yet
                if (!Main.PlayerStates[player.PlayerId].HasSpawned)
                {
                    // Read the coordinates of the SnapTo destination
                    Vector2 position;
                    {
                        var newReader = MessageReader.Get(reader);
                        position = NetHelpers.ReadVector2(newReader);
                        newReader.Recycle();
                    }
                    Logger.Info($"SnapTo: {player.GetRealName()}, ({position.x}, {position.y})", "RandomSpawn");

                    // if the SnapTo destination is a spring location, proceed to the spring process
                    if (IsAirshipVanillaSpawnPosition(position))
                    {
                        AirshipSpawn(player);
                        return !IsRandomSpawn();
                    }
                    else
                    {
                        Logger.Info("Position is not a spring position", "RandomSpawn");
                    }
                }
            }
            return true;
        }

        private static bool IsAirshipVanillaSpawnPosition(Vector2 position)
        {
            // Using the fact that the coordinates of the spring position are in increments of 0.1
            //The comparison is made with an int type in which the coordinates are multiplied by 10
            //As a countermeasure against errors of the float type and the expansion of errors due to the implementation of ReadVector2
            var decupleXFloat = position.x * 10f;
            var decupleYFloat = position.y * 10f;
            var decupleXInt = Mathf.RoundToInt(decupleXFloat);

            // if the difference between the values multiplied by 10 is closer than 0.1,
            //The original coordinates are not in increments of 0.1, so it is not a spring position.
            if (Mathf.Abs(decupleXInt - decupleXFloat) >= 0.09f)
            {
                return false;
            }
            var decupleYInt = Mathf.RoundToInt(decupleYFloat);
            if (Mathf.Abs(decupleYInt - decupleYFloat) >= 0.09f)
            {
                return false;
            }
            var decuplePosition = (decupleXInt, decupleYInt);
            return decupleVanillaSpawnPositions.Contains(decuplePosition);
        }
        /// <summary>For comparison Ten times the vanilla spring position of the airship</summary>
        private static readonly HashSet<(int x, int y)> decupleVanillaSpawnPositions =
            [
                (-7, 85),  // Walkway in front of the dormitory
                (-7, -10),  // Engine
                (-70, -115),  // Kitchen
                (335, -15),  // Cargo
                (200, 105),  // Archive
                (155, 0),  // Main Hall
            ];
    }
    [HarmonyPatch(typeof(SpawnInMinigame), nameof(SpawnInMinigame.SpawnAt))]
    public static class SpawnInMinigameSpawnAtPatch
    {
        public static bool Prefix(SpawnInMinigame __instance, [HarmonyArgument(0)] SpawnInMinigame.SpawnLocation spawnPoint)
        {
            if (!AmongUsClient.Instance.AmHost)
            {
                return true;
            }

            if (__instance.amClosing != Minigame.CloseState.None)
            {
                return false;
            }
            // Cancel vanilla upwelling if random spawn is enabled
            if (IsRandomSpawn())
            {
                // Vanilla process RpcSnapTo replaced with AirshipSpawn
                __instance.gotButton = true;
                PlayerControl.LocalPlayer.SetKinematic(true);
                PlayerControl.LocalPlayer.NetTransform.SetPaused(true);
                AirshipSpawn(PlayerControl.LocalPlayer);
                DestroyableSingleton<HudManager>.Instance.PlayerCam.SnapToTarget();
                __instance.StopAllCoroutines();
                __instance.StartCoroutine(__instance.CoSpawnAt(PlayerControl.LocalPlayer, spawnPoint));
                return false;
            }
            else
            {
                AirshipSpawn(PlayerControl.LocalPlayer);
                return true;
            }
        }
    }
    public static void AirshipSpawn(PlayerControl player)
    {
        Logger.Info($"Spawn: {player.GetRealName()}", "RandomSpawn");
        if (AmongUsClient.Instance.AmHost)
        {
            if (player.GetRoleClass() is Penguin pg)
            {
                pg.OnSpawnAirship();
            }
            if (GameStates.IsNormalGame)
            {
                // Reset cooldown player
                player.RpcResetAbilityCooldown();
            }

            if (IsRandomSpawn())
            {
                new AirshipSpawnMap().RandomTeleport(player);
            }
            else if (player.Is(CustomRoles.GM))
            {
                new AirshipSpawnMap().FirstTeleport(player);
            }
        }
        Main.PlayerStates[player.PlayerId].HasSpawned = true;
    }
    public static bool IsRandomSpawn() => RandomSpawnMode.GetBool();
    public static bool CanSpawnInFirstRound() => SpawnInFirstRound.GetBool();

    [Obfuscation(Exclude = true)]
    private enum RandomSpawnOpt
    {
        RandomSpawnMode,
        RandomSpawn_SpawnInFirstRound,
        RandomSpawn_SpawnRandomLocation,
        RandomSpawn_AirshipAdditionalSpawn,
        RandomSpawn_SpawnRandomVents
    }

    private static OptionItem RandomSpawnMode;
    private static OptionItem SpawnInFirstRound;
    private static OptionItem SpawnRandomLocation;
    private static OptionItem AirshipAdditionalSpawn;
    private static OptionItem SpawnRandomVents;

    public static void SetupCustomOption()
    {
        RandomSpawnMode = BooleanOptionItem.Create(60470, RandomSpawnOpt.RandomSpawnMode, false, TabGroup.ModSettings, false)
            .HideInFFA()
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        SpawnInFirstRound = BooleanOptionItem.Create(60476, RandomSpawnOpt.RandomSpawn_SpawnInFirstRound, true, TabGroup.ModSettings, false)
            .SetParent(RandomSpawnMode);
        SpawnRandomLocation = BooleanOptionItem.Create(60471, RandomSpawnOpt.RandomSpawn_SpawnRandomLocation, true, TabGroup.ModSettings, false)
            .SetParent(RandomSpawnMode);
        AirshipAdditionalSpawn = BooleanOptionItem.Create(60472, RandomSpawnOpt.RandomSpawn_AirshipAdditionalSpawn, true, TabGroup.ModSettings, false)
            .SetParent(SpawnRandomLocation);
        SpawnRandomVents = BooleanOptionItem.Create(60475, RandomSpawnOpt.RandomSpawn_SpawnRandomVents, false, TabGroup.ModSettings, false)
            .SetParent(RandomSpawnMode);
    }

    public abstract class SpawnMap
    {
        public abstract Dictionary<string, Vector2> Positions { get; }
        public virtual void RandomTeleport(PlayerControl player)
        {
            Teleport(player, true);
        }
        public virtual void FirstTeleport(PlayerControl player)
        {
            Teleport(player, false);
        }

        private void Teleport(PlayerControl player, bool isRadndom)
        {
            int selectRandomSpawn;

            if (isRadndom && Options.CurrentGameMode != CustomGameMode.FFA)
            {
                selectRandomSpawn = SpawnRandomLocation.GetBool() ? 1 : 2;

                if (SpawnRandomLocation.GetBool() && SpawnRandomVents.GetBool())
                {
                    var rand = IRandom.Instance;
                    selectRandomSpawn = rand.Next(1, 3); // 1 or 2
                }
                else if (!SpawnRandomLocation.GetBool() && !SpawnRandomVents.GetBool())
                {
                    selectRandomSpawn = 0;
                }
            }
            else selectRandomSpawn = 1;

            if (selectRandomSpawn == 1)
            {
                var location = GetLocation(!isRadndom);
                Logger.Info($"{player.Data.PlayerName}:{location}", "RandomSpawnInLocation");
                player.RpcTeleport(location, isRandomSpawn: true);
            }
            else
            {
                Logger.Info($"{player.Data.PlayerName}", "RandomSpawnInVent");
                player.RpcRandomVentTeleport();
            }
        }
        public Vector2 GetLocation(bool first = false)
        {
            var locations = Positions.ToArray();
            if (first) return locations[0].Value;

            var location = locations.ToArray().OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault();

            if (GameStates.AirshipIsActive && !AirshipAdditionalSpawn.GetBool())
                location = locations.ToArray()[0..6].OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault();

            return location.Value;
        }
    }

    public class SkeldSpawnMap : SpawnMap
    {
        public override Dictionary<string, Vector2> Positions { get; } = new()
        {
            ["Cafeteria"] = new Vector2(-1.0f, 3.0f),
            ["Weapons"] = new Vector2(9.3f, 1.0f),
            ["O2"] = new Vector2(6.5f, -3.8f),
            ["Navigation"] = new Vector2(16.5f, -4.8f),
            ["Shields"] = new Vector2(9.3f, -12.3f),
            ["Communications"] = new Vector2(4.0f, -15.5f),
            ["Storage"] = new Vector2(-1.5f, -15.5f),
            ["Admin"] = new Vector2(4.5f, -7.9f),
            ["Electrical"] = new Vector2(-7.5f, -8.8f),
            ["LowerEngine"] = new Vector2(-17.0f, -13.5f),
            ["UpperEngine"] = new Vector2(-17.0f, -1.3f),
            ["Security"] = new Vector2(-13.5f, -5.5f),
            ["Reactor"] = new Vector2(-20.5f, -5.5f),
            ["MedBay"] = new Vector2(-9.0f, -4.0f)
        };
    }
    public class MiraHQSpawnMap : SpawnMap
    {
        public override Dictionary<string, Vector2> Positions { get; } = new()
        {
            ["Cafeteria"] = new Vector2(25.5f, 2.0f),
            ["Balcony"] = new Vector2(24.0f, -2.0f),
            ["Storage"] = new Vector2(19.5f, 4.0f),
            ["ThreeWay"] = new Vector2(17.8f, 11.5f),
            ["Communications"] = new Vector2(15.3f, 3.8f),
            ["MedBay"] = new Vector2(15.5f, -0.5f),
            ["LockerRoom"] = new Vector2(9.0f, 1.0f),
            ["Decontamination"] = new Vector2(6.1f, 6.0f),
            ["Laboratory"] = new Vector2(9.5f, 12.0f),
            ["Reactor"] = new Vector2(2.5f, 10.5f),
            ["Launchpad"] = new Vector2(-4.5f, 2.0f),
            ["Admin"] = new Vector2(21.0f, 17.5f),
            ["Office"] = new Vector2(15.0f, 19.0f),
            ["Greenhouse"] = new Vector2(17.8f, 23.0f)
        };
    }
    public class PolusSpawnMap : SpawnMap
    {
        public override Dictionary<string, Vector2> Positions { get; } = new()
        {
            ["OfficeLeft"] = new Vector2(19.5f, -18.0f),
            ["OfficeRight"] = new Vector2(26.0f, -17.0f),
            ["Admin"] = new Vector2(24.0f, -22.5f),
            ["Communications"] = new Vector2(12.5f, -16.0f),
            ["Weapons"] = new Vector2(12.0f, -23.5f),
            ["BoilerRoom"] = new Vector2(2.3f, -24.0f),
            ["O2"] = new Vector2(2.0f, -17.5f),
            ["Electrical"] = new Vector2(9.5f, -12.5f),
            ["Security"] = new Vector2(3.0f, -12.0f),
            ["Dropship"] = new Vector2(16.7f, -3.0f),
            ["Storage"] = new Vector2(20.5f, -12.0f),
            ["Rocket"] = new Vector2(26.7f, -8.5f),
            ["Laboratory"] = new Vector2(36.5f, -7.5f),
            ["Toilet"] = new Vector2(34.0f, -10.0f),
            ["SpecimenRoom"] = new Vector2(36.5f, -22.0f)
        };
    }

    public class DleksSpawnMap : SpawnMap
    {
        public static Dictionary<string, Vector2> TempPositions = new SkeldSpawnMap().Positions
            .ToDictionary(e => e.Key, e => new Vector2(-e.Value.x, e.Value.y));

        public override Dictionary<string, Vector2> Positions { get; } = TempPositions;
    }
    public class AirshipSpawnMap : SpawnMap
    {
        public override Dictionary<string, Vector2> Positions { get; } = new()
        {
            ["Brig"] = new Vector2(-0.7f, 8.5f),
            ["Engine"] = new Vector2(-0.7f, -1.0f),
            ["Kitchen"] = new Vector2(-7.0f, -11.5f),
            ["CargoBay"] = new Vector2(33.5f, -1.5f),
            ["Records"] = new Vector2(20.0f, 10.5f),
            ["MainHall"] = new Vector2(15.5f, 0.0f),
            ["NapRoom"] = new Vector2(6.3f, 2.5f),
            ["MeetingRoom"] = new Vector2(17.1f, 14.9f),
            ["GapRoom"] = new Vector2(12.0f, 8.5f),
            ["Vault"] = new Vector2(-8.9f, 12.2f),
            ["Communications"] = new Vector2(-13.3f, 1.3f),
            ["Cockpit"] = new Vector2(-23.5f, -1.6f),
            ["Armory"] = new Vector2(-10.3f, -5.9f),
            ["ViewingDeck"] = new Vector2(-13.7f, -12.6f),
            ["Security"] = new Vector2(5.8f, -10.8f),
            ["Electrical"] = new Vector2(16.3f, -8.8f),
            ["Medical"] = new Vector2(29.0f, -6.2f),
            ["Toilet"] = new Vector2(30.9f, 6.8f),
            ["Showers"] = new Vector2(21.2f, -0.8f)
        };
    }
    public class FungleSpawnMap : SpawnMap
    {
        public override Dictionary<string, Vector2> Positions { get; } = new()
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
    }
}
