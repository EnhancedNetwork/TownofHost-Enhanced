using Hazel;
using System;
using System.Security.Cryptography;
using System.Text;
using TOHE.Modules.Rpc;
using TOHE.Patches;
using TOHE.Roles.Impostor;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Modules.ChatManager
{
    public class ChatManager
    {
        public static bool cancel = false;
        private static readonly List<(byte, string)> chatHistory = [];
        private static readonly Dictionary<byte, List<string>> LastSystemChatMsgs = [];
        private const int maxHistorySize = 20;
        public static List<string> ChatSentBySystem = [];
        public static QuickChatSpamMode quickChatSpamMode => (QuickChatSpamMode)UseQuickChatSpamCheat.GetInt();
        public static void ResetHistory()
        {
            chatHistory.Clear();
            LastSystemChatMsgs.Clear();
        }
        public static void ClearLastSysMsg()
        {
            LastSystemChatMsgs.Clear();
        }
        public static void AddSystemChatHistory(byte playerId, string msg)
        {
            if (!LastSystemChatMsgs.ContainsKey(playerId))
                LastSystemChatMsgs[playerId] = [];
            LastSystemChatMsgs[playerId].Add(msg);
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
            else if (CheckCommond(ref msg, "shoot|guess|bet|st|gs|bt|猜|赌|賭|sp|jj|tl|trial|审判|判|审|審判|審|compare|cmp|比较|比較|duel|sw|swap|st|换票|换|換票|換|finish|结束|结束会议|結束|結束會議|reveal|展示|rt|rit|ritual|bloodritual|summon|sm|db|daybreak", false)) operate = 2;
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
                    if (HideExileChat.GetBool())
                    {
                        Logger.Info($"Message sent in exiling screen, spamming the chat", "ChatManager");
                        _ = new LateTask(SendPreviousMessagesToAll, 0.3f, "Spamming the chat");
                    }
                    return;
                }
                if (!player.IsAlive()) return;
                message = msg;
                //Logger.Warn($"Logging msg : {message}","Checking Exile");
                var newChatEntry = (player.PlayerId, message);
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
            var message = new RpcQuickChatSpam();
            RpcUtils.LateBroadcastReliableMessage(message);
        }
        public static void SendPreviousMessagesToAll()
        {
            if (!AmongUsClient.Instance.AmHost || !GameStates.IsModHost || !HudManager.InstanceExists) return;
            //This should never function for non host
            Logger.Info(" Sending Previous Messages To Everyone", "ChatManager");

            PlayerControl[] aapc = Main.AllAlivePlayerControls;
            if (aapc.Length == 0) return;

            if (GameStates.IsVanillaServer)
            {
                ClearChat();

                StringBuilder sb = new();
                chatHistory.ForEach(x =>
                {
                    byte id = x.Item1;
                    string msg = x.Item2.Trim();
                    sb.Append(id.GetPlayerName());
                    sb.Append(':');
                    sb.Append(' ');
                    sb.AppendLine(msg);
                });
                LateTask.New(() => Utils.SendMessage("\n", title: sb.ToString().Trim()), 0.2f);
                
                return;
            }


            string[] filtered = [.. chatHistory.Where(a => Utils.GetPlayerById(a.Item1).IsAlive()).Select(b => b.Item2)];
            ChatController chat = HudManager.Instance.Chat;
            var writer = CustomRpcSender.Create("SendPreviousMessagesToAll", SendOption.Reliable);
            var hasValue = false;

            if (filtered.Length < 20) ClearChat(aapc);

            foreach (string str in filtered)
            {
                string[] entryParts = str.Split(':');
                string senderId = entryParts[0].Trim();
                string senderMessage = entryParts[1].Trim();
                for (var j = 2; j < entryParts.Length; j++) senderMessage += ':' + entryParts[j].Trim();

                PlayerControl senderPlayer = Utils.GetPlayerById(Convert.ToByte(senderId));
                if (senderPlayer == null) continue;

                chat.AddChat(senderPlayer, senderMessage);
                SendRPC(writer, senderPlayer, senderMessage);
                hasValue = true;

                if (writer.stream.Length > 500)
                {
                    writer.SendMessage();
                    writer = CustomRpcSender.Create("SendPreviousMessagesToAll", SendOption.Reliable);
                    hasValue = false;
                }
            }

            hasValue |= ChatUpdatePatch.SendLastMessages(ref writer);
            writer.SendMessage(!hasValue);
        }

        private static void SendRPC(CustomRpcSender writer, PlayerControl senderPlayer, string senderMessage, int targetClientId = -1)
        {
            if (GameStates.IsLobby && senderPlayer.AmOwner)
                senderMessage = senderMessage.Insert(0, new('\n', PlayerControl.LocalPlayer.name.Count(x => x == '\n')));

            writer.AutoStartRpc(senderPlayer.NetId, RpcCalls.SendChat, targetClientId)
                .Write(senderMessage)
                .EndRpc();
        }

        // Base from https://github.com/Rabek009/MoreGamemodes/blob/master/Modules/Utils.cs
        public static void ClearChat(params PlayerControl[] targets)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            PlayerControl player = GameStates.IsVanillaServer ? PlayerControl.LocalPlayer : Main.AllAlivePlayerControls.MinBy(x => x.PlayerId) ?? Main.AllPlayerControls.MinBy(x => x.PlayerId) ?? PlayerControl.LocalPlayer;
            if (player == null) return;

            if (GameStates.IsVanillaServer)
            {
                if (targets.Length <= 1 || targets.Length >= Main.AllAlivePlayerControls.Length)
                    Loop.Times(30, _ => Utils.SendMessage(string.Empty, targets.Length == 1 ? targets[0].PlayerId : byte.MaxValue, "\u200b", force: true, sendOption: SendOption.None));
                else
                    targets.Do(x => Loop.Times(30, _ => Utils.SendMessage(string.Empty, x.PlayerId, "\u200b", force: true, sendOption: SendOption.None)));
                
                return;
            }

            if (targets.Length == 0 || targets.Length == PlayerControl.AllPlayerControls.Count) SendEmptyMessage(null);
            else targets.Do(SendEmptyMessage);
            return;

            void SendEmptyMessage(PlayerControl receiver)
            {
                bool toEveryone = receiver == null;
                bool toLocalPlayer = !toEveryone && receiver.AmOwner;
                if (HudManager.InstanceExists && (toLocalPlayer || toEveryone)) HudManager.Instance.Chat.AddChat(player, "<size=32767>.");
                if (toLocalPlayer) return;
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.SendChat, SendOption.Reliable, toEveryone ? -1 : receiver.OwnerId);
                writer.Write("<size=32767>.");
                writer.Write(true);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
        }
    }
}
