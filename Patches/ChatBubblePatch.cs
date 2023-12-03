using HarmonyLib;
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
[HarmonyPatch(typeof(ChatBubble))]
public static class ChatBubblePatch
{
    [HarmonyPatch(nameof(ChatBubble.SetName)), HarmonyPostfix]
    public static void SetName_Postfix(ChatBubble __instance)
    {
        if (GameStates.IsInGame && __instance.playerInfo.PlayerId == PlayerControl.LocalPlayer.PlayerId)
            __instance.NameText.color = PlayerControl.LocalPlayer.GetRoleColor();
    }
    [HarmonyPatch(nameof(ChatBubble.SetText)), HarmonyPrefix]
    public static void SetText_Prefix(ChatBubble __instance, ref string chatText)
    {
        bool modded = PlayerControl.LocalPlayer.IsModClient();
        __instance.Background.color = modded ? Color.black : Color.white;

        if (modded)
        {
            chatText = Utils.ColorString(Color.white, chatText.TrimEnd('\0'));
        }
    }
}