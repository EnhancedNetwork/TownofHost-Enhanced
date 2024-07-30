namespace TOHE;

[HarmonyPatch(typeof(RoleOptionSetting), nameof(RoleOptionSetting.UpdateValuesAndText))]
class ChanceChangePatch
{
    public static void Postfix(RoleOptionSetting __instance)
    {
        // Remove vanilla role spawn
        string disableText = $" ({Translator.GetString("Disabled")})";
        foreach (var button in __instance.GetComponentsInChildren<PassiveButton>())
        {
            button.gameObject.SetActive(false);
        }

        if (!__instance.titleText.text.Contains(disableText))
            __instance.titleText.text += disableText;

        if (__instance.roleChance != 0 || __instance.roleMaxCount != 0)
        {
            __instance.roleChance = 0;
            __instance.roleMaxCount = 0;
            __instance.OnValueChanged.Invoke(__instance);
        }
    }
    /*[HarmonyPatch(typeof(GameOptionsManager), nameof(GameOptionsManager.SwitchGameMode))]
    class SwitchGameModePatch
    {
        public static void Postfix(GameModes gameMode)
        {
            if (gameMode == GameModes.HideNSeek)
            {
                ErrorText.Instance.HnSFlag = true;
                ErrorText.Instance.AddError(ErrorCode.HnsUnload);
                Harmony.UnpatchAll();
                Main.Instance.Unload();
            }
        }
    }*/
}