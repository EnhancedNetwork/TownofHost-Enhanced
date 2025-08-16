using System;
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
            if (reader.Tag == 3) // Tags.RemoveGame = 3
            {
                if (reader.ReadInt32() == 32)
                {
                    var extraBytes = "";
                    while (reader.Position < reader.Length)
                    {
                        extraBytes += reader.ReadByte().ToString();
                        extraBytes += " ";
                        if (extraBytes.Length > 32) break;
                    }
                    if (extraBytes == "") extraBytes = "None";
                    Logger.Info($"{client?.PlayerName} was disconnected on server-side. Junk Bytes: {extraBytes}", "ServerHandleMessagePatch");

                    this.ClientDisconnect(client);
                    return;
                }
            }
        }
        catch (Exception e)
        {
            Logger.Error($"Couldn't get disconnect info / Error: {e.Message}", "ServerHandleMessagePatch");
        }
    }
}
