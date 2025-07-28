using AmongUs.GameOptions;
using UnityEngine;

namespace TOHE.Patches;

// Thanks TOU-R: https://github.com/eDonnes124/Town-Of-Us-R/blob/master/source/Patches/ShowHostMeetingPatch.cs

[HarmonyPatch]
public class SteamOptionsPatch
{
    // Gurge44 - https://github.com/Gurge44/EndlessHostRoles
    [HarmonyPatch(nameof(ToggleOption.Toggle))]
    [HarmonyPrefix]
    private static bool TogglePrefix(ToggleOption __instance)
    {
        if (ModGameOptionsMenu.OptionList.TryGetValue(__instance, out int index))
        {
            __instance.CheckMark.enabled = !__instance.CheckMark.enabled;
            OptionItem item = OptionItem.AllOptions[index];
            item.SetValue(__instance.GetBool() ? 1 : 0);
            __instance.OnValueChanged.Invoke(__instance);
            NotificationPopperPatch.AddSettingsChangeMessage(index, item, true);
            return false;
        }

        return true;
    }
}
