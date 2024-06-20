using TMPro;
using UnityEngine;

namespace TOHE.Patches;

[HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Start))]
public class LobbyStartPatch
{
    private static GameObject Paint;
    public static void Postfix()
    {
        _ = new LateTask(() =>
        {
            if (!GameStates.IsLobby || Paint != null) return;
            
            var LeftBox = GameObject.Find("Leftbox");
            if (LeftBox != null)
            {
                Paint = Object.Instantiate(LeftBox, LeftBox.transform.parent.transform);
                Paint.name = "Lobby Paint";
                Paint.transform.localPosition = new Vector3(0.042f, -2.59f, -10.5f);
                SpriteRenderer renderer = Paint.GetComponent<SpriteRenderer>();
                renderer.sprite = Utils.LoadSprite("TOHE.Resources.Images.LobbyPaint.png", 290f);
            }
        }, 3f, "LobbyPaint", shoudLog: false);
    }
}
[HarmonyPatch(typeof(HostInfoPanel), nameof(HostInfoPanel.SetUp))]
public static class HostInfoPanelUpdatePatch
{
    private static TextMeshPro HostText;
    public static void Postfix(HostInfoPanel __instance)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            if (HostText == null)
                HostText = __instance.content.transform.FindChild("Name").GetComponent<TextMeshPro>();

            string htmlStringRgb = ColorUtility.ToHtmlStringRGB(Palette.PlayerColors[__instance.player.ColorId]);
            string hostName = Main.HostRealName;
            string youLabel = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.HostYouLabel);

            // Set text in host info panel
            HostText.text = $"<color=#{htmlStringRgb}>{hostName}</color>  <size=90%><b><font=\"Barlow-BoldItalic SDF\" material=\"Barlow-BoldItalic SDF Outline\">({youLabel})";
        }
    }
}