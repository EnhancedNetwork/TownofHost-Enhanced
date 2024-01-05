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
            if (GameStates.IsHideNSeek) return; // Will innersloth give permission for this?

            // Changing server version for AU mods
            //if (!Main.VersionCheat.Value)
            __result += 25;
            Logger.Info($"IsOnlineGame: {__result}", "VersionServer");
        }
    }
}
[HarmonyPatch(typeof(Constants), nameof(Constants.IsVersionModded))]
public static class IsVersionModdedPatch
{
    public static bool Prefix(ref bool __result)
    {
        __result = true;
        return false;
    }
}