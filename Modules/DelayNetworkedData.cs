using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using System;
using UnityEngine;

namespace TOHE.Modules.DelayNetworkDataSpawn;

[HarmonyPatch(typeof(InnerNetClient))]
public class InnerNetClientPatch
{
    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.SendInitialData))]
    [HarmonyPrefix]
    public static bool SendInitialDataPrefix(InnerNetClient __instance, int clientId)
    {
        if (!Constants.IsVersionModded() || __instance.NetworkMode != NetworkModes.OnlineGame) return true;
        // We make sure other stuffs like playercontrol and Lobby behavior is spawned properly
        // Then we spawn networked data for new clients
        MessageWriter messageWriter = MessageWriter.Get(SendOption.Reliable);
        messageWriter.StartMessage(6);
        messageWriter.Write(__instance.GameId);
        messageWriter.WritePacked(clientId);
        Il2CppSystem.Collections.Generic.List<InnerNetObject> obj = __instance.allObjects;
        lock (obj)
        {
            HashSet<GameObject> hashSet = [];
            for (int i = 0; i < __instance.allObjects.Count; i++)
            {
                InnerNetObject innerNetObject = __instance.allObjects[i];
                if (innerNetObject && (innerNetObject.OwnerId != -4 || __instance.AmModdedHost) && hashSet.Add(innerNetObject.gameObject))
                {
                    GameManager gameManager = innerNetObject as GameManager;
                    if (gameManager != null)
                    {
                        __instance.SendGameManager(clientId, gameManager);
                    }
                    else
                    {
                        if (innerNetObject is not NetworkedPlayerInfo)
                            __instance.WriteSpawnMessage(innerNetObject, innerNetObject.OwnerId, innerNetObject.SpawnFlags, messageWriter);
                    }
                }
            }
            messageWriter.EndMessage();
            // Logger.Info($"send first data to {clientId}, size is {messageWriter.Length}", "SendInitialDataPrefix");
            __instance.SendOrDisconnect(messageWriter);
            messageWriter.Recycle();
        }
        DelaySpawnPlayerInfo(__instance, clientId);
        return false;
    }

    private static void DelaySpawnPlayerInfo(InnerNetClient __instance, int clientId)
    {
        List<NetworkedPlayerInfo> players = GameData.Instance.AllPlayers.ToArray().ToList();

        // We send 5 players at a time to prevent too huge packet
        while (players.Count > 0)
        {
            var batch = players.Take(5).ToList();

            MessageWriter messageWriter = MessageWriter.Get(SendOption.Reliable);
            messageWriter.StartMessage(6);
            messageWriter.Write(__instance.GameId);
            messageWriter.WritePacked(clientId);

            foreach (var player in batch)
            {
                if (messageWriter.Length > 500) break;
                if (player != null && player.ClientId != clientId && !player.Disconnected)
                {
                    __instance.WriteSpawnMessage(player, player.OwnerId, player.SpawnFlags, messageWriter);
                }
                players.Remove(player);
            }
            messageWriter.EndMessage();
            // Logger.Info($"send delayed network data to {clientId} , size is {messageWriter.Length}", "SendInitialDataPrefix");
            __instance.SendOrDisconnect(messageWriter);
            messageWriter.Recycle();
        }
    }

    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.SendAllStreamedObjects))]
    [HarmonyPrefix]
    public static bool SendAllStreamedObjectsPrefix(InnerNetClient __instance, ref bool __result)
    {
        if (!Constants.IsVersionModded() || __instance.NetworkMode != NetworkModes.OnlineGame) return true;
        // Bypass all NetworkedData here.
        __result = false;
        Il2CppSystem.Collections.Generic.List<InnerNetObject> obj = __instance.allObjects;
        lock (obj)
        {
            for (int i = 0; i < __instance.allObjects.Count; i++)
            {
                InnerNetObject innerNetObject = __instance.allObjects[i];
                if (innerNetObject && innerNetObject is not NetworkedPlayerInfo && innerNetObject.IsDirty && (innerNetObject.AmOwner || (innerNetObject.OwnerId == -2 && __instance.AmHost)))
                {
                    MessageWriter messageWriter = __instance.Streams[(int)innerNetObject.sendMode];
                    messageWriter.StartMessage(1);
                    messageWriter.WritePacked(innerNetObject.NetId);
                    try
                    {
                        if (innerNetObject.Serialize(messageWriter, false))
                        {
                            messageWriter.EndMessage();
                        }
                        else
                        {
                            messageWriter.CancelMessage();
                        }
                        if (innerNetObject.Chunked && innerNetObject.IsDirty)
                        {
                            __result = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Exception(ex, "SendAllStreamedObjectsPrefix");
                        messageWriter.CancelMessage();
                    }
                }
            }
        }
        for (int j = 0; j < __instance.Streams.Length; j++)
        {
            MessageWriter messageWriter2 = __instance.Streams[j];
            if (messageWriter2.HasBytes(7))
            {
                messageWriter2.EndMessage();
                __instance.SendOrDisconnect(messageWriter2);
                messageWriter2.Clear((SendOption)j);
                messageWriter2.StartMessage(5);
                messageWriter2.Write(__instance.GameId);
            }
        }
        return false;
    }
    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.Spawn))]
    [HarmonyPostfix]
    public static void Spawn_Postfix(InnerNetClient __instance, InnerNetObject netObjParent, int ownerId = -2, SpawnFlags flags = SpawnFlags.None)
    {
        if (!Constants.IsVersionModded() || __instance.NetworkMode != NetworkModes.OnlineGame) return;

        if (__instance.AmHost)
        {
            if (netObjParent is NetworkedPlayerInfo playerinfo)
            {
                _ = new LateTask(() =>
                {
                    if (playerinfo != null && AmongUsClient.Instance.AmConnected)
                    {
                        var client = AmongUsClient.Instance.GetClient(playerinfo.ClientId);
                        if (client != null && !client.IsDisconnected())
                        {
                            if (playerinfo.IsIncomplete)
                            {
                                Logger.Info($"Disconnecting Client [{client.Id}]{client.PlayerName} {client.FriendCode} for playerinfo timeout", "DelayedNetworkedData");
                                AmongUsClient.Instance.SendLateRejection(client.Id, DisconnectReasons.ClientTimeout);
                                __instance.OnPlayerLeft(client, DisconnectReasons.ClientTimeout);
                            }
                        }
                    }
                }, 5f, "PlayerInfo Green Bean Kick", false);
            }

            if (netObjParent is PlayerControl player)
            {
                _ = new LateTask(() =>
                {
                    if (player != null && !player.notRealPlayer && !player.isDummy && AmongUsClient.Instance.AmConnected)
                    {
                        var client = AmongUsClient.Instance.GetClient(player.OwnerId);
                        if (client != null && !client.IsDisconnected())
                        {
                            if (player.Data == null || player.Data.IsIncomplete)
                            {
                                Logger.Info($"Disconnecting Client [{client.Id}]{client.PlayerName} {client.FriendCode} for playercontrol timeout", "DelayedNetworkedData");
                                AmongUsClient.Instance.SendLateRejection(client.Id, DisconnectReasons.ClientTimeout);
                                __instance.OnPlayerLeft(client, DisconnectReasons.ClientTimeout);
                            }
                        }
                    }
                }, 5.5f, "PlayerControl Green Bean Kick", false);
            }
        }

        if (!__instance.AmHost)
        {
            Debug.LogError("Tried to spawn while not host:" + (netObjParent?.ToString()));
        }
        return;
    }


    private static byte timer = 0;
    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.FixedUpdate))]
    [HarmonyPostfix]
    public static void FixedUpdatePostfix(InnerNetClient __instance)
    {
        // Send a networked data pre 2 fixed update should be a good practice?
        if (!Constants.IsVersionModded() || __instance.NetworkMode != NetworkModes.OnlineGame) return;
        if (!__instance.AmHost || __instance.Streams == null) return;

        if (timer == 0)
        {
            timer = 1;
            return;
        }

        var player = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(x => x.IsDirty);
        if (player != null)
        {
            timer = 0;
            MessageWriter messageWriter = MessageWriter.Get(SendOption.Reliable);
            messageWriter.StartMessage(5);
            messageWriter.Write(__instance.GameId);
            messageWriter.StartMessage(1);
            messageWriter.WritePacked(player.NetId);
            try
            {
                if (player.Serialize(messageWriter, false))
                {
                    messageWriter.EndMessage();
                }
                else
                {
                    messageWriter.Recycle();
                    player.ClearDirtyBits();
                    return;
                }
                messageWriter.EndMessage();
                __instance.SendOrDisconnect(messageWriter);
                messageWriter.Recycle();
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "FixedUpdatePostfix");
                messageWriter.CancelMessage();
                player.ClearDirtyBits();
            }
        }
    }
    [Obfuscation(Exclude = true)]
    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.SendOrDisconnect)), HarmonyPrefix]
    public static void SendOrDisconnectPatch(InnerNetClient __instance, MessageWriter msg)
    {
        if (DebugModeManager.IsDebugMode)
        {
            Logger.Info($"Packet({msg.Length}), SendOption:{msg.SendOption}", "SendOrDisconnectPatch");
        }
        else if (msg.Length > 1000)
        {
            Logger.Info($"Large Packet({msg.Length})", "SendOrDisconnectPatch");
        }
    }
}
[HarmonyPatch(typeof(GameData), nameof(GameData.DirtyAllData))]
internal class DirtyAllDataPatch
{
    // Currently this function only occurs in CreatePlayer
    // It's believed to lag host, delay the playercontrol spawn mesasge & blackout new client
    // & send huge packets to all clients & completely no need to run
    // Temporarily disable it until Innersloth get a better fix.
    public static bool Prefix()
    {
        return false;
    }
}

