using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using InnerNet;
using Rewired;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TOHE.Modules;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Neutral;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
class OnGameJoinedPatch
{
    public static void Postfix(AmongUsClient __instance)
    {
        while (!Options.IsLoaded) System.Threading.Tasks.Task.Delay(1);
        Logger.Info($"{__instance.GameId} Joining room", "OnGameJoined");
        Main.IsHostVersionCheating = false;
        Main.playerVersion = new Dictionary<byte, PlayerVersion>();
        if (!Main.VersionCheat.Value) RPC.RpcVersionCheck();
        SoundManager.Instance.ChangeAmbienceVolume(DataManager.Settings.Audio.AmbienceVolume);

        if (GameStates.IsModHost)
            Main.HostClientId = Utils.GetPlayerById(0)?.GetClientId() ?? -1;

        ChatUpdatePatch.DoBlockChat = false;
        GameStates.InGame = false;
        ErrorText.Instance.Clear();

        if (AmongUsClient.Instance.AmHost) // Execute the following only on the host
        {

            GameStartManagerPatch.GameStartManagerUpdatePatch.exitTimer = -1;
            Main.DoBlockNameChange = false;
            Main.newLobby = true;
            Main.DevRole = new();
            EAC.DeNum = new();
            Main.AllPlayerNames = new();
            Main.PlayerQuitTimes = new();

            if (Main.NormalOptions.KillCooldown == 0f)
                Main.NormalOptions.KillCooldown = Main.LastKillCooldown.Value;

            AURoleOptions.SetOpt(Main.NormalOptions.Cast<IGameOptions>());
            if (AURoleOptions.ShapeshifterCooldown == 0f)
                AURoleOptions.ShapeshifterCooldown = Main.LastShapeshifterCooldown.Value;

            _ = new LateTask(() =>
            {
                if (BanManager.CheckEACList(PlayerControl.LocalPlayer.FriendCode, PlayerControl.LocalPlayer.GetClient().GetHashedPuid()) && GameStates.IsOnlineGame)
                {
                    AmongUsClient.Instance.ExitGame(DisconnectReasons.Banned);
                    SceneChanger.ChangeScene("MainMenu");
                }
            }, 1f, "OnGameJoinedPatch");
        }
    }
}
[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.DisconnectInternal))]
class DisconnectInternalPatch
{
    public static void Prefix(InnerNetClient __instance, DisconnectReasons reason, string stringReason)
    {
        Logger.Info($"Disconnect (Reason:{reason}:{stringReason}, ping:{__instance.Ping})", "Reason Disconnect");
    }
}
[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
class OnPlayerJoinedPatch
{
    public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
    {
        Logger.Info($"{client.PlayerName}(ClientID:{client.Id}/FriendCode:{client.FriendCode}/HashPuid:{client.GetHashedPuid()}/Platform:{client.PlatformData.Platform}) Joining room", "Session");
        if (AmongUsClient.Instance.AmHost && client.FriendCode == "" && Options.KickPlayerFriendCodeNotExist.GetBool())
        {
            if (!Options.TempBanPlayerFriendCodeNotExist.GetBool())
            {
                AmongUsClient.Instance.KickPlayer(client.Id, false);
                Logger.SendInGame(string.Format(GetString("Message.KickedByNoFriendCode"), client.PlayerName));
                Logger.Info($"Kicked a player {client?.PlayerName} without a friend code", "Kick");
            }
            else
            {
                if (!BanManager.TempBanWhiteList.Contains(client.GetHashedPuid()))
                    BanManager.TempBanWhiteList.Add(client.GetHashedPuid());
                AmongUsClient.Instance.KickPlayer(client.Id, true);
                Logger.SendInGame(string.Format(GetString("Message.TempBannedByNoFriendCode"), client.PlayerName));
                Logger.Info($"TempBanned a player {client?.PlayerName} without a friend code", "Temp Ban");
            }
        }
        Platforms platform = client.PlatformData.Platform;
        if (AmongUsClient.Instance.AmHost && Options.KickOtherPlatformPlayer.GetBool() && platform != Platforms.Unknown)
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
        BanManager.CheckDenyNamePlayer(client);
        RPC.RpcVersionCheck();

        if (AmongUsClient.Instance.AmHost)
        {
            if (Main.SayStartTimes.ContainsKey(client.Id)) Main.SayStartTimes.Remove(client.Id);
            if (Main.SayBanwordsTimes.ContainsKey(client.Id)) Main.SayBanwordsTimes.Remove(client.Id);
            //if (Main.newLobby && Options.ShareLobby.GetBool()) Cloud.ShareLobby();
            if (client.GetHashedPuid() != "" && Options.TempBanPlayersWhoKeepQuitting.GetBool()
                && !FixedUpdatePatch.CheckAllowList(client.FriendCode))
            {
                if (Main.PlayerQuitTimes.ContainsKey(client.GetHashedPuid()))
                {
                    if (Main.PlayerQuitTimes[client.GetHashedPuid()] >= Options.QuitTimesTillTempBan.GetInt())
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
    public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData data, [HarmonyArgument(1)] DisconnectReasons reason)
    {
        try
        {
            if (GameStates.IsInGame)
            {
                if (data.Character.Is(CustomRoles.Lovers) && !data.Character.Data.IsDead)
                    foreach (var lovers in Main.LoversPlayers.ToArray())
                    {
                        Main.isLoversDead = true;
                        Main.LoversPlayers.Remove(lovers);
                        Main.PlayerStates[lovers.PlayerId].RemoveSubRole(CustomRoles.Lovers);
                    }
                if (data.Character.Is(CustomRoles.Executioner) && Executioner.Target.ContainsKey(data.Character.PlayerId))
                    Executioner.ChangeRole(data.Character);
                if (Executioner.Target.ContainsValue(data.Character.PlayerId))
                    Executioner.ChangeRoleByTarget(data.Character);
                if (data.Character.Is(CustomRoles.Lawyer) && Lawyer.Target.ContainsKey(data.Character.PlayerId))
                    Lawyer.ChangeRole(data.Character);
                if (Lawyer.Target.ContainsValue(data.Character.PlayerId))
                    Lawyer.ChangeRoleByTarget(data.Character);
                if (data.Character.Is(CustomRoles.Pelican))
                    Pelican.OnPelicanDied(data.Character.PlayerId);
                if (Spiritualist.SpiritualistTarget == data.Character.PlayerId)
                    Spiritualist.RemoveTarget();
                if (Main.PlayerStates[data.Character.PlayerId].deathReason == PlayerState.DeathReason.etc) // If no cause of death was established
                {
                    Main.PlayerStates[data.Character.PlayerId].deathReason = PlayerState.DeathReason.Disconnected;
                    Main.PlayerStates[data.Character.PlayerId].SetDead();
                }
                AntiBlackout.OnDisconnect(data.Character.Data);
                PlayerGameOptionsSender.RemoveSender(data.Character);
            }

            if (Main.HostClientId == __instance.ClientId)
            {
                var clientId = -1;
                var player = PlayerControl.LocalPlayer;
                var title = "<color=#aaaaff>" + GetString("DefaultSystemMessageTitle") + "</color>";
                var name = player?.Data?.PlayerName;
                var msg = "";
                if (GameStates.IsInGame)
                {
                    Utils.ErrorEnd("Host exits the game");
                    msg = GetString("Message.HostLeftGameInGame");
                }
                else if (GameStates.IsLobby)
                    msg = GetString("Message.HostLeftGameInLobby");

                player.SetName(title);
                DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg);
                player.SetName(name);

                var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
                writer.StartMessage(clientId);
                writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
                    .Write(title)
                    .EndRpc();
                writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
                    .Write(msg)
                    .EndRpc();
                writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
                    .Write(player.Data.PlayerName)
                    .EndRpc();
                writer.EndMessage();
                writer.SendMessage();
            }

            // 附加描述掉线原因
            switch (reason)
            {
                case DisconnectReasons.Hacking:
                    Logger.SendInGame(string.Format(GetString("PlayerLeftByAU-Anticheat"), data?.PlayerName));
                    break;
                case DisconnectReasons.Error:
                    Logger.SendInGame(string.Format(GetString("PlayerLeftByError"), data?.PlayerName));
                    _ = new LateTask(() =>
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Error);
                        GameManager.Instance.enabled = false;
                        GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
                    }, 3f, "Disconnect Error Auto-end");

                    break;
            }

            Logger.Info($"{data?.PlayerName} - (ClientID:{data?.Id} / FriendCode:{data?.FriendCode} / HashPuid:{data?.GetHashedPuid()} / Platform:{data?.PlatformData.Platform}) Disconnect (Reason:{reason}，Ping:{AmongUsClient.Instance.Ping})", "Session");

            if (AmongUsClient.Instance.AmHost)
            {
                Main.SayStartTimes.Remove(__instance.ClientId);
                Main.SayBanwordsTimes.Remove(__instance.ClientId);
                Main.playerVersion.Remove(data?.Character?.PlayerId ?? byte.MaxValue);

                if (GameStates.IsLobby)
                {
                    if (data?.GetHashedPuid() != "" && Options.TempBanPlayersWhoKeepQuitting.GetBool()
                        && !FixedUpdatePatch.CheckAllowList(data?.FriendCode))
                    {
                        if (!Main.PlayerQuitTimes.ContainsKey(data?.GetHashedPuid()))
                            Main.PlayerQuitTimes.Add(data?.GetHashedPuid(), 1);
                        else Main.PlayerQuitTimes[data?.FriendCode]++;

                        if (Main.PlayerQuitTimes[data?.GetHashedPuid()] >= Options.QuitTimesTillTempBan.GetInt())
                        {
                            BanManager.TempBanWhiteList.Add(data?.GetHashedPuid());
                            //should ban on player's next join game
                        }
                    }
                }
            }
        }
        catch (Exception error)
        {
            Logger.Error(error.ToString(), "OnPlayerLeftPatch.Postfix");
            //Logger.SendInGame("Error: " + error.ToString());
        }
    }
}
[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CreatePlayer))]
class CreatePlayerPatch
{
    public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        Logger.Msg($"Create player data: ID {client.Character.PlayerId}: {client.PlayerName}", "CreatePlayer");

