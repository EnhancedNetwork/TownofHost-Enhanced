using BepInEx.Unity.IL2CPP.Utils.Collections;
using UnityEngine;

namespace TOHE.Patches;

[HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Start))]
public class LobbyStartPatch
{
    private static GameObject LobbyPaintObject;
    private static GameObject DropshipDecorationsObject;
    private static Sprite LobbyPaintSprite;
    private static Sprite DropshipDecorationsSprite;

    private static bool FirstDecorationsLoad = true;
    public static void Prefix()
    {
        LobbyPaintSprite = Utils.LoadSprite("TOHE.Resources.Images.LobbyPaint.png", 290f);
        DropshipDecorationsSprite = Utils.LoadSprite("TOHE.Resources.Images.Dropship-Decorations.png", 60f);
    }
    public static void Postfix(LobbyBehaviour __instance)
    {
        float waitTime = 0f;
        if (FirstDecorationsLoad)
            waitTime = 0.25f;
        else
            waitTime = 0.05f;

        _ = new LateTask(() =>
        {
            __instance.StartCoroutine(CoLoadDecorations().WrapToIl2Cpp());
        }, waitTime, "Co Load Dropship Decorations", shoudLog: false);

        static System.Collections.IEnumerator CoLoadDecorations()
        {
            var LeftBox = GameObject.Find("Leftbox");
            if (LeftBox != null)
            {
                LobbyPaintObject = Object.Instantiate(LeftBox, LeftBox.transform.parent.transform);
                LobbyPaintObject.name = "Lobby Paint";
                LobbyPaintObject.transform.localPosition = new Vector3(0.042f, -2.59f, -10.5f);
                SpriteRenderer renderer = LobbyPaintObject.GetComponent<SpriteRenderer>();
                renderer.sprite = LobbyPaintSprite;
            }

            yield return null;

            if (Main.EnableCustomDecorations.Value)
            {
                var Dropship = GameObject.Find("SmallBox");
                if (Dropship != null)
                {
                    DropshipDecorationsObject = Object.Instantiate(Dropship, Object.FindAnyObjectByType<LobbyBehaviour>().transform);
                    DropshipDecorationsObject.name = "Lobby_Decorations";
                    DropshipDecorationsObject.transform.DestroyChildren();
                    Object.Destroy(DropshipDecorationsObject.GetComponent<PolygonCollider2D>());
                    DropshipDecorationsObject.GetComponent<SpriteRenderer>().sprite = DropshipDecorationsSprite;
                    DropshipDecorationsObject.transform.SetSiblingIndex(1);
                    DropshipDecorationsObject.transform.localPosition = new(0.05f, 0.8334f);
                }
            }

            yield return null;

            FirstDecorationsLoad = false;
        }
    }
}
// https://github.com/SuperNewRoles/SuperNewRoles/blob/master/SuperNewRoles/Patches/LobbyBehaviourPatch.cs
[HarmonyPatch(typeof(LobbyBehaviour))]
public class LobbyBehaviourPatch
{
    [HarmonyPatch(nameof(LobbyBehaviour.Update)), HarmonyPostfix]
    public static void Update_Postfix(LobbyBehaviour __instance)
    {
        System.Func<ISoundPlayer, bool> lobbybgm = x => x.Name.Equals("MapTheme");
        ISoundPlayer MapThemeSound = SoundManager.Instance.soundPlayers.Find(lobbybgm);
        if (Main.DisableLobbyMusic.Value)
        {
            if (MapThemeSound == null) return;
            SoundManager.Instance.StopNamedSound("MapTheme");
        }
        else
        {
            if (MapThemeSound != null) return;
            SoundManager.Instance.CrossFadeSound("MapTheme", __instance.MapTheme, 0.5f);
        }
    }
}

[HarmonyPatch(typeof(PlayerMaterial), nameof(PlayerMaterial.SetColors), [typeof(int), typeof(Material)])]
public class PlayerMaterialPatch
{
    public static void Prefix([HarmonyArgument(0)] ref int colorId)
    {
        if (colorId < 0 || colorId >= Palette.PlayerColors.Length)
        {
            colorId = 0;
        }
    }
}

[HarmonyPatch(typeof(NetworkedPlayerInfo), nameof(NetworkedPlayerInfo.Init))]
public class NetworkedPlayerInfoInitPatch
{
    public static void Postfix(NetworkedPlayerInfo __instance)
    {
        foreach (var outfit in __instance.Outfits)
        {
            if (outfit.Value != null)
            {
                if (outfit.Value.ColorId < 0 || outfit.Value.ColorId >= Palette.PlayerColors.Length)
                {
                    outfit.Value.ColorId = 0;
                }
            }
        }
    }
}
