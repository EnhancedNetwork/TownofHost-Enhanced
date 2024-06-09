using TMPro;
using UnityEngine;

namespace TOHE.Patches;

// Thanks TOU-R: https://github.com/eDonnes124/Town-Of-Us-R/blob/master/source/Patches/ShowHostMeetingPatch.cs

[HarmonyPatch]
public class ShowHostMeetingPatch
{
    private static string hostName = string.Empty;
    private static int hostColor = int.MaxValue;

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.ShowRole))]
    [HarmonyPostfix]
    public static void ShowRolePostfix()
    {
        hostName = AmongUsClient.Instance.GetHost().Character.CurrentOutfit.PlayerName;
        hostColor = AmongUsClient.Instance.GetHost().Character.CurrentOutfit.ColorId;
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    [HarmonyPostfix]
    public static void UpdatePostfix(MeetingHud __instance)
    {
        // Not display in local game, because it will be impossible to complete the meeting
        if (!GameStates.IsOnlineGame) return;

        PlayerMaterial.SetColors(hostColor, __instance.HostIcon);
        __instance.ProceedButton.gameObject.GetComponentInChildren<TextMeshPro>().text = string.Format(Translator.GetString("HostIconInMeeting"), hostName);
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
