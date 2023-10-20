using System.Collections.Generic;
using System.Linq;
using Hazel;
using System;
using TOHE.Roles.Impostor;
using System.Text;

namespace TOHE.Modules.ChatManager
{
    public class ChatManager
    {
        public static bool cancel = false;
        private static List<Dictionary<byte, string>> chatHistory = new();
        private const int maxHistorySize = 20;
        public static void resetHistory()
        {
            chatHistory = new();
        }
        public static bool CheckCommond(ref string msg, string command, bool exact = true)
        {
            var comList = command.Split('|');
            for (int i = 0; i < comList.Count(); i++)
            {
                if (exact)
                {
                    if (msg == "/" + comList[i]) return true;
                }
                else
                {
                    if (msg.StartsWith("/" + comList[i]))
                    {
                        msg = msg.Replace("/" + comList[i], string.Empty);
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

        public static void SendMessage(PlayerControl player, string message)
        {
            int operate = 0; // 1:ID 2:猜测
            string msg = message;
            string playername = player.GetNameWithRole();
            message = message.ToLower().TrimStart().TrimEnd();
            if (!player.IsAlive() || !AmongUsClient.Instance.AmHost) return;
            if (GameStates.IsInGame) operate = 3;
            if (CheckCommond(ref msg, "id|guesslist|gl编号|玩家编号|玩家id|id列表|玩家列表|列表|所有id|全部id")) operate = 1;
            else if (CheckCommond(ref msg, "shoot|guess|bet|st|gs|bt|猜|赌|sp|jj|tl|trial|审判|判|审|compare|cmp|比较|duel|sw|换票|换|swap|st|finish|reveal", false)) operate = 2;
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
            else if (operate == 3)
            {
                message = msg;
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