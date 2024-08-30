using Hazel;
using System;

namespace TOHE.Patches;

// Patches here is also activated from ShipStatus.Serialize and IntroCutScene 
// through Utils.SetAllVentInteractions

[HarmonyPatch(typeof(VentilationSystem), nameof(VentilationSystem.Deteriorate))]
static class VentSystemDeterioratePatch
{
    public static Dictionary<byte, int> LastClosestVent;
    public static Dictionary<byte, int> LastClosedAtVentId;
    public static void Postfix(VentilationSystem __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.IntroDestroyed) return;
        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            LastClosestVent[pc.PlayerId] = pc.GetVentsFromClosest()[0].Id;
            if (pc.BlockVentInteraction())
            {
                var readyVents = pc.GetVentsFromClosest(9f);
                int readyVentId = (readyVents == null || readyVents.Count == 0) ? -1 : readyVents[0].Id;

                if (LastClosedAtVentId[pc.PlayerId] != readyVentId)
                {
                    pc.RpcCloseVent(__instance);
                }
            }
        }
    }

    /// <summary>
    /// Check blocking vents
    /// </summary>
    public static bool BlockVentInteraction(this PlayerControl pc)
    {
        if (!pc.AmOwner && !pc.IsModClient() && !pc.Data.IsDead && !pc.CanUseVent())
        {
            return true;
        }
        return false;
    }

    public static void SerializeV2(VentilationSystem __instance, PlayerControl player = null)
    {
        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (pc.AmOwner || (player != null && pc != player)) continue;
            if (pc.BlockVentInteraction())
            {
                pc.RpcCloseVent(__instance);
            }
            else
            {
                pc.RpcSerializeVent(__instance);
            }
        }
    }

    /// <summary>
    /// Send rpc for blocking specifics vent use or all vents
    /// </summary>
    public static void RpcCloseVent(this PlayerControl pc, VentilationSystem __instance = null)
    {
        if (__instance == null)
        {
            __instance = ShipStatus.Instance.Systems[SystemTypes.Ventilation].Cast<VentilationSystem>();

            if (__instance == null) return;
        }

        List<Vent> readyVents = pc.GetVentsFromClosest();
        
        LastClosedAtVentId[pc.PlayerId] = !(readyVents == null || readyVents.Count < 1) ? readyVents[0].Id : -1;

        if (readyVents.Count < 1) return;

        MessageWriter writer = MessageWriter.Get(SendOption.None);
        writer.StartMessage(6);
        writer.Write(AmongUsClient.Instance.GameId);
        writer.WritePacked(pc.GetClientId());
        {
            writer.StartMessage(1);
            writer.WritePacked(ShipStatus.Instance.NetId);
            {
                writer.StartMessage((byte)SystemTypes.Ventilation);
                int vents = readyVents.Count;
                List<NetworkedPlayerInfo> AllPlayers = [];
                foreach (var playerInfo in GameData.Instance.AllPlayers)
                {
                    if (playerInfo != null && !playerInfo.Disconnected)
                        AllPlayers.Add(playerInfo);
                }
                int maxVents = Math.Min(vents, AllPlayers.Count);
                int blockedVents = 0;
                writer.WritePacked(maxVents);
                foreach (var vent in readyVents)
                {
                    writer.Write(AllPlayers[blockedVents].PlayerId);
                    writer.Write((byte)vent.Id);
                    ++blockedVents;
                    if (blockedVents >= maxVents)
                        break;
                }

                writer.WritePacked(0); // No need to Serialize Player Inside Vents to DisableVents player
                /*
                writer.WritePacked(__instance.PlayersInsideVents.Count);
                foreach (Il2CppSystem.Collections.Generic.KeyValuePair<byte, byte> keyValuePair2 in __instance.PlayersInsideVents)
                {
                    writer.Write(keyValuePair2.Key);
                    writer.Write(keyValuePair2.Value);
                }
                */

                writer.EndMessage();
            }
            writer.EndMessage();
        }
        writer.EndMessage();
        AmongUsClient.Instance.SendOrDisconnect(writer);
        writer.Recycle();
    }

    private static void RpcSerializeVent(this PlayerControl pc, VentilationSystem __instance = null)
    {
        if (__instance == null)
        {
            __instance = ShipStatus.Instance.Systems[SystemTypes.Ventilation].Cast<VentilationSystem>();

            if (__instance == null) return;
        }

        MessageWriter writer = MessageWriter.Get(SendOption.None);
        writer.StartMessage(6);
        writer.Write(AmongUsClient.Instance.GameId);
        writer.WritePacked(pc.GetClientId());
        {
            writer.StartMessage(1);
            writer.WritePacked(ShipStatus.Instance.NetId);
            {
                writer.StartMessage((byte)SystemTypes.Ventilation);
                {
                    __instance.Serialize(writer, false);
                }
                writer.EndMessage();
            }
            writer.EndMessage();
        }
        writer.EndMessage();
        AmongUsClient.Instance.SendOrDisconnect(writer);
        writer.Recycle();
    }
}
