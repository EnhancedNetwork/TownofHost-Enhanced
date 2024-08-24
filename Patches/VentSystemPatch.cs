using AmongUs.GameOptions;
using Hazel;
using System;

namespace TOHE.Patches;

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Serialize))]
class ShipStatusSerializePatch
{
    public static void Prefix(ShipStatus __instance, /*[HarmonyArgument(0)] MessageWriter writer,*/ [HarmonyArgument(1)] bool initialState)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (initialState) return;
        bool cancel = false;
        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (pc.BlockVentInteraction())
                cancel = true;
        }
        var ventilationSystem = __instance.Systems[SystemTypes.Ventilation].Cast<VentilationSystem>();
        if (cancel && ventilationSystem != null && ventilationSystem.IsDirty)
        {
            Utils.SetAllVentInteractions();
            ventilationSystem.IsDirty = false;
        }

    }
}

[HarmonyPatch(typeof(VentilationSystem), nameof(VentilationSystem.Deteriorate))]
static class VentSystemDeterioratePatch
{
    public static Dictionary<byte, int> LastClosestVent;
    public static void Postfix(VentilationSystem __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.IntroDestroyed) return;
        foreach (var pc in PlayerControl.AllPlayerControls)
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
        if (!pc.AmOwner && !pc.IsModClient() && !pc.Data.IsDead && !pc.CanUseVent())
        {
            //foreach (var vent in ShipStatus.Instance.AllVents)
            //{
            //    if ()
            //        return true;
            //}

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
    /// Send rpc for block specifics vent use or all vents
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
                    if (!pc.CanUseVent())
                        ++vents;
                }
                List<NetworkedPlayerInfo> AllPlayers = [];
                foreach (var playerInfo in GameData.Instance.AllPlayers)
                {
                    if (playerInfo != null && !playerInfo.Disconnected)
                        AllPlayers.Add(playerInfo);
                }
                int maxVents = Math.Min(vents, AllPlayers.Count);
                int blockedVents = 0;
                writer.WritePacked(maxVents);
                foreach (var vent in pc.GetVentsFromClosest())
                {
                    if (!pc.CanUseVent())
                    {
                        writer.Write(AllPlayers[blockedVents].PlayerId);
                        writer.Write((byte)vent.Id);
                        ++blockedVents;
                    }
                    if (blockedVents >= maxVents)
                        break;
                }
                writer.WritePacked(__instance.PlayersInsideVents.Count);
                foreach (Il2CppSystem.Collections.Generic.KeyValuePair<byte, byte> keyValuePair2 in __instance.PlayersInsideVents)
                {
                    writer.Write(keyValuePair2.Key);
                    writer.Write(keyValuePair2.Value);
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
