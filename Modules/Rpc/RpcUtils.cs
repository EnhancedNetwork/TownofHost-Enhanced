using AmongUs.InnerNet.GameDataMessages;
using Hazel;
using System;

namespace TOHE.Modules.Rpc;

public class RpcUtils
{
    public static void LateBroadcastReliableMessage(BaseGameDataMessage message)
    {
        try
        {
            AmongUsClient.Instance.LateBroadcastReliableMessage(message.Cast<IGameDataMessage>());
        }
        catch (Exception error)
        {
            Logger.Error($"Error: {error}", "Rpc.Utils.LateBroadcastReliableMessage");
        }
    }

    public static void LateBroadCastUnReliableMessage(BaseGameDataMessage message)
    {
        try
        {
            AmongUsClient.Instance.LateBroadcastUnreliableMessage(message.Cast<IGameDataMessage>());
        }
        catch (Exception error)
        {
            Logger.Error($"Error: {error}", "Rpc.Utils.LateBroadCastUnReliableMessage");
        }
    }

    public static void SendMessageSpecially(BaseGameDataMessage message, int clientId = -1, SendOption sendOption = SendOption.Reliable)
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
}
