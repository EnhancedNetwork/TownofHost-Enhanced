using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace TOHE;

[HarmonyPatch(typeof(TextBoxTMP))]
public class TextBoxPatch
{
    static Dictionary<string, string> replaceDic = new()
            {
                { "（", " (" },
                { "）", ") " },
                { "，", ", " },
                { "：", ": " },
                { "[", "【" },
                { "]", "】" },
                { "‘", " '" },
                { "’", "' " },
                { "“", " ''" },
                { "”", "'' " },
                { "！", "! " },
            };
    [HarmonyPatch(nameof(TextBoxTMP.SetText)), HarmonyPrefix]
    public static bool ModifyCharacterLimit(TextBoxTMP __instance, [HarmonyArgument(0)] string input, [HarmonyArgument(1)] string inputCompo = "")
    {
        __instance.characterLimit = AmongUsClient.Instance.AmHost ? 20000 : 2000;
        __instance.AllowSymbols = true;
        __instance.AllowEmail = true;
        __instance.allowAllCharacters = true;
        if (input.Length < 1) return true;
        string before = input[^1..];
        if (replaceDic.TryGetValue(before, out var after))
        {
            __instance.SetText(input.Replace(before, after));
            return false;
        }
        return true;
    }
}
[HarmonyPatch(typeof(FreeChatInputField), nameof(FreeChatInputField.UpdateCharCount))]
internal class UpdateCharCountPatch
{
    public static void Postfix(FreeChatInputField __instance)
    {
        int length = __instance.textArea.text.Length;
        __instance.charCountText.SetText($"{length}/{__instance.textArea.characterLimit}");
        if (length < (AmongUsClient.Instance.AmHost ? 19000 : 1900))
            __instance.charCountText.color = Color.black;
        else if (length < (AmongUsClient.Instance.AmHost ? 19999 : 1999))
            __instance.charCountText.color = new Color(1f, 1f, 0f, 1f);
        else
            __instance.charCountText.color = Color.red;
    }
}