using AmongUs.InnerNet.GameDataMessages;
using AmongUs.QuickChat;
using Hazel;
using System;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Modules.Rpc
{
    class RpcQuickChatSpam : CustomModdedData
    {
        public override GameDataTypes FirstDataType => GameDataTypes.RpcFlag;
        public static QuickChatSpamMode quickChatSpamMode => (QuickChatSpamMode)UseQuickChatSpamCheat.GetInt();

        public override void SerializeCustomValues(MessageWriter writer)
        {
            var firstAlivePlayer = Main.AllAlivePlayerControls.OrderBy(x => x.PlayerId).FirstOrDefault() ?? PlayerControl.LocalPlayer;
            var title = "<color=#aaaaff>" + GetString("DefaultSystemMessageTitle") + "</color>";
            var name = firstAlivePlayer?.Data?.PlayerName ?? "Error";

            firstAlivePlayer.Data.PlayerName = title;

            // writer.StartMessage((byte)GameDataTypes.RpcFlag);
            writer.WritePacked(firstAlivePlayer.NetId);
            writer.Write((byte)RpcCalls.SetName);
            writer.Write(firstAlivePlayer.Data.NetId);
            writer.Write(title);
            writer.EndMessage();

            switch (quickChatSpamMode)
            {
                case QuickChatSpamMode.QuickChatSpam_Disabled:
                    Logger.Info("QuickChatSpam disabled but trying to spam?", "SendQuickChatSpam");
                    goto case QuickChatSpamMode.QuickChatSpam_Random20;
                // Send as random 20 here
                case QuickChatSpamMode.QuickChatSpam_Random20:
                    var random = IRandom.Instance;
                    var stringNamesValues = Enum.GetValues(typeof(StringNames)).Cast<StringNames>().ToArray();
                    for (int i = 0; i < 21; i++)
                    {
                        var randomString = stringNamesValues[random.Next(stringNamesValues.Length)];
                        var message = new RpcSendQuickChatMessage(firstAlivePlayer.NetId, new(QuickChatPhraseType.ComplexPhrase, randomString, 0, null));
                        message.Serialize(writer);
                        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(randomString), false);
                    }
                    break;
                case QuickChatSpamMode.QuickChatSpam_How2PlayNormal:
                    foreach (var names in Main.how2playN)
                    {
                        var message = new RpcSendQuickChatMessage(firstAlivePlayer.NetId, new(QuickChatPhraseType.ComplexPhrase, names, 0, null));
                        message.Serialize(writer);
                        message.Serialize(writer);
                        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(names), false);
                        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(names), false);
                    }
                    break;
                case QuickChatSpamMode.QuickChatSpam_How2PlayHidenSeek:
                    foreach (var names in Main.how2playHnS)
                    {
                        var message = new RpcSendQuickChatMessage(firstAlivePlayer.NetId, new(QuickChatPhraseType.ComplexPhrase, names, 0, null));
                        message.Serialize(writer);
                        message.Serialize(writer);
                        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(names), false);
                        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(names), false);
                    }
                    break;
                case QuickChatSpamMode.QuickChatSpam_EzHacked:
                    foreach (var names in Main.how2playEzHacked)
                    {
                        var message = new RpcSendQuickChatMessage(firstAlivePlayer.NetId, new(QuickChatPhraseType.ComplexPhrase, names, 0, null));
                        message.Serialize(writer);
                        message.Serialize(writer);
                        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(names), false);
                        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(names), false);
                    }
                    break;
                case QuickChatSpamMode.QuickChatSpam_Empty:
                    {
                        var message = new RpcSendQuickChatMessage(firstAlivePlayer.NetId, new(QuickChatPhraseType.SimplePhrase, StringNames.None, 0, null));
                        for (var i = 0; i < 21; i++)
                        {
                            message.Serialize(writer);
                            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(StringNames.None), false);
                        }
                    }
                    break;
            }

            firstAlivePlayer.Data.PlayerName = name;
            writer.StartMessage((byte)GameDataTypes.RpcFlag);
            writer.WritePacked(firstAlivePlayer.NetId);
            writer.Write((byte)RpcCalls.SetName);
            writer.Write(firstAlivePlayer.Data.NetId);
            writer.Write(name);
            // writer.EndMessage();
        }
    }
}
