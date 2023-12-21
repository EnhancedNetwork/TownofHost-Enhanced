using HarmonyLib;
using System.Collections.Generic;

namespace TOHE.Patches;

/* Originally by Kap. Reference: https://github.com/KARPED1EM/TownOfNext/blob/TONX/TONX/Patches/TextBoxPatch.cs */
[HarmonyPatch(typeof(TextBoxTMP))]
public class TextBoxPatch
{
    static Dictionary<string, string> replaceDic = new()
            {
                { "£¨", " (" },
                { "£©", ") " },
                { "£¬", ", " },
                { "£º", ": " },
                { "[", "¡¾" },
                { "]", "¡¿" },
                { "¡®", " '" },
                { "¡¯", "' " },
                { "¡°", " ''" },
                { "¡±", "'' " },
                { "£¡", "! " },
            };
    [HarmonyPatch(nameof(TextBoxTMP.SetText)), HarmonyPrefix]
    public static bool ModifyCharacterLimit(TextBoxTMP __instance, [HarmonyArgument(0)] string input, [HarmonyArgument(1)] string inputCompo = "")
    {
        __instance.characterLimit = AmongUsClient.Instance.AmHost ? 999 : 300;
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
