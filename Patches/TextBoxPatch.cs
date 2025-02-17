using System;

namespace TOHE.Patches;

// Originally code by Gurge44. Reference: https://github.com/Gurge44/EndlessHostRoles/blob/main/Patches/TextBoxPatch.cs

[HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.SetText))]
class TextBoxTMPSetTextPatch
{
    // The only characters to treat specially are \r, \n and \b, allow all other characters to be written
    public static bool Prefix(TextBoxTMP __instance, [HarmonyArgument(0)] string input, [HarmonyArgument(1)] string inputCompo = "")
    {
        bool flag = false;
        char ch = ' ';
        __instance.AdjustCaretPosition(input.Length - __instance.text.Length);
        __instance.tempTxt.Clear();

        foreach (char c in input)
        {
            char upperInvariant = c;
            if (ch == ' ' && upperInvariant == ' ')
            {
                __instance.AdjustCaretPosition(-1);
            }
            else
            {
                switch (upperInvariant)
                {
                    case '\r':
                    case '\n':
                        flag = true;
                        break;
                    case '\b':
                        __instance.tempTxt.Length = Math.Max(__instance.tempTxt.Length - 1, 0);
                        __instance.AdjustCaretPosition(-2);
                        break;
                }

                if (__instance.ForceUppercase) upperInvariant = char.ToUpperInvariant(upperInvariant);
                if (upperInvariant is not '\r' and not '\n' and not '\b')
                {
                    __instance.tempTxt.Append(upperInvariant);
                    ch = upperInvariant;
                }
            }
        }

        if (!__instance.tempTxt.ToString().Equals(DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.EnterName), StringComparison.OrdinalIgnoreCase) && __instance.characterLimit > 0)
        {
            int length = __instance.tempTxt.Length;
            __instance.tempTxt.Length = Math.Min(__instance.tempTxt.Length, __instance.characterLimit);
            __instance.AdjustCaretPosition(-(length - __instance.tempTxt.Length));
        }

        input = __instance.tempTxt.ToString();
        if (!input.Equals(__instance.text) || !inputCompo.Equals(__instance.compoText))
        {
            __instance.text = input;
            __instance.compoText = inputCompo;
            string str = __instance.text;
            string compoText = __instance.compoText;

            if (__instance.Hidden)
            {
                str = "";
                for (int index = 0; index < __instance.text.Length; ++index)
                    str += "*";
            }

            __instance.outputText.text = str + compoText;
            __instance.outputText.ForceMeshUpdate(true, true);
            if (__instance.keyboard != null) __instance.keyboard.text = __instance.text;
            __instance.OnChange.Invoke();
        }

        if (flag) __instance.OnEnter.Invoke();
        __instance.SetPipePosition();

        return false;
    }
}

//Thanks https://github.com/NuclearPowered/Reactor/blob/master/Reactor/Patches/Fixes/CursorPosPatch.cs

//2024.8.13 break this

/// <summary>
/// "Fixes" an issue where empty TextBoxes have wrong cursor positions.
/// </summary>
/*[HarmonyPatch(typeof(TextMeshProExtensions), nameof(TextMeshProExtensions.CursorPos), typeof(TextMeshPro))]
[HarmonyPatch(typeof(TextMeshProExtensions), nameof(TextMeshProExtensions.CursorPos), typeof(TextMeshPro), typeof(int))]
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
} */



/* Originally by KARPED1EM. Reference: https://github.com/KARPED1EM/TownOfNext/blob/TONX/TONX/Patches/TextBoxPatch.cs */
/*[HarmonyPatch(typeof(TextBoxTMP))]
public class TextBoxPatch
{
    [HarmonyPatch(nameof(TextBoxTMP.SetText)), HarmonyPrefix]
    public static void ModifyCharacterLimit(TextBoxTMP __instance)
    {
        __instance.characterLimit = 1200;
    }
}*/