using HarmonyLib;

namespace TOHE.Patches;

[HarmonyPatch(typeof(DeconSystem), nameof(DeconSystem.Deteriorate))]
public static class DeconSystemDeterioratePatch
{
    public static void Prefix(DeconSystem __instance)
    {
        if (!Options.ChangeDecontaminationTime.GetBool()) return;

        if (Options.IsActiveMiraHQ)
        {
            __instance.DoorOpenTime = Options.DecontaminationTimeOnMiraHQ.GetFloat();
            __instance.DeconTime = Options.DecontaminationTimeOnMiraHQ.GetFloat();
        }
        else if (Options.IsActivePolus)
        {
            __instance.DoorOpenTime = Options.DecontaminationTimeOnPolus.GetFloat();
            __instance.DeconTime = Options.DecontaminationTimeOnPolus.GetFloat();
        }
    }
}
