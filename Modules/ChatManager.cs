using AmongUs.QuickChat;
using Hazel;
using System;
using System.Security.Cryptography;
using System.Text;
using TOHE.Roles.Impostor;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Modules.ChatManager
{
    public class ChatManager
    {
        public static bool cancel = false;
        private static readonly List<Dictionary<byte, string>> chatHistory = [];
        private static readonly Dictionary<byte, string> LastSystemChatMsg = [];
        private const int maxHistorySize = 20;
        public static List<string> ChatSentBySystem = [];
        public static QuickChatSpamMode quickChatSpamMode => (QuickChatSpamMode)UseQuickChatSpamCheat.GetInt();
        public static void ResetHistory()
        {
            chatHistory.Clear();
            LastSystemChatMsg.Clear();
        }
        public static void ClearLastSysMsg()
        {
            LastSystemChatMsg.Clear();
        }
        public static void AddSystemChatHistory(byte playerId, string msg)
        {
            LastSystemChatMsg[playerId] = msg;
        }
        public static bool CheckCommond(ref string msg, string command, bool exact = true)
        {
            var comList = command.Split('|');
            foreach (string comm in comList)
            {
                if (exact)
                {
                    if (msg == "/" + comm) return true;
                }
                else
                {
                    if (msg.StartsWith("/" + comm))
                    {
                        msg = msg.Replace("/" + comm, string.Empty);
                        return true;
                    }
                }
            }
            return false;
        }
        public static bool CheckName(ref string msg, string command, bool exact = true)
        {
            var comList = command.Split('|');
            foreach (var com in comList)
            {
                if (exact)
                {
                    if (msg.Contains(com))
                    {
                        return true;
                    }
                }
                else
                {
                    int index = msg.IndexOf(com);
                    if (index != -1)
                    {
                        msg = msg.Remove(index, com.Length);
                        return true;
                    }
                }
            }
            return false;
        }

        private static string GetTextHash(string text)
        {
            using SHA256 sha256 = SHA256.Create();

            // get sha-256 hash
            byte[] sha256Bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
            string sha256Hash = BitConverter.ToString(sha256Bytes).Replace("-", "").ToLower();

            // pick front 5 and last 4
            return string.Concat(sha256Hash.AsSpan(0, 5), sha256Hash.AsSpan(sha256Hash.Length - 4));
        }

        public static void AddToHostMessage(string text)
        {
            if (text != "")
            {
                ChatSentBySystem.Add(GetTextHash(text));
            }
        }
        public static void SendMessage(PlayerControl player, string message)
        {
            int operate = 0; // 1:ID 2:猜测
            string msg = message;
            string playername = player.GetNameWithRole();
            message = message.ToLower().TrimStart().TrimEnd();

            if (GameStates.IsInGame) operate = 3;
            if (CheckCommond(ref msg, "id|guesslist|gl编号|玩家编号|玩家id|id列表|玩家列表|列表|所有id|全部id|編號|玩家編號")) operate = 1;
            else if (CheckCommond(ref msg, "shoot|guess|bet|st|gs|bt|猜|赌|賭|sp|jj|tl|trial|审判|判|审|審判|審|compare|cmp|比较|比較|duel|sw|swap|st|换票|换|換票|換|finish|结束|结束会议|結束|結束會議|reveal|展示|rt|rit|ritual|bloodritual", false)) operate = 2;
            else if (ChatSentBySystem.Contains(GetTextHash(msg))) operate = 5;

            if ((operate == 1 || Blackmailer.CheckBlackmaile(player)) && player.IsAlive())
            {
                Logger.Info($"包含特殊信息，不记录", "ChatManager");
                message = msg;
                cancel = true;
            }
            else if (operate == 2)
            {
                Logger.Info($"指令{msg}，不记录", "ChatManager");
                message = msg;
                cancel = false;
            }
            else if (operate == 4)
            {
                Logger.Info($"指令{msg}，不记录", "ChatManager");
                message = msg;
                SendPreviousMessagesToAll();
            }
            else if (operate == 5)
            {
                Logger.Info($"system message{msg}，不记录", "ChatManager");
                message = msg;
                cancel = true;
            }
            else if (operate == 3)
            {
                if (GameStates.IsExilling)
                {
                    if (Options.HideExileChat.GetBool())
                    {
                        Logger.Info($"Message sent in exiling screen, spamming the chat", "ChatManager");
                        _ = new LateTask(SendPreviousMessagesToAll, 0.3f, "Spamming the chat");
                    }
                    return;
                }
                if (!player.IsAlive()) return;
                message = msg;
                //Logger.Warn($"Logging msg : {message}","Checking Exile");
                Dictionary<byte, string> newChatEntry = new()
                    {
                        { player.PlayerId, message }
                    };
                chatHistory.Add(newChatEntry);

                if (chatHistory.Count > maxHistorySize)
                {
                    chatHistory.RemoveAt(0);
                }
                cancel = false;
            }
        }

        public static void SendQuickChatSpam()
        {
            var firstAlivePlayer = Main.AllAlivePlayerControls.OrderBy(x => x.PlayerId).FirstOrDefault() ?? PlayerControl.LocalPlayer;
            var title = "<color=#aaaaff>" + GetString("DefaultSystemMessageTitle") + "</color>";
            var name = firstAlivePlayer?.Data?.PlayerName ?? "Error";

            var writer = CustomRpcSender.Create("EzHacked_QuickChatSpamExploit", ExtendedPlayerControl.RpcSendOption);
            writer.AutoStartRpc(firstAlivePlayer.NetId, (byte)RpcCalls.SetName);
            writer.Write(firstAlivePlayer.Data.NetId);
            writer.Write(title);
            writer.EndRpc();

            firstAlivePlayer.Data.PlayerName = title;

            switch (quickChatSpamMode)
            {
                case QuickChatSpamMode.QuickChatSpam_Disabled:
                    Logger.Info("QuickChatSpam disabled but trying to spam?", "SendQuickChatSpam");
                    goto case QuickChatSpamMode.QuickChatSpam_Random20;
                // Send as random 20 here
                case QuickChatSpamMode.QuickChatSpam_Random20:
                    var random = IRandom.Instance;
                    var stringNamesValues = Enum.GetValues(typeof(StringNames)).Cast<StringNames>().ToArray();
                    for (int i = 0; i < 25; i++)
                    {
                        var randomString = stringNamesValues[random.Next(stringNamesValues.Length)];
                        writer.AutoStartRpc(firstAlivePlayer.NetId, (byte)RpcCalls.SendQuickChat);
                        writer.Write((byte)QuickChatPhraseType.ComplexPhrase);
                        writer.Write((uint)randomString);
                        writer.Write((byte)0);
                        writer.EndRpc();
                        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(randomString), false);
                    }
                    break;
                case QuickChatSpamMode.QuickChatSpam_How2PlayNormal:
                    foreach (var names in Main.how2playN)
                    {
                        writer.AutoStartRpc(firstAlivePlayer.NetId, (byte)RpcCalls.SendQuickChat);
                        writer.Write((byte)QuickChatPhraseType.SimplePhrase);
                        writer.Write((uint)names);
                        writer.EndRpc();
                        writer.AutoStartRpc(firstAlivePlayer.NetId, (byte)RpcCalls.SendQuickChat);
                        writer.Write((byte)QuickChatPhraseType.SimplePhrase);
                        writer.Write((uint)names);
                        writer.EndRpc();
                        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(names), false);
                        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(names), false);
                    }
                    break;
                case QuickChatSpamMode.QuickChatSpam_How2PlayHidenSeek:
                    foreach (var names in Main.how2playHnS)
                    {
                        writer.AutoStartRpc(firstAlivePlayer.NetId, (byte)RpcCalls.SendQuickChat);
                        writer.Write((byte)QuickChatPhraseType.SimplePhrase);
                        writer.Write((uint)names);
                        writer.EndRpc();
                        writer.AutoStartRpc(firstAlivePlayer.NetId, (byte)RpcCalls.SendQuickChat);
                        writer.Write((byte)QuickChatPhraseType.SimplePhrase);
                        writer.Write((uint)names);
                        writer.EndRpc();
                        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(names), false);
                        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(names), false);
                    }
                    break;
                case QuickChatSpamMode.QuickChatSpam_EzHacked:
                    foreach (var names in Main.how2playEzHacked)
                    {
                        writer.AutoStartRpc(firstAlivePlayer.NetId, (byte)RpcCalls.SendQuickChat);
                        writer.Write((byte)QuickChatPhraseType.SimplePhrase);
                        writer.Write((uint)names);
                        writer.EndRpc();
                        writer.AutoStartRpc(firstAlivePlayer.NetId, (byte)RpcCalls.SendQuickChat);
                        writer.Write((byte)QuickChatPhraseType.SimplePhrase);
                        writer.Write((uint)names);
                        writer.EndRpc();
                        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(names), false);
                        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(names), false);
                    }
                    break;
            }
            writer.AutoStartRpc(firstAlivePlayer.NetId, (byte)RpcCalls.SetName);
            writer.Write(firstAlivePlayer.Data.NetId);
            writer.Write(name);
            writer.EndRpc();

            firstAlivePlayer.Data.PlayerName = name;
            writer.SendMessage();
        }
        public static void SendPreviousMessagesToAll()
        {
            if (!AmongUsClient.Instance.AmHost || !GameStates.IsModHost) return;
            //This should never function for non host
            if (GameStates.IsExilling && chatHistory.Count < 20)
            {
                if (quickChatSpamMode != QuickChatSpamMode.QuickChatSpam_Disabled)
                {
                    SendQuickChatSpam();
                }
                else
                {
                    var firstAlivePlayer = Main.AllAlivePlayerControls.OrderBy(x => x.PlayerId).FirstOrDefault();
                    if (firstAlivePlayer == null) firstAlivePlayer = PlayerControl.LocalPlayer;

                    var title = "<color=#aaaaff>" + GetString("DefaultSystemMessageTitle") + "</color>";
                    var name = firstAlivePlayer?.Data?.PlayerName;
                    string spamMsg = GetString("ExileSpamMsg");

                    for (int i = 0; i < 20 - chatHistory.Count; i++)
                    {
                        int clientId = -1; //sendTo == byte.MaxValue ? -1 : Utils.GetPlayerById(sendTo).GetClientId();
                                           //if (clientId == -1)
                                           //{
                        firstAlivePlayer.SetName(title);
                        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, spamMsg);
                        firstAlivePlayer.SetName(name);
                        //}
                        var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
                        writer.StartMessage(clientId);
                        writer.StartRpc(firstAlivePlayer.NetId, (byte)RpcCalls.SetName)
                            .Write(firstAlivePlayer.Data.NetId)
                            .Write(title)
                            .EndRpc();
                        writer.StartRpc(firstAlivePlayer.NetId, (byte)RpcCalls.SendChat)
                            .Write(spamMsg)
                            .EndRpc();
                        writer.StartRpc(firstAlivePlayer.NetId, (byte)RpcCalls.SetName)
                            .Write(firstAlivePlayer.Data.NetId)
                            .Write(name)
                            .EndRpc();
                        writer.EndMessage();
                        writer.SendMessage();
                        //DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, spamMsg);
                        //var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);

                        //writer.StartMessage(-1);
                        //writer.StartRpc(firstAlivePlayer.NetId, (byte)RpcCalls.SendChat)
                        //    .Write(spamMsg)
                        //    .EndRpc()
                        //    .EndMessage()
                        //    .SendMessage();
                    }
                }
            }
            //var rd = IRandom.Instance;
            //CustomRoles[] roles = (CustomRoles[])Enum.GetValues(typeof(CustomRoles));
            //string[] specialTexts = new string[] { "bet", "bt", "guess", "gs", "shoot", "st", "赌", "猜", "审判", "tl", "判", "审", "trial" };
            //int numPlayers = Main.AllAlivePlayerControls.Count();
            //var allAlivePlayers = Main.AllAlivePlayerControls.ToArray();
            //int roleCount = roles.Length;

            //for (int i = chatHistory.Count; i < 30; i++)
            //{
            //    StringBuilder msgBuilder = new();
            //    msgBuilder.Append('/');
            //    if (rd.Next(1, 100) < 20)
            //    {
            //        msgBuilder.Append("id");
            //    }
            //    else
            //    {
            //        msgBuilder.Append(specialTexts[rd.Next(specialTexts.Length)]);
            //        msgBuilder.Append(rd.Next(1, 100) < 50 ? string.Empty : " ");
            //        msgBuilder.Append(rd.Next(15));
            //        msgBuilder.Append(rd.Next(1, 100) < 50 ? string.Empty : " ");
            //        CustomRoles role = roles[rd.Next(roleCount)];
            //        msgBuilder.Append(rd.Next(1, 100) < 50 ? string.Empty : " ");
            //        msgBuilder.Append(Utils.GetRoleName(role));
            //    }
            //    string msg = msgBuilder.ToString();

            //    var player = allAlivePlayers[rd.Next(numPlayers)];
            //    DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg);
            //    var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);

            //    writer.StartMessage(-1);
            //    writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
            //        .Write(msg)
            //        .EndRpc()
            //        .EndMessage()
            //        .SendMessage();
            //}

            for (int i = 0; i < chatHistory.Count; i++)
            {
                var entry = chatHistory[i];
                var senderId = entry.Keys.First();
                var senderMessage = entry[senderId];
                var senderPlayer = senderId.GetPlayer();
                if (senderPlayer == null) continue;

                var playerDead = !senderPlayer.IsAlive();
                if (playerDead)
                {
                    senderPlayer.Revive();
                }

                DestroyableSingleton<HudManager>.Instance.Chat.AddChat(senderPlayer, senderMessage);
                var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);

                writer.StartMessage(-1);
                writer.StartRpc(senderPlayer.NetId, (byte)RpcCalls.SendChat)
                    .Write(senderMessage)
                    .EndRpc()
                    .EndMessage()
                    .SendMessage();

                if (playerDead)
                {
                    senderPlayer.Die(DeathReason.Kill, true);
                }
            }
            foreach (var playerId in LastSystemChatMsg.Keys.ToArray())
            {
                var pc = playerId.GetPlayer();
                if (pc == null && playerId != byte.MaxValue) continue;
                var title = "<color=#FF0000>" + GetString("LastMessageReplay") + "</color>";
                Utils.SendMessage(LastSystemChatMsg[playerId], playerId, title: title, noReplay: true);
            }
        }
    }
}
