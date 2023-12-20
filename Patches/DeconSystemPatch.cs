using HarmonyLib;

namespace TOHE.Patches;

[HarmonyPatch(typeof(DeconSystem), nameof(DeconSystem.Deteriorate))]
public static class DeconSystemDeterioratePatch
{
    public static void Prefix(DeconSystem __instance)
    {
        if (!Options.ChangeDecontaminationTime.GetBool()) return;

        float deconTime;

        if (Options.IsActiveMiraHQ)
        {
            // Temp decon time MiraHQ
            deconTime = Options.DecontaminationTimeOnMiraHQ.GetFloat();

            // Set same value for "DeconTime" and "DoorOpenTime"
            __instance.DoorOpenTime = deconTime;
            __instance.DeconTime = deconTime;
        }
        else if (Options.IsActivePolus)
        {
            // Temp decon time Polus
            deconTime = Options.DecontaminationTimeOnPolus.GetFloat();

            // Set same value for "DeconTime" and "DoorOpenTime"
            __instance.DoorOpenTime = deconTime;
            __instance.DeconTime = deconTime;
        }
    }
}
