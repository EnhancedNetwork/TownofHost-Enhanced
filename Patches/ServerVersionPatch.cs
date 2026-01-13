namespace TOHE.Patches;

[HarmonyPatch(typeof(Constants), nameof(Constants.GetBroadcastVersion))]
class ServerUpdatePatch
{
    static void Postfix(ref int __result)
    {
        
        if (GameStates.IsLocalGame)
        {
            var (Year, Month, Day, Revision) = GetVersionComponents(__result);
            string version = $"{Month}/{Day}/{Year} rev {Revision}, {__result}";
            Logger.Info($"IsLocalGame: {version}", "VersionServer");
        }
        if (GameStates.IsOnlineGame)
        {
            // Changing server version for AU mods

            //if (!Main.VersionCheat.Value)
            __result += 25;
            var (Year, Month, Day, Revision) = GetVersionComponents(__result);
            string version = $"{Month}/{Day}/{Year} rev {Revision}, {__result}";
            Logger.Info($"IsOnlineGame: {version}", "VersionServer");
        }
    }

    internal static (int Year, int Month, int Day, int Revision) GetVersionComponents(int broadcastVersion)
	{
		int num = broadcastVersion / 25000;
		broadcastVersion -= num * 25000;
		int num2 = broadcastVersion / 1800;
		broadcastVersion -= num2 * 1800;
		int num3 = broadcastVersion / 50;
		broadcastVersion -= num3 * 50;
		int num4 = broadcastVersion;
		return (num, num2, num3, num4);
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
