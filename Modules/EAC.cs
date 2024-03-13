using AmongUs.GameOptions;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using InnerNet;
using static TOHE.Translator;
using TOHE.Modules.ChatManager;

namespace TOHE;

internal class EAC
{
    public static int DeNum = 0;
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
    public static bool ReceiveRpc(PlayerControl pc, byte callId, MessageReader reader)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (pc == null || reader == null) return false;
        try
        {
            MessageReader sr = MessageReader.Get(reader);
            var rpc = (RpcCalls)callId;
            switch (rpc)
            {
                //case RpcCalls.CheckName:
                //    string name = sr.ReadString();
                //    if (sr.BytesRemaining > 0 && sr.ReadBoolean()) return false;
                //    if (
                //        ((name.Contains("<size") || name.Contains("size>")) && name.Contains('?') && !name.Contains("color")) ||
                //        name.Length > 160 ||
                //        name.Count(f => f.Equals("\"\\n\"")) > 3 ||
                //        name.Count(f => f.Equals("\n")) > 3 ||
                //        name.Count(f => f.Equals("\r")) > 3 ||
                //        name.Contains("░") ||
                //        name.Contains("▄") ||
                //        name.Contains("█") ||
                //        name.Contains("▌") ||
                //        name.Contains("▒") ||
                //        name.Contains("习近平")
                //        )
                //    {
                //        WarnHost();
                //        Report(pc, "非法检查游戏名称");
                //        Logger.Fatal($"玩家非法检查名称【{pc.GetClientId()}:{pc.GetRealName()}】，已驳回", "EAC");
                //        return true;
                //    }
                //    break;
                //case RpcCalls.SetName:
                //    if (!GameStates.IsLobby)
                //    {
                //        WarnHost();
                //        Report(pc, "Bad SetName rpc");
                //        HandleCheat(pc, "Bad SetName rpc");
                //        Logger.Fatal($"非法设置玩家【{pc.GetClientId()}:{pc.GetRealName()}】的游戏名称，已驳回", "EAC");
                //        return true;
                //    }
                //    break;
                case RpcCalls.SetRole:
                    var role = (RoleTypes)sr.ReadUInt16();
                    if (GameStates.IsLobby && (role is RoleTypes.CrewmateGhost or RoleTypes.ImpostorGhost))
                    {
                        WarnHost();
                        Report(pc, "非法设置状态为幽灵");
                        Logger.Fatal($"非法设置玩家【{pc.GetClientId()}:{pc.GetRealName()}】的状态为幽灵，已驳回", "EAC");
                        return true;
                    }
                    break;
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
                    if (!GameStates.IsInGame)
                    {
                        WarnHost();
                        Report(pc, "Report body out of game A");
                        HandleCheat(pc, "Report body out of game A");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非游戏内开会，已驳回", "EAC");
                        return true;
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
                case RpcCalls.SetColor:
                case RpcCalls.CheckColor:
                    var color = sr.ReadByte();
                    if (!GameStates.IsLobby)
                    {
                        WarnHost();
                        Report(pc, "Set color in game");
                        HandleCheat(pc, "Set color in game");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】游戏内设置颜色，已驳回", "EAC");
                        return true;
                    }
                    if (pc.Data.DefaultOutfit.ColorId != -1 &&
                        (Main.AllPlayerControls.Count(x => x.Data.DefaultOutfit.ColorId == color) >= 5
                        || color < 0 || color > 18))
                    {
                        WarnHost();
                        Report(pc, "非法设置颜色");
                        AmongUsClient.Instance.KickPlayer(pc.GetClientId(), false);
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法设置颜色，已驳回", "EAC");
                        return true;
                    }
                    if (pc.AmOwner)
                    {
                        WarnHost();
                        Report(pc, "非法设置房主颜色");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法设置房主颜色，已驳回", "EAC");
                        return true;
                    }
                    break;
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
                    Report(pc, "Directly Shapeshift");
                    var swriter = AmongUsClient.Instance.StartRpcImmediately(pc.NetId, (byte)RpcCalls.Shapeshift, SendOption.Reliable, -1);
                    swriter.WriteNetObject(pc);
                    swriter.Write(false);
                    AmongUsClient.Instance.FinishRpcImmediately(swriter);
                    HandleCheat(pc, "Directly Shapeshift");
                    Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】直接变形，已驳回", "EAC");
                    return true;

            }
            switch (callId)
            {
                case 101:
                    var AUMChat = sr.ReadString();
                    WarnHost();
                    Report(pc, "AUM");
                    HandleCheat(pc, GetString("EAC.CheatDetected.EAC"));
                    return true;
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
                    if (pc.AmOwner)
                    {
                        WarnHost();
                        Report(pc, "Change Host skin");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】改房主皮肤，已驳回", "EAC");
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
        //Update system rpc can not get received by playercontrol.handlerpc
        var Mapid = Utils.GetActiveMapId();
        Logger.Info("Check sabotage RPC" + ", PlayerName: " + player.GetNameWithRole() + ", SabotageType: " + systemType.ToString() + ", amount: " + amount.ToString(), "EAC");
        if (!AmongUsClient.Instance.AmHost) return false;
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
    public static bool RpcReportDeadBodyCheck(PlayerControl player, GameData.PlayerInfo target)
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
            Report(player, "Report body out of game");
            HandleCheat(player, "Report body out of game");
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
    public static void Report(PlayerControl pc, string reason)
    {
        string msg = $"{pc.GetClientId()}|{pc.FriendCode}|{pc.Data.PlayerName}|{pc.GetClient().GetHashedPuid()}|{reason}";
        //Cloud.SendData(msg);
        Logger.Fatal($"EAC报告：{msg}", "EAC Cloud");
        if (Options.CheatResponses.GetInt() != 5)
            Logger.SendInGame(string.Format(GetString("Message.NoticeByEAC"), $"{pc?.Data?.PlayerName} | {pc.GetClient().GetHashedPuid()}", reason));
    }
    public static bool ReceiveInvalidRpc(PlayerControl pc, byte callId)
    {
        if (Options.CheatResponses.GetInt() == 5)
        {
            Logger.Info("Cancel action bcz cheat response is cancel.", "MalumMenu on top!");
            return false;
        }
        switch (callId)
        {
            case unchecked((byte)42069):
                Report(pc, "AUM");
                HandleCheat(pc, GetString("EAC.CheatDetected.EAC"));
                return true;
        }
        return true;
    }
    public static void HandleCheat(PlayerControl pc, string text)
    {
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
                    Utils.SendMessage(string.Format(GetString("Message.NoticeByEAC"), pc?.Data?.PlayerName, text), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), GetString("MessageFromEAC")));
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
