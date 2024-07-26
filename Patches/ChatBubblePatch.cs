using TMPro;
using UnityEngine;

namespace TOHE.Patches;

[HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetRight))]
class ChatBubbleSetRightPatch
{
    public static void Postfix(ChatBubble __instance)
    {
        if (Main.isChatCommand) __instance.SetLeft();
    }
}
[HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetName))]
class ChatBubbleSetNamePatch
{
    public static void Postfix(ChatBubble __instance, [HarmonyArgument(1)] bool isDead, [HarmonyArgument(2)] bool voted)
    {
        if (GameStates.IsInGame && !voted && __instance.playerInfo.PlayerId == PlayerControl.LocalPlayer.PlayerId)
            __instance.NameText.color = PlayerControl.LocalPlayer.GetRoleColor();

        if (Main.DarkTheme.Value)
        {
            if (isDead)
                __instance.Background.color = new Color(0.0f, 0.0f, 0.0f, 0.6f);
            else
                __instance.Background.color = Color.black;

            __instance.TextArea.color = Color.white;
        }
    }
}

//Thanks https://github.com/NuclearPowered/Reactor/blob/master/Reactor/Patches/Fixes/CursorPosPatch.cs

/// <summary>
/// "Fixes" an issue where empty TextBoxes have wrong cursor positions.
/// </summary>
[HarmonyPatch(typeof(TextMeshProExtensions), nameof(TextMeshProExtensions.CursorPos))]
internal static class CursorPosPatch
{
    public static bool Prefix(TextMeshPro self, ref Vector2 __result)
    {
        if (self.textInfo == null || self.textInfo.lineCount == 0 || self.textInfo.lineInfo[0].characterCount <= 0)
        {
            __result = self.GetTextInfo(" ").lineInfo.First().lineExtents.max;
            return false;
        }

        return true;
    }
}