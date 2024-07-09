using Hazel;
using InnerNet;
using UnityEngine;

namespace TOHE.Modules.DelayNetworkDataSpawn;

[HarmonyPatch(typeof(InnerNetClient))]
public class InnerNetClientPatch
{
    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.SendInitialData))]
    [HarmonyPrefix]
    public static bool SendInitialDataPrefix(InnerNetClient __instance, int clientId)
    {
        if (!Constants.IsVersionModded()) return true;
        // We make sure other stuffs like playercontrol and Lobby behavior is spawned properly
        // Then we spawn networked data for new clients
        MessageWriter messageWriter = MessageWriter.Get(SendOption.Reliable);
        messageWriter.StartMessage(6);
        messageWriter.Write(__instance.GameId);
        messageWriter.WritePacked(clientId);
        Il2CppSystem.Collections.Generic.List<InnerNetObject> obj = __instance.allObjects;
        lock (obj)
        {
            HashSet<GameObject> hashSet = new HashSet<GameObject>();
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

    public static void DelaySpawnPlayerInfo(InnerNetClient __instance, int clientId)
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
                if (player !=  null && player.ClientId != clientId && !player.Disconnected)
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
