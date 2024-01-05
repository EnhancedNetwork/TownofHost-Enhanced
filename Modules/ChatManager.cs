using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TOHE.Roles.Impostor;
using static TOHE.Translator;

namespace TOHE.Modules.ChatManager
{
    public class ChatManager
    {
        public static bool cancel = false;
        private static List<Dictionary<byte, string>> chatHistory = new();
        private const int maxHistorySize = 20;
        public static List<string> ChatSentBySystem = new();
        public static void ResetHistory()
        {
            chatHistory = new();
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

        public static string getTextHash(string text)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // get sha-256 hash
                byte[] sha256Bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
                string sha256Hash = BitConverter.ToString(sha256Bytes).Replace("-", "").ToLower();

                // pick front 5 and last 4
                return string.Concat(sha256Hash.AsSpan(0, 5), sha256Hash.AsSpan(sha256Hash.Length - 4));
            }
        }

        public static void AddToHostMessage(string text)
        {
            if (text != "")
            {
                ChatSentBySystem.Add(getTextHash(text));
            }
        }
        public static void SendMessage(PlayerControl player, string message)
        {
            int operate = 0; // 1:ID 2:猜测
            string msg = message;
            string playername = player.GetNameWithRole();
            message = message.ToLower().TrimStart().TrimEnd();

            if (GameStates.IsInGame) operate = 3;
            if (CheckCommond(ref msg, "id|guesslist|gl编号|玩家编号|玩家id|id列表|玩家列表|列表|所有id|全部id")) operate = 1;
            else if (CheckCommond(ref msg, "shoot|guess|bet|st|gs|bt|猜|赌|sp|jj|tl|trial|审判|判|审|compare|cmp|比较|duel|sw|换票|换|swap|st|finish|reveal", false)) operate = 2;
            else if (ChatSentBySystem.Contains(getTextHash(msg))) operate = 5;
            
            if ((operate == 1 || Blackmailer.ForBlackmailer.Contains(player.PlayerId)) && player.IsAlive())
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
                        _ = new LateTask (SendPreviousMessagesToAll, 0.3f, "Spamming the chat");
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

        public static void SendPreviousMessagesToAll()
        {
            if (!AmongUsClient.Instance.AmHost || !GameStates.IsModHost) return;
            //This should never function for non host
            if (GameStates.IsExilling && chatHistory.Count < 20)
            {
                var firstAlivePlayer = Main.AllAlivePlayerControls.OrderBy(x => x.PlayerId).FirstOrDefault();
                if (firstAlivePlayer == null) return;

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
                        .Write(title)
                        .EndRpc();
                    writer.StartRpc(firstAlivePlayer.NetId, (byte)RpcCalls.SendChat)
                        .Write(spamMsg)
                        .EndRpc();
                    writer.StartRpc(firstAlivePlayer.NetId, (byte)RpcCalls.SetName)
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
                var senderPlayer = Utils.GetPlayerById(senderId);
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
        }
    }
}