[HarmonyPatch(typeof(NetworkedPlayerInfo), nameof(NetworkedPlayerInfo.Serialize))]
internal class NetworkedPlayerInfoSerializePatch
{
    public static bool Prefix(NetworkedPlayerInfo __instance, MessageWriter writer, bool initialState, ref bool __result)
    {
        writer.Write(__instance.PlayerId);
        writer.WritePacked(__instance.ClientId);
        writer.Write((byte)__instance.Outfits.Count);
        foreach (Il2CppSystem.Collections.Generic.KeyValuePair<PlayerOutfitType, NetworkedPlayerInfo.PlayerOutfit> keyValuePair in __instance.Outfits)
        {
            writer.Write((byte)keyValuePair.Key);

            if (initialState)
            {
                var oldOutfit = keyValuePair.Value;
                NetworkedPlayerInfo.PlayerOutfit playerOutfit = new();
                Main.AllClientRealNames.TryGetValue(__instance.ClientId, out var name);
                playerOutfit.Set(name ?? " ", oldOutfit.ColorId, oldOutfit.HatId, oldOutfit.SkinId, oldOutfit.VisorId, oldOutfit.PetId, oldOutfit.NamePlateId);
                playerOutfit.Serialize(writer);
            }
            else
            {
                keyValuePair.Value.Serialize(writer);
            }
        }
        writer.WritePacked(__instance.PlayerLevel);
        byte b = 0;
        if (__instance.Disconnected)
        {
            b |= 1;
        }
        if (__instance.IsDead)
        {
            b |= 4;
        }
        writer.Write(b);
        writer.Write((ushort)__instance.Role.Role);

        writer.Write(false); // for some very cool reason you cant get_RoleWhenAlive. It throws an exception.
        // Serializing false here does not influence game play

        if (__instance.Tasks != null)
        {
            writer.Write((byte)__instance.Tasks.Count);
            for (int i = 0; i < __instance.Tasks.Count; i++)
            {
                __instance.Tasks[i].Serialize(writer);
            }
        }
        else
        {
            writer.Write(0);
        }
        writer.Write(__instance.FriendCode ?? string.Empty);

        if (GameStates.IsVanillaServer) // we can serialize empty puid safely on custom servers
        {
            writer.Write(__instance.Puid ?? string.Empty);
        }
        else
        {
            writer.Write(string.Empty);
        }

        if (!initialState)
        {
            __instance.ClearDirtyBits();
        }
        __result = true;
        return false;
    }
}
