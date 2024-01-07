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
            // Temp decon time
            var deconTime = Utils.GetActiveMapName() switch
            {
                MapNames.Mira => Options.DecontaminationTimeOnMiraHQ.GetFloat(),
                MapNames.Polus => Options.DecontaminationTimeOnPolus.GetFloat(),
                _ => 3f,
            };

            // Set same value for "DeconTime" and "DoorOpenTime"
            __instance.DoorOpenTime = deconTime;
            __instance.DeconTime = deconTime;
        }
        else
        {
            // Set to defalt
            __instance.DoorOpenTime = 3f;
            __instance.DeconTime = 3f;
        }
    }
}
