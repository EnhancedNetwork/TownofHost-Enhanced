using HarmonyLib;

namespace TOHE.Patches;

[HarmonyPatch(typeof(Constants), nameof(Constants.GetBroadcastVersion))]
class ServerUpdatePatch
{
    static void Postfix(ref int __result)
    {
        if (GameStates.IsLocalGame)
        {
            Logger.Info($"IsLocalGame: {__result}", "VersionServer");
        }
        if (GameStates.IsOnlineGame)
        {
            // Changing server version for AU mods
            __result += 25;
            Logger.Info($"IsOnlineGame: {__result}", "VersionServer");
        }

    }
}