        // Standard nickname
        var name = client.PlayerName;
        if (Options.FormatNameMode.GetInt() == 2 && client.Id != AmongUsClient.Instance.ClientId)
            name = Main.Get_TName_Snacks;
        else
        {
            name = name.RemoveHtmlTags().Replace(@"\", string.Empty).Replace("/", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("<", string.Empty).Replace(">", string.Empty);
            if (name.Length > 10) name = name[..10];
            if (Options.DisableEmojiName.GetBool()) name = Regex.Replace(name, @"\p{Cs}", string.Empty);
            if (Regex.Replace(Regex.Replace(name, @"\s", string.Empty), @"[\x01-\x1F,\x7F]", string.Empty).Length < 1) name = Main.Get_TName_Snacks;
        }
        Main.AllPlayerNames.Remove(client.Character.PlayerId);
        Main.AllPlayerNames.TryAdd(client.Character.PlayerId, name);
        if (!name.Equals(client.PlayerName))
        {
            _ = new LateTask(() =>
            {
                if (client.Character == null) return;
                Logger.Warn($"Standard nickname：{client.PlayerName} => {name}", "Name Format");
                client.Character.RpcSetName(name);
            }, 1f, "Name Format");
        }

        _ = new LateTask(() => { if (client.Character == null || client == null) return; OptionItem.SyncAllOptions(client.Id); }, 3f, "Sync All Options For New Player");
        Main.GuessNumber[client.Character.PlayerId] = new List<int> { -1, 7 };

        _ = new LateTask(() =>
        {
            //Logger.Warn($"{client.Character.CurrentOutfit.ColorId},{client.Character.CurrentOutfit.HatId}, {client.Character.CurrentOutfit.SkinId}, {client.Character.CurrentOutfit.VisorId}, {client.Character.CurrentOutfit.PetId}", "SKIN LOGGED");

            if (client.Character == null) return;
            if (Main.OverrideWelcomeMsg != "") Utils.SendMessage(Main.OverrideWelcomeMsg, client.Character.PlayerId);
            else TemplateManager.SendTemplate("welcome", client.Character.PlayerId, true);
        }, 3f, "Welcome Message");

        _ = new LateTask(() =>
        {
            if (Options.GradientTagsOpt.GetBool())
            {
                if (client.Character == null) return;
                Utils.SendMessage(GetString("Warning.GradientTags"),client.Character.PlayerId);
            }
        }, 3.3f, "GradientWarning");

        if (Main.OverrideWelcomeMsg == "" && Main.PlayerStates.Count != 0 && Main.clientIdList.Contains(client.Id))
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
                        Utils.ShowLastRoles(client.Character.PlayerId);
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