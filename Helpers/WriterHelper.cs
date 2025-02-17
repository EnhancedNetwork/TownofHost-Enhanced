//using Hazel;

//namespace TOHE;

//public static class WriterHelper // Currently not in use, because all rpc are based on byte. May be in use for a new enum perhaps...
//{
//    /// <summary>
//    /// Starts a rpc using a integer value.
//    /// </summary>
//    /// <param name="targetNetId">The target InnerNetObject that the rpc will be executed on</param>
//    /// <param name="callId">The call that will be called in the InnerNetObject</param>
//    /// <param name="option">The send option of the packet, Reliable packets are bigger</param>
//    /// <returns>A new MessageWriter</returns>
//    private static MessageWriter StartRpc(this AmongUsClient client, uint targetNetId, int callId, SendOption option = SendOption.Reliable)
//    {
//        MessageWriter messageWriter = client.Streams[(int)option];
//        messageWriter.StartMessage(2);
//        messageWriter.WritePacked(targetNetId);
//        messageWriter.Write(callId);
//        return messageWriter;
//    }

//    /// <summary>
//    /// Starts a rpc immediately using a integer value.
//    /// </summary>
//    /// <param name="targetNetId">The target InnerNetObject that the rpc will be executed on</param>
//    /// <param name="callId">The call that will be called in the InnerNetObject</param>
//    /// <param name="option">The send option of the packet, Reliable packets are bigger</param>
//    /// <param name="targetClientId">The target client id that the message will be streamed to</param>
//    /// <returns>A new MessageWriter</returns>
//    private static MessageWriter StartRpcImmediately(this AmongUsClient client, uint targetNetId, int callId, SendOption option, int targetClientId = -1)
//    {
//        MessageWriter messageWriter = MessageWriter.Get(option);
//        if (targetClientId < 0)
//        {
//            messageWriter.StartMessage(5);
//            messageWriter.Write(client.GameId);
//        }
//        else
//        {
//            messageWriter.StartMessage(6);
//            messageWriter.Write(client.GameId);
//            messageWriter.WritePacked(targetClientId);
//        }
//        messageWriter.StartMessage(2);
//        messageWriter.WritePacked(targetNetId);
//        messageWriter.Write(callId);
//        return messageWriter;
//    }
//}
