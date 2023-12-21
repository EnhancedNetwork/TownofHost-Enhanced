using HarmonyLib;

namespace TOHE.Patches;

[HarmonyPatch(typeof(DeconSystem), nameof(DeconSystem.UpdateSystem))]
public static class DeconSystemUpdateSystemPatch
{
    public static void Prefix(DeconSystem __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        if (Options.ChangeDecontaminationTime.GetBool())
        {
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
        else
        {
            // Set to defalt
            __instance.DoorOpenTime = 3f;
            __instance.DeconTime = 3f;
        }
    }
}
