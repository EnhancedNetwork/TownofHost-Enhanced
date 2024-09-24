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
        if (!Constants.IsVersionModded() || GameStates.IsInGame || __instance.NetworkMode != NetworkModes.OnlineGame) return true;
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

    // InnerSloth vanilla officials send PlayerInfo in spilt reliable packets
    private static void DelaySpawnPlayerInfo(InnerNetClient __instance, int clientId)
    {
        List<NetworkedPlayerInfo> players = GameData.Instance.AllPlayers.ToArray().ToList();

        foreach (var player in players)
        {
            if (player != null && player.ClientId != clientId && !player.Disconnected)
            {
                MessageWriter messageWriter = MessageWriter.Get(SendOption.Reliable);
                messageWriter.StartMessage(6);
                messageWriter.Write(__instance.GameId);
                messageWriter.WritePacked(clientId);

                __instance.WriteSpawnMessage(player, player.OwnerId, player.SpawnFlags, messageWriter);
                messageWriter.EndMessage();

                __instance.SendOrDisconnect(messageWriter);
                messageWriter.Recycle();
            }
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

    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.FixedUpdate))]
    [HarmonyPostfix]
    public static void FixedUpdatePostfix(InnerNetClient __instance)
    {
        // Just send with None calls. Who cares?
        if (!Constants.IsVersionModded() || GameStates.IsInGame || __instance.NetworkMode != NetworkModes.OnlineGame) return;
        if (!__instance.AmHost || __instance.Streams == null) return;

        var players = GameData.Instance.AllPlayers.ToArray().Where(x => x.IsDirty).ToList();
        if (players != null)
        {
            foreach (var player in players)
            {
                MessageWriter messageWriter = MessageWriter.Get(SendOption.None);
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
                        messageWriter.CancelMessage();
                        player.ClearDirtyBits();
                        continue;
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
                    continue;
                }
            }
        }
    }
}

// Seems like there is no need to patch this if we are always sending with None calls
/*
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
*/
