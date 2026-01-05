using TMPro;
using UnityEngine;

namespace TOHE.Patches;

// Thanks TOU-R: https://github.com/eDonnes124/Town-Of-Us-R/blob/master/source/Patches/ShowHostMeetingPatch.cs

[HarmonyPatch]
public class ShowHostMeetingPatch
{
    public static PlayerControl HostControl = null;
    public static string HostName = string.Empty;
    public static int HostColor = int.MaxValue;

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.OnDestroy))]
    [HarmonyPostfix]
    public static void OnDestroy_Postfix()
    {
        try
        {
            if (GameStates.IsInGame && HostControl == null)
            {
                HostControl = AmongUsClient.Instance.GetHost().Character;
                HostName = Main.PlayerStates[AmongUsClient.Instance.GetHost().Character.Data.PlayerId].NormalOutfit.PlayerName;
                HostColor = Main.PlayerStates[AmongUsClient.Instance.GetHost().Character.Data.PlayerId].NormalOutfit.ColorId;
            }
        }
        catch { }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    [HarmonyPostfix]
    public static void Update_Postfix(MeetingHud __instance)
    {
        // Not display in local game, because it will be impossible to complete the meeting
        if (!GameStates.IsOnlineGame) return;

        PlayerMaterial.SetColors(HostColor, __instance.HostIcon);
        __instance.ProceedButton.gameObject.GetComponentInChildren<TextMeshPro>().text = string.Format(Translator.GetString("HostIconInMeeting"), HostName);
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPostfix]

    public static void Setup_Postfix(MeetingHud __instance)
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
