using Hazel;
using InnerNet;
using TOHE.Modules;

namespace TOHE.Patches;

[Obfuscation(Exclude = true)]
public enum GameDataTag : byte
{
    DataFlag = 1,
    RpcFlag = 2,
    SpawnFlag = 4,
    DespawnFlag = 5,
    SceneChangeFlag = 6,
    ReadyFlag = 7,
    ChangeSettingsFlag = 8,
    ConsoleDeclareClientPlatformFlag = 205,
    PS4RoomRequest = 206,
    XboxDeclareXuid = 207,
}

[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.HandleGameDataInner))]
internal class GameDataHandlerPatch
{
    public static bool Prefix(InnerNetClient __instance, MessageReader reader, int msgNum)
    {
        MessageReader subReader = MessageReader.Get(reader);
        var tag = (GameDataTag)reader.Tag;

        switch (tag)
        {
            case GameDataTag.DataFlag:
                {
                    var netId = reader.ReadPackedUInt32();
                    if (__instance.allObjects.allObjectsFast.TryGetValue(netId, out var obj))
                    {
                        if (obj.AmOwner)
                        {
                            Logger.Warn(string.Format("Received DataFlag for object {0} {1} that we own.", netId.ToString(), obj.name), "GameDataHandlerPatch");
                            EAC.WarnHost();
                            return false;
                        }
                        else
                        {
                            if (AmongUsClient.Instance.AmHost)
                            {
                                if (obj == MeetingHud.Instance)
                                {
                                    Logger.Warn(string.Format("Received DataFlag for MeetingHud {0} that we own.", netId.ToString()), "GameDataHandlerPatch");
                                    EAC.WarnHost();
                                    return false;
                                }

                                if (obj == VoteBanSystem.Instance)
                                {
                                    Logger.Warn(string.Format("Received DataFlag for VoteBanSystem {0} that we own.", netId.ToString()), "GameDataHandlerPatch");
                                    EAC.WarnHost();
                                    return false;
                                }

                                if (obj is NetworkedPlayerInfo)
                                {
                                    Logger.Warn(string.Format("Received DataFlag for NetworkedPlayerInfo {0} that we own.", netId.ToString()), "GameDataHandlerPatch");
                                    EAC.WarnHost();
                                    return false;
                                }
                            }
                        }
                    }

                    break;
                }

            case GameDataTag.RpcFlag:
                break;

            case GameDataTag.SpawnFlag:
                break;

            case GameDataTag.DespawnFlag:
                break;

            case GameDataTag.SceneChangeFlag:
                {
                    // Sender is only allowed to change his own scene.
                    var clientId = reader.ReadPackedInt32();
                    var scene = reader.ReadString();

                    var client = Utils.GetClientById(clientId);

                    if (client == null)
                    {
                        Logger.Warn($"Received SceneChangeFlag for unknown client {clientId}.", "GameDataHandlerPatch");
                        return false;
                    }

                    if (scene == string.Empty || scene == null)
                    {
                        Logger.Warn(string.Format("Client {0} ({1}) tried to send SceneChangeFlag with null scene.", client.PlayerName, client.Id), "GameDataHandlerPatch");
                        EAC.WarnHost();
                        return false;
                    }

                    if (scene.ToLower() == "tutorial")
                    {
                        Logger.Warn(string.Format("Client {0} ({1}) tried to send SceneChangeFlag to Tutorial.", client.PlayerName, client.Id), "GameDataHandlerPatch");
                        EAC.WarnHost(100);

                        if (GameStates.IsOnlineGame && AmongUsClient.Instance.AmHost && GameStates.IsShip && !GameStates.IsLobby)
                        {
                            CriticalErrorManager.SetCriticalError("SceneChange Tutorial Hack", false);
                            CriticalErrorManager.CheckEndGame();
                        }
                        return false;
                    }

                    if (GameStates.IsInGame)
                    {
                        Logger.Warn(string.Format("Client {0} ({1}) tried to send SceneChangeFlag during mid of game.", client.PlayerName, client.Id), "GameDataHandlerPatch");
                        return false;
                    }

                    break;
                }

            case GameDataTag.ReadyFlag:
                {
                    var clientId = reader.ReadPackedInt32();
                    var client = Utils.GetClientById(clientId);

                    if (client == null)
                    {
                        Logger.Warn($"Received ReadyFlag for unknown client {clientId}.", "GameDataHandlerPatch");
                        EAC.WarnHost();
                        return false;
                    }

                    if (AmongUsClient.Instance.AmHost)
                    {
                        if (!StartGameHostPatch.isStartingAsHost)
                        {
                            Logger.Warn($"Received ReadyFlag while game is started from {clientId}.", "GameDataHandlerPatch");
                            EAC.WarnHost();
                            return false;
                        }
                    }

                    break;
                }

            case GameDataTag.ConsoleDeclareClientPlatformFlag:
                break;
        }

        return true;
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGameHost))]
internal class StartGameHostPatch
{
    public static bool isStartingAsHost = false;
    public static void Prefix(AmongUsClient __instance)
    {
        if (LobbyBehaviour.Instance != null)
            isStartingAsHost = true;
    }
    public static void Postfix(AmongUsClient __instance)
    {
        Logger.Info("StartGameHostPatch: Postfix called", "StartGameHostPatch");
        if (ShipStatus.Instance != null)
            isStartingAsHost = false;

        GameStates.InGame = true;
    }
}

[HarmonyPatch]
internal class AuthTimeoutPatch
{
    [HarmonyPatch(typeof(AuthManager._CoConnect_d__4), nameof(AuthManager._CoConnect_d__4.MoveNext))]
    [HarmonyPatch(typeof(AuthManager._CoWaitForNonce_d__6), nameof(AuthManager._CoWaitForNonce_d__6.MoveNext))]
    [HarmonyPrefix]
    // From Reactor.gg
    // https://github.com/NuclearPowered/Reactor/blob/master/Reactor/Patches/Miscellaneous/CustomServersPatch.cs
    public static bool CoWaitforNoncePrefix(ref bool __result)
    {
        if (GameStates.IsVanillaServer)
        {
            return true;
        }
        __result = false;
        return false;
    }

    // If you dont patch this, u still need to wait for 5s
    // I have no idea why this is happening
    [HarmonyPatch(typeof(AmongUsClient._CoJoinOnlinePublicGame_d__49), nameof(AmongUsClient._CoJoinOnlinePublicGame_d__49.MoveNext))]
    [HarmonyPrefix]
    public static void EnableUdpMatchmakingPrefix(AmongUsClient._CoJoinOnlinePublicGame_d__49 __instance)
    {
        // Skip to state 1 which just calls CoJoinOnlineGameDirect
        if (__instance.__1__state == 0 && !ServerManager.Instance.IsHttp)
        {
            __instance.__1__state = 1;
            __instance.__8__1 = new AmongUsClient.__c__DisplayClass49_0
            {
                matchmakerToken = string.Empty,
            };
        }
    }
}

[HarmonyPatch(typeof(NetworkedPlayerInfo), nameof(NetworkedPlayerInfo.UpdateName))]
public class NetworkedPlayerInfoPatch
{
    // Prevent mark dirty here
    public static bool Prefix(NetworkedPlayerInfo __instance, string playerName, ClientData client)
    {
        __instance.PlayerName = playerName;
        client.UpdatePlayerName(playerName);
        return false;
    }
}
