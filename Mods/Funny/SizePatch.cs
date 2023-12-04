using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace TOHE
{
    [HarmonyPatch]
    public static class SizePatch
    {
        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
        [HarmonyPostfix]
        public static void Postfix(HudManager __instance)
        {
            PlayerControl playerControl = PlayerControl.LocalPlayer.GetComponent<PlayerControl>();
            Vector3 playerLocalScale = PlayerControl.LocalPlayer.transform.localScale;
            if (Main.BigSize.Value == true)
            {
                //Set sprite scale
            PlayerControl.LocalPlayer.transform.localScale = new Vector3 (1.5f, 1.5f, 1f);
            }
            else
            {
                //Set sprite scale
                PlayerControl.LocalPlayer.transform.localScale = new Vector3 (0.7f, 0.7f, 1f);
            }
        }
    }
}
