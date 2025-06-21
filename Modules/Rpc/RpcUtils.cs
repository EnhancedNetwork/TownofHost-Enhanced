using AmongUs.InnerNet.GameDataMessages;
using Hazel;
using InnerNet;
using System;

namespace TOHE.Modules.Rpc;

[HarmonyPatch]
public class RpcUtils
{
    // You need to register and cast shit into IGameDataMessage so the game will recoginze it and put it in quque
    public static void LateBroadcastReliableMessage(BaseGameDataMessage message)
    {
        try
        {
            AmongUsClient.Instance.LateBroadcastReliableMessage(message.CastFast<IGameDataMessage>());
        }
        catch (Exception error)
        {
            Logger.Error($"Error: {error}", "Rpc.Utils.LateBroadcastReliableMessage");
        }
    }

    public static void LateBroadcastUnReliableMessage(BaseGameDataMessage message)
    {
        try
        {
            AmongUsClient.Instance.LateBroadcastUnreliableMessage(message.CastFast<IGameDataMessage>());
        }
        catch (Exception error)
        {
            Logger.Error($"Error: {error}", "Rpc.Utils.LateBroadCastUnReliableMessage");
        }
    }

    public static void SendMessageImmediately(BaseGameDataMessage message, int clientId = -1, SendOption sendOption = SendOption.Reliable)
    {
        var writer = MessageWriter.Get(sendOption);

        if (clientId < 0)
        {
            writer.StartMessage(5);
            writer.Write(AmongUsClient.Instance.GameId);
        }
        else
        {
            writer.StartMessage(6);
            writer.Write(AmongUsClient.Instance.GameId);
            writer.WritePacked(clientId);
        }

        message.Serialize(writer);

        writer.EndMessage();
        AmongUsClient.Instance.SendOrDisconnect(writer);
        writer.Recycle();
    }

    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.PackAndSendQueuedMessages))]
    [HarmonyPostfix]
    public static void PackAndSendQueuedMessagesPostfix(InnerNetClient __instance, [HarmonyArgument(1)] SendOption sendOption)
    {
        PackAndSendSpecificMessage(sendOption);
    }

    public static Dictionary<int, List<BaseGameDataMessage>> queuedReliableMessage = [];
    public static Dictionary<int, List<BaseGameDataMessage>> queuedUnreliableMessage = [];

    public static void PackAndSendSpecificMessage(SendOption sendOption)
    {
        Dictionary<int, List<BaseGameDataMessage>> queuedMessage = sendOption == SendOption.Reliable ? queuedReliableMessage : queuedUnreliableMessage;

        IEnumerable<KeyValuePair<int, List<BaseGameDataMessage>>> topQueues;

        if (!Options.BypassRateLimitAC.GetBool())
        {
            topQueues = queuedMessage.Where(x => x.Value.Count > 0);
        }
        else
        {
            var limit = sendOption == SendOption.Reliable ? Options.MaxSpiltReliablePacketsPerTick.GetInt() : Options.MaxSpiltNonePacketsPerTick.GetInt();
            topQueues = queuedMessage
                .Where(x => x.Value.Count > 0)
                .OrderByDescending(x => x.Value.Count)
                .Take(limit);
        }

        foreach (var message in topQueues)
        {
            var writer = MessageWriter.Get(sendOption);

            writer.StartMessage(6);
            writer.Write(AmongUsClient.Instance.GameId);
            writer.WritePacked(message.Key);

            while (message.Value.Count > 0)
            {
                var msg = message.Value[0];
                message.Value.RemoveAt(0);
                msg.CastFast<IGameDataMessage>().Serialize(writer);

                if (writer.Length > 500)
                {
                    break;
                }
            }

            writer.EndMessage();
            AmongUsClient.Instance.SendOrDisconnect(writer);
            writer.Recycle();
        }
    }

    public static void LateSpecificSendMessage(BaseGameDataMessage message, int clientId = -1, SendOption sendOption = SendOption.Reliable)
    {
        if (sendOption == SendOption.Reliable)
        {
            if (!queuedReliableMessage.ContainsKey(clientId))
                queuedReliableMessage[clientId] = [];
            queuedReliableMessage[clientId].Add(message);
        }
        else
        {
            if (!queuedUnreliableMessage.ContainsKey(clientId))
                queuedUnreliableMessage[clientId] = [];
            queuedUnreliableMessage[clientId].Add(message);
        }
    }
}
