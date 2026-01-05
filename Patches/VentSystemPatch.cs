using Hazel;
using System;

namespace TOHE.Patches;

// Patches here is also activated from ShipStatus.Serialize and IntroCutScene 
// through Utils.SetAllVentInteractions

[HarmonyPatch(typeof(VentilationSystem), nameof(VentilationSystem.PerformVentOp))]
static class PerformVentOpPatch
{
    public static bool Prefix(VentilationSystem __instance, [HarmonyArgument(0)] byte playerId, [HarmonyArgument(1)] VentilationSystem.Operation op/*, [HarmonyArgument(2)] byte ventId*/, [HarmonyArgument(3)] SequenceBuffer<VentilationSystem.VentMoveInfo> seqBuffer)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        if (Utils.GetPlayerById(playerId) == null) return true;
        switch (op)
        {
            case VentilationSystem.Operation.Move:
                if (!__instance.PlayersInsideVents.ContainsKey(playerId))
                {
                    seqBuffer.BumpSid();
                    return false;
                }

                break;
        }

        return true;
    }
}
[HarmonyPatch(typeof(VentilationSystem), nameof(VentilationSystem.Deteriorate))]
static class VentSystemDeterioratePatch
{
    public static Dictionary<byte, int> LastClosestVent = [];
    public static Dictionary<byte, bool> PlayerHadBlockedVentLastTime = [];
    public static bool ForceUpadate;

    public static void Postfix()
    {
        if (!AmongUsClient.Instance.AmHost || !Main.IntroDestroyed || GameStates.IsMeeting) return;

        if (ForceUpadate || FixedUpdateInNormalGamePatch.BufferTime.GetValueOrDefault((byte)0, 0) % 6 == 0)
        {
            var needUpdate = false;
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (pc.BlockVentInteraction())
                {
                    var closestVents = pc.GetVentsFromClosest()[0].Id;
                    if (ForceUpadate || closestVents != LastClosestVent.GetValueOrDefault(pc.PlayerId, 99))
                    {
                        PlayerHadBlockedVentLastTime[pc.PlayerId] = true;
                        LastClosestVent[pc.PlayerId] = closestVents;
                        needUpdate = true;
                    }
                }
                else if (PlayerHadBlockedVentLastTime[pc.PlayerId])
                {
                    PlayerHadBlockedVentLastTime[pc.PlayerId] = false;
                    LastClosestVent[pc.PlayerId] = 99;
                    needUpdate = true;
                }
            }

            if (needUpdate)
                ShipStatus.Instance.Systems[SystemTypes.Ventilation].CastFast<VentilationSystem>().IsDirty = true;
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
        foreach (var pc in Main.AllAlivePlayerControls)
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
        MessageWriter writer = MessageWriter.Get(ExtendedPlayerControl.RpcSendOption);
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
        MessageWriter writer = MessageWriter.Get(ExtendedPlayerControl.RpcSendOption);
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
