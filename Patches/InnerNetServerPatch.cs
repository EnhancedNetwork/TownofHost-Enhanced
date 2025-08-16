using Hazel;
using InnerNet;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(InnerNetServer), nameof(InnerNetServer.HandleMessage))]
class ServerHandleMessagePatch
{
    public static void Prefix(InnerNetServer __instance, InnerNetServer.Player client, MessageReader reader, SendOption sendOption)
    {
        try
        {
            MessageReader copyReader = reader.Duplicate();
            if (copyReader.Tag == 3) // Tags.RemoveGame = 3
            {
                if (copyReader.ReadInt32() == 32)
                {
                    var extraBytes = "";
                    while (copyReader.Position < copyReader.Length)
                    {
                        extraBytes += copyReader.ReadByte().ToString();
                        extraBytes += " ";
                        if (extraBytes.Length > 32) break;
                    }
                    if (extraBytes == "") extraBytes = "None";
                    Logger.Debug($"{client?.PlayerName} was disconnected on server-side. Junk Bytes: {extraBytes}", "ServerHandleMessagePatch");
                }
            }
        }
        catch (Exception e)
        {
            Logger.Error($"Couldn't get disconnect info / Error: {e}", "ServerHandleMessagePatch");
        }
    }
}
