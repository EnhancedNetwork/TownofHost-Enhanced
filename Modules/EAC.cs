using AmongUs.GameOptions;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using static TOHE.Translator;

namespace TOHE;

internal class EAC
{
    public static Dictionary<byte, int> MeetingTimes = new();
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
        if (pc == null || reader == null || pc.AmOwner) return false;
        if (pc.GetClient()?.PlatformData?.Platform is Platforms.Android or Platforms.IPhone or Platforms.Switch or Platforms.Playstation or Platforms.Xbox or Platforms.StandaloneMac) return false;
        try
        {
            MessageReader sr = MessageReader.Get(reader);
            var rpc = (RpcCalls)callId;
            switch (rpc)
            {
                case RpcCalls.SetName:
                    string name = sr.ReadString();
                    if (sr.BytesRemaining > 0 && sr.ReadBoolean()) return false;
                    if (
                        ((name.Contains("<size") || name.Contains("size>")) && name.Contains('?') && !name.Contains("color")) ||
                        name.Length > 160 ||
                        name.Count(f => f.Equals("\"\\n\"")) > 3 ||
                        name.Count(f => f.Equals("\n")) > 3 ||
                        name.Count(f => f.Equals("\r")) > 3 ||
                        name.Contains("░") ||
                        name.Contains("▄") ||
                        name.Contains("█") ||
                        name.Contains("▌") ||
                        name.Contains("▒") ||
                        name.Contains("习近平")
                        )
                    {
                        WarnHost();
                        Report(pc, "非法设置游戏名称");
                        Logger.Fatal($"非法修改玩家【{pc.GetClientId()}:{pc.GetRealName()}】的游戏名称，已驳回", "EAC");
                        return true;
                    }
                    break;
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
                    if (text.StartsWith("/")) return false;
                    if (
                        text.Contains("░") ||
                        text.Contains("▄") ||
                        text.Contains("█") ||
                        text.Contains("▌") ||
                        text.Contains("▒") ||
                        text.Contains("习近平")
                        )
                    {
                        Report(pc, "非法消息");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】发送非法消息，已驳回", "EAC");
                        return true;
                    }
                    break;
                case RpcCalls.StartMeeting:
                        WarnHost();
                        Report(pc, "非法召集会议");
                        TempBanCheat(pc, "非法召集会议"); //Client doesnt use start meeting to call meeting
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法召集会议：【null】，已驳回", "EAC");
                        return true;
                case RpcCalls.SetColor:
                case RpcCalls.CheckColor:
                    var color = sr.ReadByte();
                    if (pc.Data.DefaultOutfit.ColorId != -1 &&
                        (Main.AllPlayerControls.Count(x => x.Data.DefaultOutfit.ColorId == color) >= 5
                        || !GameStates.IsLobby || color < 0 || color > 18))
                    {
                        WarnHost();
                        Report(pc, "非法设置颜色");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法设置颜色，已驳回", "EAC");
                        return true;
                    }
                    break;
                case RpcCalls.CheckMurder:
                case RpcCalls.MurderPlayer:
                    if (GameStates.IsLobby)
                    {
                        WarnHost();
                        Report(pc, "非法击杀");
                        TempBanCheat(pc, "非法报告尸体");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】大厅非法击杀，已驳回", "EAC");
                        return true;
                    }
                    else if (pc.Data.IsDead)
                    {
                        WarnHost();
                        Report(pc, "非法击杀");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法击杀，已驳回", "EAC");
                        return true;
                    }
                    break;
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
                    if (!GameStates.IsLobby)
                    {
                        WarnHost();
                        Report(pc, "非法设置颜色");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法设置颜色，已驳回", "EAC");
                        return true;
                    }
                    break;
                //case 11:
                //    MeetingTimes++;
                //    if ((GameStates.IsMeeting && MeetingTimes > 3) || GameStates.IsLobby)
                //    {
                //        WarnHost();
                //        Report(pc, "非法召集会议");
                //        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法召集会议：【null】，已驳回", "EAC");
                //        return true;
                //    }
                //    break;
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
                        TempBanCheat(pc, "非法击杀");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法击杀，已驳回", "EAC");
                        return true;
                    }
                    break;
                case 41:
                    if (GameStates.IsInGame)
                    {
                        WarnHost();
                        Report(pc, "非法设置宠物");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法设置宠物，已驳回", "EAC");
                        return true;
                    }
                    break;
                case 40:
                    if (GameStates.IsInGame)
                    {
                        WarnHost();
                        Report(pc, "非法设置皮肤");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法设置皮肤，已驳回", "EAC");
                        return true;
                    }
                    break;
                case 42:
                    if (GameStates.IsInGame)
                    {
                        WarnHost();
                        Report(pc, "非法设置面部装扮");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法设置面部装扮，已驳回", "EAC");
                        return true;
                    }
                    break;
                case 39:
                    if (GameStates.IsInGame)
                    {
                        WarnHost();
                        Report(pc, "非法设置帽子");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法设置帽子，已驳回", "EAC");
                        return true;
                    }
                    break;
                case 43:
                    if (sr.BytesRemaining > 0 && sr.ReadBoolean()) return false;
                    if (GameStates.IsInGame)
                    {
                        WarnHost();
                        Report(pc, "非法设置游戏名称");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法设置名称，已驳回", "EAC");
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

    public static Dictionary<byte, SystemTypes> SabotageWhiteList = new();
    public static bool SabotageSystemCheck(PlayerControl player, SystemTypes systemType, byte amount)
    {
        Logger.Info("Sabotage" + ", PlayerName: " + player.GetNameWithRole() + ", SabotageType: " + systemType.ToString() + ", amount: " + amount.ToString(), "EAC");
        if (player.AmOwner || !AmongUsClient.Instance.AmHost) return false;
        if (player.IsModClient()) return false;
        if (systemType == SystemTypes.Sabotage) //Vanilla sabotage using buttons
        {
            if (player.GetCustomRole().GetVNRole() != CustomRoles.Impostor && player.GetCustomRole().GetVNRole() != CustomRoles.Shapeshifter
                && player.GetCustomRole().GetDYRole() != RoleTypes.Impostor && player.GetCustomRole().GetDYRole() != RoleTypes.Shapeshifter)
            {
                if (Main.ErasedRoleStorage.ContainsKey(player.PlayerId))
                {
                    var originRole = Main.ErasedRoleStorage[player.PlayerId];
                    if (originRole.GetVNRole() != CustomRoles.Impostor && originRole.GetVNRole() != CustomRoles.Shapeshifter
                        && originRole.GetDYRole() != RoleTypes.Impostor && originRole.GetDYRole() != RoleTypes.Shapeshifter)
                    {
                        WarnHost();
                        Report(player, "Bad Sabotage A");
                        TempBanCheat(player, "Bad Sabotage A");
                        Logger.Fatal($"玩家【{player.GetClientId()}:{player.GetRealName()}】Bad Sabotage A，已驳回", "EAC");
                        return true;
                    }
                }
                else
                {
                    WarnHost();
                    Report(player, "Bad Sabotage B");
                    TempBanCheat(player, "Bad Sabotage B");
                    Logger.Fatal($"玩家【{player.GetClientId()}:{player.GetRealName()}】Bad Sabotage B，已驳回", "EAC");
                    return true;
                }
            }
        } //Cheat directly send 128 systemtype rpc
        /*//else if (amount == 8)
        //{
        //    if (SabotageWhiteList.ContainsKey(player.PlayerId))
        //    {
        //        if (SabotageWhiteList[player.PlayerId] == systemType)
        //        {
        //            return false;
        //        } //Remove the list upon 128 check
        //    } //If the sabotage is checked by host
        //    else
        //    {
        //        WarnHost();
        //        Report(player, "Bad Sabotage E");
        //        //TempBanCheat(player, "Bad Sabotage E");
        //        Logger.Fatal($"玩家【{player.GetClientId()}:{player.GetRealName()}】Bad Sabotage E，已驳回", "EAC");
        //        if (SabotageWhiteList.ContainsKey(player.PlayerId))
        //            SabotageWhiteList.Remove(player.PlayerId);
        //        return true;
        //    }
        //} //Check in sabotage system update sabotage bruhhh/ Cant read byte there
        */
        //Following is amount check
        else if (systemType == SystemTypes.LifeSupp)
        {
            if (Main.NormalOptions.MapId != 0 && Main.NormalOptions.MapId != 3) goto YesCheat;
            else if (amount >= 100)
            {
                if (SabotageWhiteList.ContainsKey(player.PlayerId))
                {
                    if (SabotageWhiteList[player.PlayerId] == systemType)
                    {
                        SabotageWhiteList.Remove(player.PlayerId);
                        return false;
                    }
                } //If the sabotage is checked by host
                else goto YesCheat;
            }
            else if (amount != 64 && amount != 65) goto YesCheat;
        }
        else if (systemType == SystemTypes.Comms)
        {
            if (amount == 0)
            {
                if (Main.NormalOptions.MapId == 1 || Main.NormalOptions.MapId == 5) goto YesCheat;
            }
            else if (amount > 100)
            {
                if (SabotageWhiteList.ContainsKey(player.PlayerId))
                {
                    if (SabotageWhiteList[player.PlayerId] == systemType)
                    {
                        SabotageWhiteList.Remove(player.PlayerId);
                        return false;
                    }
                } //If the sabotage is checked by host
                else goto YesCheat;
            }
            else if (amount == 64 || amount == 65 || amount == 32 || amount == 33 || amount == 16 || amount == 17)
            {
                if (!(Main.NormalOptions.MapId == 1 || Main.NormalOptions.MapId == 5)) goto YesCheat;
            }
            else goto YesCheat;
        }
        else if (systemType == SystemTypes.Electrical)
        {
            if (Main.NormalOptions.MapId == 5) goto YesCheat;
            if (amount > 10) //0 - 4 normal lights, > 50 sabotage
            {
                if (SabotageWhiteList.ContainsKey(player.PlayerId))
                {
                    if (SabotageWhiteList[player.PlayerId] == systemType)
                    {
                        SabotageWhiteList.Remove(player.PlayerId);
                        return false;
                    }
                } //If the sabotage is checked by host
                else goto YesCheat;
            }
        }
        else if (systemType == SystemTypes.Laboratory)
        {
            if (Main.NormalOptions.MapId != 2) goto YesCheat;
            if (amount > 100)
            {
                if (SabotageWhiteList.ContainsKey(player.PlayerId))
                {
                    if (SabotageWhiteList[player.PlayerId] == systemType)
                    {
                        SabotageWhiteList.Remove(player.PlayerId);
                        return false;
                    }
                } //If the sabotage is checked by host
                else goto YesCheat;
            }
            else if (!(amount == 64 || amount == 65 || amount == 32 || amount == 33)) goto YesCheat;
        }
        else if (systemType == SystemTypes.Reactor)
        {
            if (Main.NormalOptions.MapId == 2 || Main.NormalOptions.MapId == 4) goto YesCheat;
            if (amount > 100)
            {
                if (SabotageWhiteList.ContainsKey(player.PlayerId))
                {
                    if (SabotageWhiteList[player.PlayerId] == systemType)
                    {
                        SabotageWhiteList.Remove(player.PlayerId);
                        return false;
                    }
                } //If the sabotage is checked by host
                else goto YesCheat;
            }
            else if (!(amount == 64 || amount == 65 || amount == 32 || amount == 33)) goto YesCheat;
            //Airship use heli sabotage /Other use 64,65 | 32,33
        }
        else if (systemType == SystemTypes.HeliSabotage)
        {
            if (Main.NormalOptions.MapId != 4) goto YesCheat;
            if (amount > 100)
            {
                if (SabotageWhiteList.ContainsKey(player.PlayerId))
                {
                    if (SabotageWhiteList[player.PlayerId] == systemType)
                    {
                        SabotageWhiteList.Remove(player.PlayerId);
                        return false;
                    }
                } //If the sabotage is checked by host
                else goto YesCheat;
            }
            else if (!(amount == 64 || amount == 65 || amount == 16 || amount == 17 || amount == 32 || amount == 33)) goto YesCheat;
        }
        else if (systemType == SystemTypes.MushroomMixupSabotage)
        {
            if (Main.NormalOptions.MapId != 5) goto YesCheat;
            if (amount > 5) goto YesCheat; //Only 0 1 here
        }
        
        if (!GameStates.IsInGame || GameStates.IsMeeting)
        {
            WarnHost();
            Report(player, "Bad Sabotage D");
            if ((GameStates.IsVoting || GameStates.IsLobby))
                TempBanCheat(player, "Bad Sabotage D");
            Logger.Fatal($"玩家【{player.GetClientId()}:{player.GetRealName()}】Bad Sabotage D，已驳回", "EAC");
            return true;
        }

        if (SabotageWhiteList.ContainsKey(player.PlayerId))
            SabotageWhiteList.Remove(player.PlayerId);

        return false;

    YesCheat:
        {
            WarnHost();
            Report(player, "Bad Sabotage C");
            TempBanCheat(player, "Bad Sabotage C");
            Logger.Fatal($"玩家【{player.GetClientId()}:{player.GetRealName()}】Bad Sabotage C，已驳回", "EAC");
            if (SabotageWhiteList.ContainsKey(player.PlayerId))
                SabotageWhiteList.Remove(player.PlayerId);
            return true;
        }
    }
    public static void Report(PlayerControl pc, string reason)
    {
        string msg = $"{pc.GetClientId()}|{pc.FriendCode}|{pc.Data.PlayerName}|{pc.GetClient().GetHashedPuid()}|{reason}";
        //Cloud.SendData(msg);
        Logger.SendInGame($"[EAC] Cheat Report {pc.GetRealName()} for {reason}");
        Logger.Fatal($"EAC报告：{pc.GetRealName()}: {reason}", "EAC Cloud");
    }
    public static bool ReceiveInvalidRpc(PlayerControl pc, byte callId)
    {
        switch (callId)
        {
            case unchecked((byte)42069):
                Report(pc, "AUM");
                HandleCheat(pc, GetString("EAC.CheatDetected.EAC"));
                return true;
        }
        return true;
    }
    public static void TempBanCheat(PlayerControl pc, string reason)
    {
        string msg = $"{pc.GetClientId()}|{pc.FriendCode}|{pc.Data.PlayerName}|{pc.GetClient().GetHashedPuid()}|{reason}";

        BanManager.TempBanWhiteList.Add(pc.GetClient().GetHashedPuid());
        AmongUsClient.Instance.KickPlayer(pc.GetClientId(), true);
        Logger.Warn(msg, "EAC");
        //Logger.SendInGame(msg);
    }
    public static void HandleCheat(PlayerControl pc, string text)
    {
        switch (Options.CheatResponses.GetInt())
        {
            case 0:
                AmongUsClient.Instance.KickPlayer(pc.GetClientId(), true);
                string msg0 = string.Format(GetString("Message.BanedByEAC"), pc?.Data?.PlayerName, text);
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
        }
    }
}