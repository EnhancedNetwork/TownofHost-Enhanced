using AmongUs.Data;
using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using System;
using System.Text.RegularExpressions;
using AmongUs.InnerNet.GameDataMessages;
using TOHE.Modules;
using TOHE.Patches;
using TOHE.Roles.Core.AssignManager;
using TOHE.Roles.Crewmate;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
class OnGameJoinedPatch
{
    public static void Postfix(AmongUsClient __instance)
    {
        while (!Options.IsLoaded) System.Threading.Tasks.Task.Delay(1);
        Logger.Info($"{__instance.GameId} Joining room - Room code: {GameCode.IntToGameName(AmongUsClient.Instance.GameId) ?? string.Empty}", "OnGameJoined");

        Main.IsHostVersionCheating = false;
        Main.playerVersion = [];
        SoundManager.Instance.ChangeAmbienceVolume(DataManager.Settings.Audio.AmbienceVolume);

        Main.HostClientId = AmongUsClient.Instance.HostId;
        if (!DebugModeManager.AmDebugger && Main.VersionCheat.Value)
            Main.VersionCheat.Value = false;

        RpcUtils.queuedReliableMessage.Clear();
        RpcUtils.queuedUnreliableMessage.Clear();
        
        ChatUpdatePatch.DoBlockChat = false;
        Main.CurrentServerIsVanilla = GameStates.IsVanillaServer && !GameStates.IsLocalGame;
        GameStates.InGame = false;
        ErrorText.Instance.Clear();
        EAC.Init();
        Main.AllClientRealNames.Clear();

        if (AmongUsClient.Instance.AmHost) // Execute the following only on the Host
        {
            EndGameManagerPatch.IsRestarting = false;
            if (!RehostManager.IsAutoRehostDone)
            {
                AmongUsClient.Instance.ChangeGamePublic(RehostManager.ShouldPublic);
                RehostManager.IsAutoRehostDone = true;
            }

            Main.HostRealName = DataManager.Player.Customization.Name;
            if (!Main.AllClientRealNames.ContainsKey(__instance.ClientId))
            {
                Main.AllClientRealNames.Add(__instance.ClientId, DataManager.Player.Customization.Name);
            }

            GameStartManagerPatch.GameStartManagerUpdatePatch.exitTimer = -1;
            Main.DoBlockNameChange = false;
            RoleAssign.SetRoles = [];
            GhostRoleAssign.forceRole = [];
            EAC.DeNum = new();
            Main.AllPlayerNames.Clear();
            Main.PlayerQuitTimes.Clear();
            KickPlayerPatch.AttemptedKickPlayerList = [];

            switch (GameOptionsManager.Instance.CurrentGameOptions.GameMode)
            {
                case GameModes.NormalFools:
                case GameModes.Normal:
                    Logger.Info(" Is Normal Game", "Game Mode");

                    if (Main.NormalOptions.KillCooldown == 0f)
                        Main.NormalOptions.KillCooldown = Main.LastKillCooldown.Value;

                    AURoleOptions.SetOpt(Main.NormalOptions.Cast<IGameOptions>());

                    if (AURoleOptions.ShapeshifterCooldown == 0f)
                        AURoleOptions.ShapeshifterCooldown = Main.LastShapeshifterCooldown.Value;

                    if (AURoleOptions.GuardianAngelCooldown == 0f)
                        AURoleOptions.GuardianAngelCooldown = Main.LastGuardianAngelCooldown.Value;

                    // If custom Gamemode is HideNSeekTOHO in normal game, set Standard
                    if (Options.CurrentGameMode == CustomGameMode.HidenSeekTOHO)
                    {
                        // Select standard
                        Options.GameMode.SetValue(0);
                    }
                    break;

                case GameModes.SeekFools:
                case GameModes.HideNSeek:
                    Logger.Info(" Is Hide & Seek", "Game Mode");

                    // If custom Gamemode is Standard/FFA/C&R in H&S game, set HideNSeekTOHO
                    if (Options.CurrentGameMode is CustomGameMode.Standard or CustomGameMode.FFA or CustomGameMode.CandR or CustomGameMode.UltimateTeam or CustomGameMode.TrickorTreat)
                    {
                        // Select HideNSeekTOHO
                        Options.GameMode.SetValue(2);
                    }
                    break;

                case GameModes.None:
                    Logger.Info(" Is None", "Game Mode");
                    break;

                default:
                    Logger.Info(" No found", "Game Mode");
                    break;
            }
        }

        _ = new LateTask(() =>
        {
            try
            {
                if (!GameStates.IsOnlineGame) return;
                if (!GameStates.IsModHost)
                    RPC.RpcRequestRetryVersionCheck();
                if (BanManager.CheckEACList(EOSManager.Instance.FriendCode, BanManager.GetHashedPuid(EOSManager.Instance.ProductUserId)) && GameStates.IsOnlineGame)
                {
                    AmongUsClient.Instance.ExitGame(DisconnectReasons.Banned);
                    SceneChanger.ChangeScene("MainMenu");
                    return;
                }


                var client = AmongUsClient.Instance.GetClientFromCharacter(PlayerControl.LocalPlayer);
                var host = AmongUsClient.Instance.GetHost();

                if (!GameStates.IsVanillaServer)
                {
                    RPC.RpcSetFriendCode(EOSManager.Instance.FriendCode);
                }

                Logger.Info($"{client.PlayerName.RemoveHtmlTags()}(ClientID:{client.Id}/FriendCode:{client.FriendCode}/HashPuid:{client.GetHashedPuid()}/Platform:{client.PlatformData.Platform}) finished join room", "Session: OnGameJoined");
                Logger.Info($"{host.PlayerName.RemoveHtmlTags()}(ClientID:{host.Id}/FriendCode:{host.FriendCode}/HashPuid:{host.GetHashedPuid()}/Platform:{host.PlatformData.Platform}) is the host", "Session: OnGameJoined");
            }
            catch
            {
                Logger.Error("Error while trying to log local client data.", "OnGameJoinedPatch");
                _ = new LateTask(() =>
                {
                    try
                    {
                        if (!GameStates.IsOnlineGame && !GameStates.IsLocalGame) return;
                        var client = AmongUsClient.Instance.GetClientFromCharacter(PlayerControl.LocalPlayer);
                        var host = AmongUsClient.Instance.GetHost();
                        Logger.Info($"{client.PlayerName.RemoveHtmlTags()}(ClientID:{client.Id}/FriendCode:{client.FriendCode}/HashPuid:{client.GetHashedPuid()}/Platform:{client.PlatformData.Platform}) finished join room", "Session: OnGameJoined Retry");
                        Logger.Info($"{host.PlayerName.RemoveHtmlTags()}(ClientID:{host.Id}/FriendCode:{host.FriendCode}/HashPuid:{host.GetHashedPuid()}/Platform:{host.PlatformData.Platform}) is the host", "Session: OnGameJoined Retry");
                    }
                    catch { }
                    ;
                }, 1.5f, "Retry Log Local Client");
            }
        }, 0.7f, "OnGameJoinedPatch");
    }
}
[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.DisconnectInternal))]
class DisconnectInternalPatch
{
    public static void Prefix(InnerNetClient __instance, DisconnectReasons reason, string stringReason)
    {
        GameStates.InGame = false;
        Logger.Info($"Disconnect (Reason:{reason}:{stringReason}, ping:{__instance.Ping})", "Reason Disconnect");
        RehostManager.OnDisconnectInternal(reason);
    }
}
[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
public static class OnPlayerJoinedPatch
{
    public static bool IsDisconnected(this ClientData client)
    {
        var __instance = AmongUsClient.Instance;
        for (int i = 0; i < __instance.allClients.Count; i++)
        {
            ClientData clientData = __instance.allClients[i];
            if (clientData.Id == client.Id)
            {
                return true;
            }
        }
        return false;
        //When a client disconnects, it is removed from allClients in method amongusclient.removeplayer
    }
    public static bool HasInvalidFriendCode(string friendcode)
    {
        if (string.IsNullOrEmpty(friendcode))
        {
            return true;
        }

        if (friendcode.Length < 7) // #1234 is 5 chars, and its impossible for a friend code to only have 3
        {
            return true;
        }

        if (friendcode.Count(c => c == '#') != 1)
        {
            return true;
        }

        string pattern = @"[\W\d]";
        if (Regex.IsMatch(friendcode[..friendcode.IndexOf("#")], pattern))
        {
            return true;
        }

        return false;
    }

    private static bool IsSuspiciousPlayer(ClientData client)
    {

        // Check for known attacker patterns in player name
        if (client.PlayerName.Contains("SEEKER"))
            return true;

        // Check for suspicious platform data
        if (client.PlatformData.Platform == Platforms.Unknown)
            return true;

        return false;
    }
    public static void Postfix(/*AmongUsClient __instance,*/ [HarmonyArgument(0)] ClientData client)
    {
        Logger.Info($"{client.PlayerName}(ClientID:{client.Id}/FriendCode:{client.FriendCode}/HashPuid:{client.GetHashedPuid()}/Platform:{client.PlatformData.Platform}) Joining room", "Session: OnPlayerJoined");

        Main.AssignRolesIsStarted = false;

        _ = new LateTask(() =>
        {
            try
            {
                // Check for suspicious players
                if (IsSuspiciousPlayer(client))
                {
                    AmongUsClient.Instance.KickPlayer(client.Id, true);
                    Logger.Warn($"Kicked suspicious player: {client.PlayerName}", "Anti-Hack");
                    return;
                }

                if (AmongUsClient.Instance.AmHost && !client.IsDisconnected() && client.Character.Data.IsIncomplete)
                {
                    Logger.SendInGame(GetString("Error.InvalidColor") + $" {client.Id}/{client.PlayerName}");
                    AmongUsClient.Instance.KickPlayer(client.Id, false);
                    Logger.Info($"Kicked client {client.Id}/{client.PlayerName} bcz PlayerControl is not spawned in time.", "OnPlayerJoinedPatchPostfix");
                    return;
                }

                if (AmongUsClient.Instance.AmHost && !Main.playerVersion.TryGetValue(client.Id, out _))
                {
                    var retry = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RequestRetryVersionCheck, SendOption.None, client.Id);
                    AmongUsClient.Instance.FinishRpcImmediately(retry);
                }
            }
            catch { }
        }, 4.5f, "green bean kick late task", false);


        if (AmongUsClient.Instance.AmHost && HasInvalidFriendCode(client.FriendCode) && Options.KickPlayerFriendCodeInvalid.GetBool() && !GameStates.IsLocalGame)
        {
            if (!Options.TempBanPlayerFriendCodeInvalid.GetBool())
            {
                AmongUsClient.Instance.KickPlayer(client.Id, false);
                Logger.SendInGame(string.Format(GetString("Message.KickedByInvalidFriendCode"), client.PlayerName));
                Logger.Info($"Kicked a player {client?.PlayerName} because of invalid friend code", "Kick");
            }
            else
            {
                if (!BanManager.TempBanWhiteList.Contains(client.GetHashedPuid()))
                    BanManager.TempBanWhiteList.Add(client.GetHashedPuid());
                AmongUsClient.Instance.KickPlayer(client.Id, true);
                Logger.SendInGame(string.Format(GetString("Message.TempBannedByInvalidFriendCode"), client.PlayerName));
                Logger.Info($"TempBanned a player {client?.PlayerName} because of invalid friend code", "Temp Ban");
            }
        }

        if (AmongUsClient.Instance.AmHost && Options.AllowOnlyWhiteList.GetBool() && !BanManager.CheckAllowList(client?.FriendCode) && !GameStates.IsLocalGame)
        {
            AmongUsClient.Instance.KickPlayer(client.Id, false);
            Logger.SendInGame(string.Format(GetString("Message.KickedByWhiteList"), client.PlayerName));
            Logger.Warn($"Kicked player {client?.PlayerName}, because friendcode: {client?.FriendCode} is not in WhiteList.txt", "Kick");
        }

        Platforms platform = client.PlatformData.Platform;
        if (AmongUsClient.Instance.AmHost && Options.KickOtherPlatformPlayer.GetBool() && platform != Platforms.Unknown && !GameStates.IsLocalGame)
        {
            if ((platform == Platforms.Android && Options.OptKickAndroidPlayer.GetBool()) ||
                (platform == Platforms.IPhone && Options.OptKickIphonePlayer.GetBool()) ||
                (platform == Platforms.Xbox && Options.OptKickXboxPlayer.GetBool()) ||
                (platform == Platforms.Playstation && Options.OptKickPlayStationPlayer.GetBool()) ||
                (platform == Platforms.Switch && Options.OptKickNintendoPlayer.GetBool()))
            {
                string msg = string.Format(GetString("MsgKickOtherPlatformPlayer"), client?.PlayerName, platform.ToString());
                AmongUsClient.Instance.KickPlayer(client.Id, false);
                Logger.SendInGame(msg);
                Logger.Info(msg, "Other Platform Kick"); ;
            }
        }
        if (DestroyableSingleton<FriendsListManager>.Instance.IsPlayerBlockedUsername(client.FriendCode) && AmongUsClient.Instance.AmHost)
        {
            AmongUsClient.Instance.KickPlayer(client.Id, true);
            Logger.Info($"Ban Player ー {client?.PlayerName}({client.FriendCode}) has been banned.", "BAN");
        }
        BanManager.CheckBanPlayer(client);

        if (AmongUsClient.Instance.AmHost)
        {
            if (Main.SayStartTimes.ContainsKey(client.Id)) Main.SayStartTimes.Remove(client.Id);
            if (Main.SayBanwordsTimes.ContainsKey(client.Id)) Main.SayBanwordsTimes.Remove(client.Id);

            if (client.GetHashedPuid() != "" && Options.TempBanPlayersWhoKeepQuitting.GetBool()
                && !BanManager.CheckAllowList(client.FriendCode) && !GameStates.IsLocalGame)
            {
                if (Main.PlayerQuitTimes.TryGetValue(client.GetHashedPuid(), out var quitTimes))
                {
                    if (quitTimes >= Options.QuitTimesTillTempBan.GetInt())
                    {
                        if (!BanManager.TempBanWhiteList.Contains(client.GetHashedPuid()))
                            BanManager.TempBanWhiteList.Add(client.GetHashedPuid());
                        AmongUsClient.Instance.KickPlayer(client.Id, true);
                        Logger.SendInGame(string.Format(GetString("Message.TempBannedForSpamQuitting"), client.PlayerName));
                        Logger.Info($"Temp Ban Player ー {client?.PlayerName}({client.FriendCode}) has been temp banned.", "BAN");
                    }
                }
            }
        }
    }
}
[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
class OnPlayerLeftPatch
{
    public static bool StartingProcessing = false;
    public static byte LeftPlayerId = byte.MaxValue;
    static void Prefix([HarmonyArgument(0)] ClientData data)
    {
        StartingProcessing = true;
        LeftPlayerId = data?.Character?.PlayerId ?? byte.MaxValue;

        if (data != null && data.Character != null)
            StartGameHostPatch.DataDisconnected[data.Character.PlayerId] = true;

        if (GameStates.IsInGame)
        {
            Main.PlayerStates[data.Character.PlayerId].Disconnected = true;
        }

        if (!AmongUsClient.Instance.AmHost) return;

        if (Main.AssignRolesIsStarted)
        {
            Logger.Warn($"Assign roles not ended, try remove player {data.Character.PlayerId} from role assign", "OnPlayerLeft");
            RoleAssign.RoleResult?.Remove(data.Character.PlayerId);
            RpcSetRoleReplacer.Senders?.Remove(data.Character.OwnerId);
        }

        if (GameStates.IsNormalGame && GameStates.IsInGame)
        {
            if (Options.CurrentGameMode is CustomGameMode.CandR)
            {
                CopsAndRobbersManager.OnPlayerDisconnect(data.Character.PlayerId);
            }
            if (data.Character != null) CustomNetObject.DespawnOnQuit(data.Character.PlayerId);
            MurderPlayerPatch.AfterPlayerDeathTasks(data?.Character, data?.Character, GameStates.IsMeeting);
        }

        if (AmongUsClient.Instance.AmHost && data.Character != null)
        {
            // Remove messages sending to left Player
            for (int i = 0; i < Main.MessagesToSend.Count; i++)
            {
                var (msg, sendTo, title) = Main.MessagesToSend[i];
                if (sendTo == data.Character.PlayerId)
                {
                    Main.MessagesToSend.RemoveAt(i);
                    i--;
                }
            }

            // This latetask is to make sure that the Player control is completely despawned for everyone so nobody gonna disconnect itself
            var netid = data.Character.NetId;
            _ = new LateTask(() =>
            {
                if (GameStates.IsOnlineGame && AmongUsClient.Instance.AmHost)
                {
                    var message = new DespawnGameDataMessage(netid);
                    RpcUtils.LateBroadcastReliableMessage(message);
                }
            }, 2.5f, "Repeat Despawn", false);
        }
    }
    public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData data, [HarmonyArgument(1)] DisconnectReasons reason)
    {
        try
        {
            if (GameStates.IsNormalGame && GameStates.IsInGame)
            {
                if (data.Character.Is(CustomRoles.Lovers) && !data.Character.Data.IsDead)
                {
                    foreach (var lovers in Main.LoversPlayers.ToArray())
                    {
                        Main.isLoversDead = true;
                        Main.LoversPlayers.Remove(lovers);
                        Main.PlayerStates[lovers.PlayerId].RemoveSubRole(CustomRoles.Lovers);
                    }
                }

                Spiritualist.RemoveTarget(data.Character.PlayerId);

                var state = Main.PlayerStates[data.Character.PlayerId];
                state.Disconnected = true;
                state.SetDead();

                // If the Player left while he had a Notice message, clear it
                if (NameNotifyManager.Notifying(data.Character))
                {
                    NameNotifyManager.Notice.Remove(data.Character.PlayerId);
                }

                if (AmongUsClient.Instance.AmHost)
                {
                    try
                    {
                        data.Character.RpcSetName(state.NormalOutfit.PlayerName);
                    }
                    catch
                    { }
                }

                AntiBlackout.OnDisconnect(data.Character.Data);
                PlayerGameOptionsSender.RemoveSender(data.Character);
            }

            if (Main.HostClientId != data.Id && Main.playerVersion.ContainsKey(data.Id))
                Main.playerVersion.Remove(data.Id);

            if (Main.HostClientId == data.Id && Main.playerVersion.ContainsKey(data.Id))
            {
                var clientId = -1;
                var player = PlayerControl.LocalPlayer;
                var title = "<color=#aaaaff>" + GetString("DefaultSystemMessageTitle") + "</color>";
                var name = player?.Data?.PlayerName;
                var msg = "";
                if (GameStates.IsInGame)
                {
                    CriticalErrorManager.SetCriticalError("Host exits the game", false);
                    CriticalErrorManager.CheckEndGame();
                    msg = GetString("Message.HostLeftGameInGame");
                }
                else if (GameStates.IsLobby)
                    msg = GetString("Message.HostLeftGameInLobby");

                player.SetName(title);
                DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg);
                player.SetName(name);

                //On become Host is called before OnPlayerLeft, so this is safe to use
                if (AmongUsClient.Instance.AmHost)
                {
                    var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
                    writer.StartMessage(clientId);
                    writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
                        .Write(player.Data.NetId)
                        .Write(title)
                        .EndRpc();
                    writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
                        .Write(msg)
                        .EndRpc();
                    writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
                        .Write(player.Data.NetId)
                        .Write(player.Data.PlayerName)
                        .EndRpc();
                    writer.EndMessage();
                    writer.SendMessage();
                }
                Main.HostClientId = AmongUsClient.Instance.HostId;
                _ = new LateTask(() =>
                {
                    if (!GameStates.IsOnlineGame) return;
                    if (Main.playerVersion.ContainsKey(AmongUsClient.Instance.HostId))
                    {
                        if (AmongUsClient.Instance.AmHost)
                        {
                            Utils.SendMessage(string.Format(GetString("Message.HostLeftGameNewHostIsMod"), AmongUsClient.Instance.GetHost().Character?.GetRealName() ?? "null"));
                            _ = new LateTask(() =>
                            {
                                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Error);
                                GameManager.Instance.enabled = false;
                                Utils.NotifyGameEnding();
                                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
                            }, 3f, "Disconnect Error Auto-end");
                        }
                    }
                    else
                    {
                        var player = PlayerControl.LocalPlayer;
                        var title = "<color=#aaaaff>" + GetString("DefaultSystemMessageTitle") + "</color>";
                        var name = player?.Data?.PlayerName;
                        var msg = string.Format(GetString("Message.HostLeftGameNewHostIsNotMod"), AmongUsClient.Instance.GetHost().Character?.GetRealName() ?? "null");
                        player.SetName(title);
                        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg);
                        player.SetName(name);
                    }
                }, 0.5f, "On Host Disconnected");
            }

            switch (reason)
            {
                case DisconnectReasons.Hacking:
                    Logger.SendInGame(string.Format(GetString("PlayerLeftByAU-Anticheat"), data?.PlayerName));
                    break;
                case DisconnectReasons.Error when !GameStates.IsLobby:
                    Logger.SendInGame(string.Format(GetString("PlayerLeftByError"), data?.PlayerName));
                    break;
            }

            Logger.Info($"{data?.PlayerName} - (ClientID:{data?.Id} / FriendCode:{data?.FriendCode} / HashPuid:{data?.GetHashedPuid()} / Platform:{data?.PlatformData.Platform}) Disconnect (Reason:{reason}，Ping:{AmongUsClient.Instance.Ping})", "Session OnPlayerLeftPatch");

            // End the game when a Player exits game during assigning roles (AntiBlackOut Protect)
            if (Main.AssignRolesIsStarted)
            {
                CriticalErrorManager.SetCriticalError("The player left the game during assigning roles", true);
            }


            if (data != null)
            {
                Main.playerVersion.Remove(data.Id);
                Main.SayStartTimes.Remove(data.Id);
                Main.SayBanwordsTimes.Remove(data.Id);
            }

            if (AmongUsClient.Instance.AmHost)
            {
                if (GameStates.IsLobby && !GameStates.IsLocalGame)
                {
                    if (data?.GetHashedPuid() != "" && Options.TempBanPlayersWhoKeepQuitting.GetBool()
                        && !BanManager.CheckAllowList(data?.FriendCode))
                    {
                        if (!Main.PlayerQuitTimes.ContainsKey(data?.GetHashedPuid()))
                            Main.PlayerQuitTimes.Add(data?.GetHashedPuid(), 1);
                        else Main.PlayerQuitTimes[data?.GetHashedPuid()]++;

                        if (Main.PlayerQuitTimes[data?.GetHashedPuid()] >= Options.QuitTimesTillTempBan.GetInt())
                        {
                            BanManager.TempBanWhiteList.Add(data?.GetHashedPuid());
                        }
                    }
                }

                if (GameStates.IsMeeting)
                {
                    Swapper.CheckSwapperTarget(data.Character.PlayerId);

                    // Prevent double check end Voting
                    if (MeetingHud.Instance.state is MeetingHud.VoteStates.Discussion or MeetingHud.VoteStates.NotVoted or MeetingHud.VoteStates.Voted)
                    {
                        MeetingHud.Instance.CheckForEndVoting();
                    }
                }

                if (GameStates.IsInGame)
                {
                    if (data != null)
                    {
                        var networkedPlayerInfo = GameData.Instance.GetPlayerByClient(data);
                        if (networkedPlayerInfo != null)
                        {
                            networkedPlayerInfo.PlayerName = Main.AllClientRealNames[networkedPlayerInfo.ClientId];
                            networkedPlayerInfo.MarkDirty();
                        }
                    }
                }
            }
        }
        catch (Exception error)
        {
            Logger.Error(error.ToString(), "OnPlayerLeftPatch.Postfix");
        }

        StartingProcessing = false;
    }
}
[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.Spawn))]
class InnerNetClientSpawnPatch
{
    public static void Postfix([HarmonyArgument(1)] int ownerId, [HarmonyArgument(2)] SpawnFlags flags)
    {
        if (!AmongUsClient.Instance.AmHost || flags != SpawnFlags.IsClientCharacter) return;

        ClientData client = Utils.GetClientById(ownerId);

        Logger.Msg($"Spawn player data: ID {ownerId}: {client.PlayerName}", "InnerNetClientSpawn");

        if (client == null || client.Character == null // client is null
            || client.ColorId < 0 || Palette.PlayerColors.Length <= client.ColorId) // invalid client color
        {
            Logger.Warn("client is null or client have invalid color", "TrySyncAndSendMessage");
        }
        else
        {
            _ = new LateTask(() =>
            {
                OptionItem.SyncAllOptions(client.Id);
            }, 3f, "Sync All Options For New Player");

            _ = new LateTask(() =>
            {
                if (Main.OverrideWelcomeMsg != "") Utils.SendMessage(Main.OverrideWelcomeMsg, client.Character.PlayerId);
                else TemplateManager.SendTemplate("welcome", client.Character.PlayerId, true);
            }, 3f, "Welcome Message");

            _ = new LateTask(() =>
            {
                if (client == null || client.Character == null)
                {
                    Logger.Warn("client is null", "Spawn.RPCRequestRetryVersionCheck");
                    return;
                }

                var sender = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RequestRetryVersionCheck, SendOption.Reliable, client.Character.OwnerId);
                AmongUsClient.Instance.FinishRpcImmediately(sender);
            }, 3f, "RPC Request Retry Version Check");

            if (GameStates.IsOnlineGame)
            {
                _ = new LateTask(() =>
                {
                    if (GameStates.IsLobby && client.Character != null && LobbyBehaviour.Instance != null && GameStates.IsVanillaServer)
                    {
                        // Only for Vanilla
                        if (!client.Character.IsModded())
                        {
                            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(LobbyBehaviour.Instance.NetId, (byte)RpcCalls.LobbyTimeExpiring, SendOption.None, client.Id);
                            writer.WritePacked((int)GameStartManagerPatch.timer);
                            writer.Write(false);
                            AmongUsClient.Instance.FinishRpcImmediately(writer);
                        }
                        // Non-host modded client
                        else if (client.Character.IsNonHostModdedClient())
                        {
                            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncLobbyTimer, SendOption.Reliable, client.Id);
                            writer.WritePacked((int)GameStartManagerPatch.timer);
                            AmongUsClient.Instance.FinishRpcImmediately(writer);
                        }
                    }
                }, 3.1f, "Send RPC or Sync Lobby Timer");
            }

