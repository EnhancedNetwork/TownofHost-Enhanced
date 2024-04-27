using Hazel;

namespace TOHE;

public static class WriterHelper
{
    /// <summary>
    /// Starts a rpc using a integer value.
    /// </summary>
    /// <param name="targetNetId">The target InnerNetObject that the rpc will be executed on</param>
    /// <param name="callId">The call that will be called in the InnerNetObject</param>
    /// <param name="option">The send option of the packet, Reliable packets are bigger</param>
    /// <returns>A new MessageWriter</returns>
    public static MessageWriter StartRpc(this AmongUsClient client, uint targetNetId, int callId, SendOption option = SendOption.Reliable)
    {
        MessageWriter messageWriter = client.Streams[(int)option];
        messageWriter.StartMessage(2);
        messageWriter.WritePacked(targetNetId);
        messageWriter.Write(callId);
        return messageWriter;
    }
}
