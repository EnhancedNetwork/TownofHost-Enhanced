using HarmonyLib;

namespace TOHE.Patches
{
    [HarmonyPatch(typeof(StatsManager), nameof(StatsManager.BanMinutesLeft), MethodType.Getter)]
    public static class DisconnectPenaltyPatch {
        public static bool Prefix(StatsManager __instance, ref int __result)
        {
            if (!DebugModeManager.AmDebugger)
            {
                return true;
            }
            if (__instance.BanPoints != 0f)
            {
                __instance.BanPoints = 0f;
                __result = 0;
                Logger.Info("Debug Removed Disconnect ban", "PenaltyPatch");
                return false;
            }
            return true;
        }
    }
}
//Code from https://github.com/scp222thj/MalumMenu/blob/main/src/Passive/PenaltyPatch.cs
