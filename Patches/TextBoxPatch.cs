using System;

namespace TOHE.Patches;

// Originally code by Gurge44. Reference: https://github.com/Gurge44/EndlessHostRoles/blob/main/Patches/TextBoxPatch.cs

[HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.SetText))]
class TextBoxTMPSetTextPatch
{
    public static bool Prefix(TextBoxTMP __instance, [HarmonyArgument(0)] string input, [HarmonyArgument(1)] string inputCompo = "")
    {
        bool flag = false;
        char ch = ' ';
        __instance.tempTxt.Clear();

        foreach (var str in input)
        {
            char upperInvariant = str;
            if (ch != ' ' || upperInvariant != ' ')
            {
                switch (upperInvariant)
                {
                    case '\r' or '\n':
                        flag = true;
                        break;
                    case '\b':
                        __instance.tempTxt.Length = Math.Max(__instance.tempTxt.Length - 1, 0);
                        break;
                }

                if (__instance.ForceUppercase) upperInvariant = char.ToUpperInvariant(upperInvariant);
                if (upperInvariant is not '\b' and not '\n' and not '\r')
                {
                    __instance.tempTxt.Append(upperInvariant);
                    ch = upperInvariant;
                }
            }
        }

        if (!__instance.tempTxt.ToString().Equals(DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.EnterName), StringComparison.OrdinalIgnoreCase) && __instance.characterLimit > 0)
            __instance.tempTxt.Length = Math.Min(__instance.tempTxt.Length, __instance.characterLimit);
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
        __instance.Pipe.transform.localPosition = __instance.outputText.CursorPos();

        return false;
    }
}
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