            if (Options.GradientTagsOpt.GetBool())
            {
                _ = new LateTask(() =>
                {
                    Utils.SendMessage(GetString("Warning.GradientTags"), client.Character.PlayerId);
                }, 3.3f, "GradientWarning");
            }
        }

        Main.GuessNumber[client.Character.PlayerId] = [-1, 7];

        if (Main.OverrideWelcomeMsg == "" && Main.PlayerStates.Count != 0 && Main.clientIdList.Contains(client.Id))
        {
            if (GameStates.IsNormalGame)
            {
                if (Options.AutoDisplayKillLog.GetBool() && Main.PlayerStates.Count != 0 && Main.clientIdList.Contains(client.Id))
                {
                    _ = new LateTask(() =>
                    {
                        if (!AmongUsClient.Instance.IsGameStarted && client.Character != null)
                        {
                            Main.isChatCommand = true;
                            Utils.ShowKillLog(client.Character.PlayerId);
                        }
                    }, 3f, "DisplayKillLog");
                }
                if (Options.AutoDisplayLastRoles.GetBool())
                {
                    _ = new LateTask(() =>
                    {
                        if (!AmongUsClient.Instance.IsGameStarted && client.Character != null)
                        {
                            Main.isChatCommand = true;
                            Utils.SendMessage("\n", client.Character.PlayerId, Main.LastSummaryMessage);
                        }
                    }, 3.1f, "DisplayLastRoles");
                }
                if (Options.AutoDisplayLastResult.GetBool())
                {
                    _ = new LateTask(() =>
                    {
                        if (!AmongUsClient.Instance.IsGameStarted && client.Character != null)
                        {
                            Main.isChatCommand = true;
                            Utils.ShowLastResult(client.Character.PlayerId);
                        }
                    }, 3.2f, "DisplayLastResult");
                }
                if (PlayerControl.LocalPlayer.FriendCode.GetDevUser().IsUp && Options.EnableUpMode.GetBool())
                {
                    _ = new LateTask(() =>
                    {
                        if (!AmongUsClient.Instance.IsGameStarted && client.Character != null)
                        {
                            Main.isChatCommand = true;
                            //     Utils.SendMessage($"{GetString("Message.YTPlanNotice")} {PlayerControl.LocalPlayer.FriendCode.GetDevUser().UpName}", client.Character.PlayerId);
                        }
                    }, 3.3f, "DisplayUpWarnning");
                }
            }
        }
    }
}
