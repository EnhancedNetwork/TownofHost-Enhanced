using Hazel;
using InnerNet;
using System;
using static TOHE.Translator;

namespace TOHE;

internal class EAC
{
    public static int DeNum = 0;
    private static List<byte> LobbyDeadBodies = [];
    public static void Init()
    {
        DeNum = new();
        OriginalRoles = [];
        ReportTimes = [];
        LobbyDeadBodies = [];
    }
    public static void WarnHost(int denum = 1)
    {
        DeNum += denum;
        if (ErrorText.Instance != null)
        {
            ErrorText.Instance.CheatDetected = DeNum > 3;
            ErrorText.Instance.SBDetected = DeNum > 10;
            if (ErrorText.Instance.CheatDetected)
                ErrorText.Instance.AddError(ErrorText.Instance.SBDetected ? ErrorCode.SBDetected : ErrorCode.CheatDetected);
            else
                ErrorText.Instance.Clear();
        }
    }
    public static bool PlayerControlReceiveRpc(PlayerControl pc, byte callId, MessageReader reader)
    {
        // nvm, it works so im not doing more changes

        if (!AmongUsClient.Instance.AmHost) return false;
        //if (RoleBasisChanger.IsChangeInProgress) return false;
        if (pc == null || reader == null) return false;
        try
        {
            MessageReader sr = MessageReader.Get(reader);
            var rpc = (RpcCalls)callId;
            switch (rpc)
            {
                // Check name is now done in PlayerControl.CheckName
                case RpcCalls.CheckName:
                    if (!GameStates.IsLobby)
                    {
                        WarnHost();
                        Report(pc, "CheckName out of Lobby");
                        HandleCheat(pc, "CheckName out of Lobby");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】CheckName out of lobby，已驳回", "EAC");
                        return true;
                    }
                    break;
                /*
                case RpcCalls.SetName:
                    //Only sent by host
                    WarnHost();
                    Report(pc, "Directly SetName");
                    HandleCheat(pc, "Directly SetName");
                    Logger.Fatal($"Directly SetName【{pc.GetClientId()}:{pc.GetRealName()}】已驳回", "EAC");
                    return true;
                case RpcCalls.SetRole:
                    //Only sent by host
                    WarnHost();
                    Report(pc, "Directly SetRole");
                    HandleCheat(pc, "Directly SetRole");
                    Logger.Fatal($"Directly SetRole for【{pc.GetClientId()}:{pc.GetRealName()}】已驳回", "EAC");
                    break;
                */
                // Disabled due to host sending these rpcs to itself using custom sender
                case RpcCalls.SendChat:
                    var text = sr.ReadString();
                    if ((
                        text.Contains('░') ||
                        text.Contains('▄') ||
                        text.Contains('█') ||
                        text.Contains('▌') ||
                        text.Contains('▒') ||
                        text.Contains("习近平")
                        ))
                    {
                        Report(pc, "非法消息");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】发送非法消息，已驳回", "EAC");
                        return true;
                    }
                    break;
                case RpcCalls.StartMeeting:
                    //Only sent by host
                    WarnHost();
                    Report(pc, "Bad StartMeeting");
                    HandleCheat(pc, "Bad StartMeeting");
                    Logger.Fatal($"非法设置玩家【{pc.GetClientId()}:{pc.GetRealName()}】的游戏名称，已驳回", "EAC");
                    return true;
                case RpcCalls.ReportDeadBody:
                    var bodyid = sr.ReadByte();
                    if (!GameStates.IsInGame)
                    {
                        if (!LobbyDeadBodies.Contains(bodyid))
                        {
                            WarnHost();
                            Report(pc, "Report body out of game A");
                            HandleCheat(pc, "Report body out of game A");
                            Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非游戏内开会，已驳回", "EAC");
                            return true;
                        }
                        else
                        {
                            Report(pc, "Try to Report body out of game B (May be false)");
                            Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】尝试举报可能被非法击杀的尸体，已驳回", "EAC");
                        }
                    }
                    if (ReportTimes.TryGetValue(pc.PlayerId, out int rtimes))
                    {
                        if (rtimes > 14)
                        {
                            WarnHost();
                            Report(pc, "Spam report bodies A");
                            HandleCheat(pc, "Spam report bodies A");
                            Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】刷报告尸体满14次，已驳回", "EAC");
                            return true;
                        }
                    }
                    break;
                case RpcCalls.CheckColor:
                    if (!GameStates.IsLobby)
                    {
                        WarnHost();
                        Report(pc, "CheckColor out of Lobby");
                        HandleCheat(pc, "CheckColor out of Lobby");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】check color out of lobby，已驳回", "EAC");
                        return true;
                    }
                    break;
                // Some mods may add custom colors. Skip check color check
                case RpcCalls.SetColor:
                    // Only sent by Host
                    Report(pc, "Directly SetColor");
                    HandleCheat(pc, "Directly SetColor");
                    Logger.Fatal($"Directly SetColor【{pc.GetClientId()}:{pc.GetRealName()}】已驳回", "EAC");
                    return true;
                case RpcCalls.CheckMurder:
                    if (GameStates.IsLobby)
                    {
                        WarnHost();
                        Report(pc, "CheckMurder in Lobby");
                        HandleCheat(pc, "CheckMurder in Lobby");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法检查击杀，已驳回", "EAC");
                        return true;
                    }
                    break;
                case RpcCalls.MurderPlayer:
                    //Calls will only be sent by host(under protocol) / server(vanilla)
                    var murdered = sr.ReadNetObject<PlayerControl>();
                    if (GameStates.IsLobby)
                    {
                        Report(pc, "Directly Murder Player In Lobby");
                        HandleCheat(pc, "Directly Murder Player In Lobby");
                        if (murdered != null && !LobbyDeadBodies.Contains(murdered.PlayerId))
                        {
                            LobbyDeadBodies.Add(murdered.PlayerId);
                        }
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】大厅直接击杀，已驳回", "EAC");
                        return true;
                    }
                    Report(pc, "Directly Murder Player");
                    HandleCheat(pc, "Directly Murder Player");
                    Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】直接击杀，已驳回", "EAC");
                    return true;
                case RpcCalls.CheckShapeshift:
                    if (GameStates.IsLobby)
                    {
                        Report(pc, "Lobby Check Shapeshift");
                        HandleCheat(pc, "Lobby Check Shapeshift");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】大厅直接变形，已驳回", "EAC");
                        return true;
                    }
                    break;
                case RpcCalls.Shapeshift:
                    {
                        // Only be sent by host
                        Report(pc, "Directly Shapeshift");
                        MessageWriter swriter = AmongUsClient.Instance.StartRpcImmediately(pc.NetId, (byte)RpcCalls.Shapeshift, SendOption.Reliable, -1);
                        swriter.WriteNetObject(pc);
                        swriter.Write(false);
                        AmongUsClient.Instance.FinishRpcImmediately(swriter);
                        HandleCheat(pc, "Directly Shapeshift");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】直接变形，已驳回", "EAC");
                        return true;
                    }
                // Skip Check Phantom Rpcs bcz I dont know what the mod will do with phantom
                case RpcCalls.StartVanish:
                case RpcCalls.StartAppear:
                    {
                        var sreason = "Directly Phantom Rpcs " + rpc.ToString();
                        // Only be sent by host
                        Report(pc, sreason);
                        var swriter = AmongUsClient.Instance.StartRpcImmediately(pc.NetId, (byte)RpcCalls.StartAppear, SendOption.Reliable, -1);
                        swriter.Write(false);
                        AmongUsClient.Instance.FinishRpcImmediately(swriter);
                        HandleCheat(pc, sreason);
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()} {sreason}，已驳回", "EAC");
                        return true;
                    }
                case RpcCalls.SendChatNote:
                    // Only sent by Host
                    Report(pc, "Directly Send ChatNote");
                    HandleCheat(pc, "Directly Send ChatNote");
                    Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】直接Send ChatNote，已驳回", "EAC");
                    return true;
            }
            switch (callId)
            {
                case 101: // Aum Chat
                    try
                    {
                        var firstString = sr.ReadString();
                        var secondString = sr.ReadString();
                        sr.ReadInt32();

                        var flag = string.IsNullOrEmpty(firstString) && string.IsNullOrEmpty(secondString);

                        if (!flag)
                        {
                            Report(pc, "Aum Chat RPC");
                            HandleCheat(pc, "Aum Chat RPC");
                            Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】发送AUM聊天，已驳回", "EAC");
                            return true;
                        }
                    }
                    catch
                    {
                        // Do nothing
                    }
                    break;
                case unchecked((byte)42069): // 85 AUM
                    try
                    {
                        var aumid = sr.ReadByte();

                        if (aumid == pc.PlayerId)
                        {
                            Report(pc, "Aum RPC");
                            HandleCheat(pc, "Aum RPC");
                            Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】发送AUM RPC，已驳回", "EAC");
                            return true;
                        }
                    }
                    catch
                    {
                        // Do nothing
                    }
                    break;
                case 119: // KN Chat
                    try
                    {
                        var firstString = sr.ReadString();
                        var secondString = sr.ReadString();
                        sr.ReadInt32();

                        var flag = string.IsNullOrEmpty(firstString) && string.IsNullOrEmpty(secondString);

                        if (!flag)
                        {
                            Report(pc, "KN Chat RPC");
                            HandleCheat(pc, "KN Chat RPC");
                            Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】发送KN聊天，已驳回", "EAC");
                            return true;
                        }
                    }
                    catch
                    {
                        // Do nothing
                    }
                    break;
                case 250: // KN
                    if (sr.BytesRemaining == 0)
                    {
                        Report(pc, "KN RPC");
                        HandleCheat(pc, "KN RPC");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】发送KN RPC，已驳回", "EAC");
                        return true;
                    }
                    break;
                case unchecked((byte)420): // 164 Sicko
                    if (sr.BytesRemaining == 0)
                    {
                        Report(pc, "Sicko RPC");
                        HandleCheat(pc, "Sicko RPC");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】发送Sicko RPC，已驳回", "EAC");
                        return true;
                    }
                    break;
                case 7:
                case 8:
                    if (!GameStates.IsLobby)
                    {
                        WarnHost();
                        Report(pc, "非法设置颜色");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法设置颜色，已驳回", "EAC");
                        return true;
                    }
                    break;
                case 5:
                    string name = sr.ReadString();
                    if (GameStates.IsInGame)
                    {
                        WarnHost();
                        Report(pc, "非法设置游戏名称");
                        Logger.Fatal($"非法修改玩家【{pc.GetClientId()}:{pc.GetRealName()}】的游戏名称，已驳回", "EAC");
                        return true;
                    }
                    break;
                case 47:
                    if (GameStates.IsLobby)
                    {
                        WarnHost();
                        Report(pc, "非法击杀");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法击杀，已驳回", "EAC");
                        return true;
                    }
                    break;
                case 38:
                    if (GameStates.IsInGame)
                    {
                        WarnHost();
                        Report(pc, "Set level in game");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】游戏内改等级，已驳回", "EAC");
                        return true;
                    }
                    //if (sr.ReadPackedUInt32() > 0)
                    //{
                    //    uint ClientDataLevel = pc.GetClient() == null ? pc.GetClient().PlayerLevel : 0;
                    //    uint PlayerControlLevel = sr.ReadPackedUInt32();
                    //    if (ClientDataLevel != 0 && Math.Abs(PlayerControlLevel - ClientDataLevel) > 4)
                    //    {
                    //        WarnHost();
                    //        Report(pc, "Sus Level Change");
                    //        AmongUsClient.Instance.KickPlayer(pc.GetClientId(), false);
                    //        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】游戏内改等级，已驳回", "EAC");
                    //        return true;
                    //    }
                    //}
                    //Buggy
                    break;
                case 39:
                case 40:
                case 41:
                case 42:
                case 43:
                    if (GameStates.IsInGame)
                    {
                        WarnHost();
                        Report(pc, "Change skin in game");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】游戏内改皮肤，已驳回", "EAC");
                        return true;
                    }
                    break;
            }
        }
        catch (Exception e)
        {
            Logger.Exception(e, "EAC");
            //throw e;
        }
        WarnHost(-1);
        return false;
    }
    public static Dictionary<byte, CustomRoles> OriginalRoles = [];
    public static void LogAllRoles()
    {
        foreach (var pc in Main.AllPlayerControls.ToArray())
        {
            try
            {
                OriginalRoles.Add(pc.PlayerId, pc.GetCustomRole());
            }
            catch (Exception error)
            {
                Logger.Fatal(error.ToString(), "EAC.LogAllRoles");
            }
        }
    }
    public static bool RpcUpdateSystemCheck(PlayerControl player, SystemTypes systemType, byte amount)
    {
        if (GameStates.IsLocalGame)
        {
            return false;
        }
        //Update system rpc can not get received by playercontrol.handlerpc
        var Mapid = Utils.GetActiveMapId();
        Logger.Info("Check sabotage RPC" + ", PlayerName: " + player.GetNameWithRole() + ", SabotageType: " + systemType.ToString() + ", amount: " + amount.ToString(), "EAC");
        if (!AmongUsClient.Instance.AmHost) return false;

        if (player == null)
        {
            Logger.Warn("PlayerControl is null", "EAC RpcUpdateSystemCheck");
            return true;
        }

        if (systemType == SystemTypes.Sabotage) //Normal sabotage using buttons
        {
            if (!player.HasImpKillButton(true))
            {
                WarnHost();
                Report(player, "Bad Sabotage A : Non Imp");
                HandleCheat(player, "Bad Sabotage A : Non Imp");
                Logger.Fatal($"玩家【{player.GetClientId()}:{player.GetRealName()}】Bad Sabotage A，已驳回", "EAC");
                return true;
            }
        } //Cheater directly send 128 systemtype rpc
        else if (systemType == SystemTypes.LifeSupp)
        {
            if (Mapid != 0 && Mapid != 1 && Mapid != 3) goto YesCheat;
            else if (amount != 64 && amount != 65) goto YesCheat;
        }
        else if (systemType == SystemTypes.Comms)
        {
            if (amount == 0)
            {
                if (Mapid == 1 || Mapid == 5) goto YesCheat;
            }
            else if (amount == 64 || amount == 65 || amount == 32 || amount == 33 || amount == 16 || amount == 17)
            {
                if (!(Mapid == 1 || Mapid == 5)) goto YesCheat;
            }
            else goto YesCheat;
        }
        else if (systemType == SystemTypes.Electrical)
        {
            if (Mapid == 5) goto YesCheat;
            if (amount >= 5) //0 - 4 normal lights. other sabotage, Should never be sent by client
            {
                goto YesCheat;
            }
        }
        else if (systemType == SystemTypes.Laboratory)
        {
            if (Mapid != 2) goto YesCheat;
            else if (!(amount == 64 || amount == 65 || amount == 32 || amount == 33)) goto YesCheat;
        }
        else if (systemType == SystemTypes.Reactor)
        {
            if (Mapid == 2 || Mapid == 4) goto YesCheat;
            else if (!(amount == 64 || amount == 65 || amount == 32 || amount == 33)) goto YesCheat;
            //Airship use heli sabotage /Other use 64,65 | 32,33
        }
        else if (systemType == SystemTypes.HeliSabotage)
        {
            if (Mapid != 4) goto YesCheat;
            else if (!(amount == 64 || amount == 65 || amount == 16 || amount == 17 || amount == 32 || amount == 33)) goto YesCheat;
        }
        else if (systemType == SystemTypes.MushroomMixupSabotage)
        {
            goto YesCheat;
            //Normal clients will never directly send MushroomMixupSabotage
        }

        if (GameStates.IsMeeting && MeetingHud.Instance.state != MeetingHud.VoteStates.Animating || GameStates.IsExilling)
        {
            WarnHost();
            Report(player, "Bad Sabotage D : In Meeting");
            Logger.Fatal($"玩家【{player.GetClientId()}:{player.GetRealName()}】Bad Sabotage D，已驳回", "EAC");
            return true;
        }
        //There may be cases where a player is fixing reactor and a meeting start, triggering EAC check in meeting

        return false;

    YesCheat:
        {
            WarnHost();
            Report(player, "Bad Sabotage C : Hack send RPC");
            HandleCheat(player, "Bad Sabotage C");
            Logger.Fatal($"玩家【{player.GetClientId()}:{player.GetRealName()}】Bad Sabotage C，已驳回", "EAC");
            return true;
        }
    }

    public static Dictionary<byte, int> ReportTimes = [];
    public static bool RpcReportDeadBodyCheck(PlayerControl player, NetworkedPlayerInfo target)
    {
        if (!ReportTimes.ContainsKey(player.PlayerId))
        {
            ReportTimes.Add(player.PlayerId, 0);
        }
        //target == null , button event
        if (target == null || !Main.OverDeadPlayerList.Contains(target.PlayerId))
        {
            ReportTimes[player.PlayerId]++;
        }

        if (!GameStates.IsInGame)
        {
            WarnHost();
            Report(player, "Report body out of game C");
            HandleCheat(player, "Report body out of game C");
            Logger.Fatal($"玩家【{player.GetClientId()}:{player.GetRealName()}】非游戏内开会，已驳回", "EAC");
            return true;
        }

        if (ReportTimes[player.PlayerId] >= 14)
        {
            //I believe nobody can report 14 different bodies in a single round if host players normally
            //This check is still not enough to stop spam meeting hacks or crazy hosts that spam kill command
            WarnHost();
            Report(player, "Spam report bodies");
            HandleCheat(player, "Spam report bodies");
            Logger.Fatal($"玩家【{player.GetClientId()}:{player.GetRealName()}】刷报告尸体满14次，已驳回", "EAC");
            return true;
        }
        if (GameStates.IsMeeting)
        {
            //Cancel rpc report body if a meeting is already held
            Logger.Info($"玩家【{player.GetClientId()}:{player.GetRealName()}】在会议期间开会，已驳回", "EAC");
            return true;
        }

        return false;
        // Niko intended to do report living player check,
        // but concerning roles like bait, hacker somehow never use report dead body,
        // Niko gave up
    }

    public static bool PlayerPhysicsRpcCheck(PlayerPhysics __instance, byte callId, MessageReader reader)
    {
        if (!AmongUsClient.Instance.AmHost) return false;

        var rpcType = (RpcCalls)callId;
        MessageReader subReader = MessageReader.Get(reader);

        bool hasVent(int ventId) => ShipStatus.Instance.AllVents.Any(v => v.Id == ventId);
        bool hasLadder(int ladderId) => ShipStatus.Instance.Ladders.Any(l => l.Id == ladderId);

        var player = __instance.myPlayer;

        if (!player)
        {
            Logger.Warn("Received Physics RPC without a player", "EAC_PlayerPhysics");
            return true;
        }

        if (GameStates.IsLobby && rpcType is not RpcCalls.Pet and not RpcCalls.CancelPet)
        {
            WarnHost();
            Report(player, $"Physics {rpcType} in lobby (can be spoofed by others)");
            HandleCheat(player, $"Physics {rpcType} in lobby (can be spoofed by others)");
            Logger.Fatal($"【{player.GetClientId()}:{player.GetRealName()}】 attempted to {rpcType} in lobby.", "EAC_physics");
            return true;
        }

        switch (rpcType)
        {
            case RpcCalls.EnterVent:
            case RpcCalls.ExitVent:
                int ventid = subReader.ReadPackedInt32();
                if (!hasVent(ventid))
                {
                    if (AmongUsClient.Instance.AmHost)
                    {
                        WarnHost();
                        Report(player, "Vent null vent (can be spoofed by others)");
                        HandleCheat(player, "Vent null vent (can be spoofed by others)");
                        Logger.Fatal($"【{player.GetClientId()}:{player.GetRealName()}】 attempted to enter a unexisting vent. {ventid}", "EAC_physics");
                    }
                    else
                    {
                        // Not sure whether host will send null vent to a player huh
                        Logger.Warn($"【{player.GetClientId()}:{player.GetRealName()}】 attempted to enter a unexisting vent. {ventid}", "EAC_physics");
                        if (rpcType is RpcCalls.ExitVent)
                        {
                            player.Visible = true;
                            player.inVent = false;
                            player.moveable = true;
                            player.NetTransform.SetPaused(false);
                        }
                    }
                    return true;
                }
                break;

            case RpcCalls.BootFromVent:
                //int ventid2 = subReader.ReadPackedInt32();
                //if (!hasVent(ventid2))
                //{
                //    if (AmongUsClient.Instance.AmHost)
                //    {
                //        WarnHost();
                //        Report(player, "Got booted from a null vent (can be spoofed by others)");
                //        AmongUsClient.Instance.KickPlayer(player.GetClientId(), false);
                //        Logger.Fatal($"【{player.GetClientId()}:{player.GetRealName()}】 attempted to boot from a unexisting vent. {ventid2}", "EAC_physics");
                //    }
                //    else
                //    {
                //        // Not sure whether host will send null vent to a player huh
                //        // Nah, host may send 99 boot from vent, which is stupid
                //        Logger.Warn($"【{player.GetClientId()}:{player.GetRealName()}】 attempted to boot from a unexisting vent. {ventid2}", "EAC_physics");
                //        if (ventid2 == 99 && player.inVent)
                //        {
                //            __instance.BootFromVent(ventid2);
                //            return true;
                //        }
                //        player.Visible = true;
                //        player.inVent = false;
                //        player.moveable = true;
                //        player.NetTransform.SetPaused(false);
                //    }
                //    return true;
                //}

                // BootFromVent can only be sent by host
                WarnHost();
                Report(player, "Got boot from vent from clients, can be spoofed");
                HandleCheat(player, "Got boot from vent from clients, can be spoofed");
                Logger.Fatal($"【{player.GetClientId()}:{player.GetRealName()}】 sent boot from vent, can be spoofed.", "EAC_physics");
                break;

            case RpcCalls.ClimbLadder:
                int ladderId = subReader.ReadPackedInt32();
                if (!hasLadder(ladderId))
                {
                    if (AmongUsClient.Instance.AmHost)
                    {
                        WarnHost();
                        Report(player, "climb null ladder (can be spoofed by others)");
                        HandleCheat(player, "climb null ladder (can be spoofed by others)");
                        Logger.Fatal($"【{player.GetClientId()}:{player.GetRealName()}】 attempted to climb a unexisting ladder.", "EAC_physics");
                    }
                    return true;
                }
                if (player.AmOwner)
                {
                    Logger.Fatal($"Got climb ladder for my self, this is impossible", "EAC_physics");
                    return true;
                }
                break;

            case RpcCalls.Pet:
                if (player.AmOwner)
                {
                    Logger.Fatal($"Got pet pet for my self, this is impossible", "EAC_physics");
                    return true;
                }

                // if (player.CurrentOutfit.PetId == "")
                // Petting air is fine i guess lol
                break;
        }
        return false;
    }

    public static void Report(PlayerControl pc, string reason)
    {
        if (pc == null)
        {
            Logger.Warn("Report PlayerControl is null", "EAC Report");
            return;
        }

        string msg = $"{pc.GetClientId()}|{pc.FriendCode}|{pc.Data.PlayerName}|{pc.GetClient().GetHashedPuid()}|{reason}";
        //Cloud.SendData(msg);
        Logger.Fatal($"EAC报告：{msg}", "EAC Cloud");
        if (Options.CheatResponses.GetInt() != 5)
            Logger.SendInGame(string.Format(GetString("Message.NoticeByEAC"), $"{pc?.Data?.PlayerName} | {pc.GetClient().GetHashedPuid()}", reason));
    }
    public static void HandleCheat(PlayerControl pc, string text)
    {
        if (pc == null)
        {
            Logger.Warn("Target PlayerControl is null", "EAC HandleCheat");
            return;
        }

        switch (Options.CheatResponses.GetInt())
        {
            case 0:
                AmongUsClient.Instance.KickPlayer(pc.GetClientId(), true);
                string msg0 = string.Format(GetString("Message.BannedByEAC"), pc?.Data?.PlayerName, text);
                Logger.Warn(msg0, "EAC");
                Logger.SendInGame(msg0);
                break;
            case 1:
                AmongUsClient.Instance.KickPlayer(pc.GetClientId(), false);
                string msg1 = string.Format(GetString("Message.KickedByEAC"), pc?.Data?.PlayerName, text);
                Logger.Warn(msg1, "EAC");
                Logger.SendInGame(msg1);
                break;
            case 2:
                Utils.SendMessage(string.Format(GetString("Message.NoticeByEAC"), pc?.Data?.PlayerName, text), PlayerControl.LocalPlayer.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), GetString("MessageFromEAC")));
                break;
            case 3:
                foreach (var apc in Main.AllPlayerControls.Where(x => x.PlayerId != pc?.Data?.PlayerId).ToArray())
                    Utils.SendMessage(string.Format(GetString("Message.NoticeByEAC"), pc?.Data?.PlayerName, text), apc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), GetString("MessageFromEAC")));
                break;
            case 4:
                if (!BanManager.TempBanWhiteList.Contains(pc.GetClient().GetHashedPuid()))
                    BanManager.TempBanWhiteList.Add(pc.GetClient().GetHashedPuid());
                AmongUsClient.Instance.KickPlayer(pc.GetClientId(), true);
                string msg2 = string.Format(GetString("Message.TempBannedByEAC"), pc?.Data?.PlayerName, text);
                Logger.Warn(msg2, "EAC");
                Logger.SendInGame(msg2);
                break;
            case 5:
                Logger.Info("No Handle Cheat", "MalumMenu_On_Top");
                //Hope u like this scp =(
                //Even handle the cheats brings lag
                break;
        }
    }
}
