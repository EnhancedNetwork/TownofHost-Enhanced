using UnityEngine;

namespace TOHE;

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
