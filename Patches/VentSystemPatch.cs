using Hazel;
using System;

namespace TOHE.Patches;

// Patches here is also activated from ShipStatus.Serialize and IntroCutScene 
// through Utils.SetAllVentInteractions

[HarmonyPatch(typeof(VentilationSystem), nameof(VentilationSystem.Deteriorate))]
static class VentSystemDeterioratePatch
{
    public static Dictionary<byte, int> LastClosestVent;
    public static void Postfix(VentilationSystem __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.IntroDestroyed) return;
        foreach (var pc in PlayerControl.AllPlayerControls.GetFastEnumerator())
        {
            if (pc.BlockVentInteraction())
            {
                LastClosestVent[pc.PlayerId] = pc.GetVentsFromClosest()[0].Id;
                pc.RpcCloseVent(__instance);
            }
        }
    }
    /// <summary>
    /// Check blocking vents
    /// </summary>
    public static bool BlockVentInteraction(this PlayerControl pc)
    {
        if (!pc.AmOwner && pc.IsAlive() && (!pc.CanUseVents() || pc.HasAnyBlockedVent()))
        {
            return true;
        }
        return false;
    }

    public static void SerializeV2(VentilationSystem __instance, PlayerControl player = null)
    {
        foreach (var pc in PlayerControl.AllPlayerControls.GetFastEnumerator())
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
    private static void RpcCloseVent(this PlayerControl pc, VentilationSystem __instance)
    {
        MessageWriter writer = MessageWriter.Get(SendOption.None);
        writer.StartMessage(6);
        writer.Write(AmongUsClient.Instance.GameId);
        writer.WritePacked(pc.GetClientId());
        {
            writer.StartMessage(1);
            writer.WritePacked(ShipStatus.Instance.NetId);
            {
                writer.StartMessage((byte)SystemTypes.Ventilation);
                int vents = 0;
                foreach (var vent in ShipStatus.Instance.AllVents)
                {
                    if (pc.CantUseVent(vent.Id))
                        ++vents;
                }
                List<NetworkedPlayerInfo> AllPlayers = [];
                foreach (var playerInfo in GameData.Instance.AllPlayers.GetFastEnumerator())
                {
                    if (playerInfo != null && !playerInfo.Disconnected)
                        AllPlayers.Add(playerInfo);
                }
                int maxVents = Math.Min(vents, AllPlayers.Count);
                int blockedVents = 0;
                writer.WritePacked(maxVents);
                foreach (var vent in pc.GetVentsFromClosest())
                {
                    if (pc.CantUseVent(vent.Id))
                    {
                        writer.Write(AllPlayers[blockedVents].PlayerId);
                        writer.Write((byte)vent.Id);
                        ++blockedVents;
                    }
                    if (blockedVents >= maxVents)
                        break;
                }
                writer.WritePacked(__instance.PlayersInsideVents.Count);
                foreach (var (playerId, ventId) in __instance.PlayersInsideVents)
                {
                    writer.Write(playerId);
                    writer.Write(ventId);
                }
                writer.EndMessage();
            }
            writer.EndMessage();
        }
        writer.EndMessage();
        AmongUsClient.Instance.SendOrDisconnect(writer);
        writer.Recycle();
    }

    private static void RpcSerializeVent(this PlayerControl pc, VentilationSystem __instance)
    {
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
[HarmonyPatch(typeof(VentilationSystem), nameof(VentilationSystem.IsVentCurrentlyBeingCleaned))]
static class VentSystemIsVentCurrentlyBeingCleanedPatch
{
    // Patch block use vent for host becouse host always skips RpcSerializeVent
    public static bool Prefix([HarmonyArgument(0)] int id, ref bool __result)
    {
        if (!AmongUsClient.Instance.AmHost) return true;

        if (PlayerControl.LocalPlayer.CantUseVent(id))
        {
            __result = true;
            return false;
        }

        // Run original code if host not have bloked vent
        return true;
    }
}