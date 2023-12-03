using HarmonyLib;
using System.Collections.Generic;

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