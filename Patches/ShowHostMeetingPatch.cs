using TMPro;
using UnityEngine;

namespace TOHE.Patches;

// Thanks TOU-R: https://github.com/eDonnes124/Town-Of-Us-R/blob/master/source/Patches/ShowHostMeetingPatch.cs

[HarmonyPatch]
public class ShowHostMeetingPatch
{
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    [HarmonyPostfix]
    public static void UpdatePostfix(MeetingHud __instance)
    {
        // Not display in local game, because it will be impossible to complete the meeting
        if (!GameStates.IsOnlineGame) return;

        var host = GameData.Instance.GetHost();

        if (host != null)
        {
            PlayerMaterial.SetColors(host.DefaultOutfit.ColorId, __instance.HostIcon);
            __instance.ProceedButton.gameObject.GetComponentInChildren<TextMeshPro>().text = string.Format(Translator.GetString("HostIconInMeeting"), host.PlayerName);
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPostfix]

    public static void Setup(MeetingHud __instance)
    {
        if (!GameStates.IsOnlineGame) return;

        __instance.ProceedButton.gameObject.transform.localPosition = new(-2.5f, 2.2f, 0);
        __instance.ProceedButton.gameObject.GetComponent<SpriteRenderer>().enabled = false;
        __instance.ProceedButton.GetComponent<PassiveButton>().enabled = false;
        __instance.HostIcon.enabled = true;
        __instance.HostIcon.gameObject.SetActive(true);
        __instance.ProceedButton.gameObject.SetActive(true);
    }
}