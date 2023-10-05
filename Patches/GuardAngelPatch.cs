using HarmonyLib;

namespace TOHE;
internal class GuardAngelPatch
{
    [HarmonyPatch(typeof(MeetingIntroAnimation), nameof(MeetingIntroAnimation.Start))]
    public static class ProtectedRecentlyPatch
    {
        public static bool Prefix(MeetingIntroAnimation __instance)
        {
            __instance.ProtectedRecently.active = false;
            __instance.ProtectedRecently.transform.localPosition = new UnityEngine.Vector3(100f, 100f, 100f);
            __instance.ProtectedRecentlySound = new();
            return true;
        }
    }
}
