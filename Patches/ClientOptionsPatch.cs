using UnityEngine;

namespace TOHE;

//锟斤拷源锟斤拷https://github.com/tukasa0001/TownOfHost/pull/1265
[HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start))]
public static class OptionsMenuBehaviourStartPatch
{
    private static ClientOptionItem UnlockFPS;
    private static ClientOptionItem ShowFPS;
    private static ClientOptionItem EnableGM;
    private static ClientOptionItem AutoStart;
    private static ClientOptionItem DarkTheme;
    private static ClientOptionItem DisableLobbyMusic;
    private static ClientOptionItem ShowTextOverlay;
    private static ClientOptionItem ShowModdedClientText;
    private static ClientOptionItem HorseMode;
    private static ClientOptionItem LongMode;
    private static ClientOptionItem EnableCommandHelper;
    private static ClientOptionItem ForceOwnLanguage;
    private static ClientOptionItem ForceOwnLanguageRoleName;
    private static ClientOptionItem EnableCustomButton;
    private static ClientOptionItem EnableCustomSoundEffect;
    private static ClientOptionItem EnableCustomDecorations;
    private static ClientOptionItem SwitchVanilla;

#if DEBUG
    private static ClientOptionItem VersionCheat;
    private static ClientOptionItem GodMode;
    private static ClientOptionItem AutoRehost;
#endif

    public static void Postfix(OptionsMenuBehaviour __instance)
    {
        if (!__instance.DisableMouseMovement) return;

        Main.SwitchVanilla.Value = false;
        if (Main.ResetOptions || !DebugModeManager.AmDebugger)
        {
            Main.ResetOptions = false;
            Main.VersionCheat.Value = false;
            Main.GodMode.Value = false;
            Main.AutoRehost.Value = false;
        }

#if ANDROID
        Main.UnlockFPS.Value = false;
#endif

#if !ANDROID
        if (UnlockFPS == null || !UnlockFPS.ToggleButton)
        {
            UnlockFPS = ClientOptionItem.Create("UnlockFPS", Main.UnlockFPS, __instance, UnlockFPSButtonToggle);
            static void UnlockFPSButtonToggle()
            {
                Application.targetFrameRate = Main.UnlockFPS.Value ? 165 : 60;
                Logger.SendInGame(string.Format(Translator.GetString("FPSSetTo"), Application.targetFrameRate));
            }
        }
#endif

        if (ShowFPS == null || !ShowFPS.ToggleButton)
        {
            ShowFPS = ClientOptionItem.Create("ShowFPS", Main.ShowFPS, __instance);
        }
        if (EnableGM == null || !EnableGM.ToggleButton)
        {
            EnableGM = ClientOptionItem.Create("GM", Main.EnableGM, __instance);
        }
        if (AutoStart == null || !AutoStart.ToggleButton)
        {
            AutoStart = ClientOptionItem.Create("AutoStart", Main.AutoStart, __instance, AutoStartButtonToggle);
            static void AutoStartButtonToggle()
            {
                if (Main.AutoStart.Value == false && GameStates.IsCountDown)
                {
                    GameStartManager.Instance.ResetStartState();
                    Logger.SendInGame(Translator.GetString("CancelStartCountDown"));
                }
            }
        }
        if (DarkTheme == null || !DarkTheme.ToggleButton)
        {
            DarkTheme = ClientOptionItem.Create("DarkTheme", Main.DarkTheme, __instance);
        }
        if (DisableLobbyMusic == null || !DisableLobbyMusic.ToggleButton)
        {
            DisableLobbyMusic = ClientOptionItem.Create("DisableLobbyMusic", Main.DisableLobbyMusic, __instance);
        }
        if (ShowTextOverlay == null || !ShowTextOverlay.ToggleButton)
        {
            ShowTextOverlay = ClientOptionItem.Create("ShowTextOverlay", Main.ShowTextOverlay, __instance);
        }
        if (ShowModdedClientText == null || !ShowModdedClientText.ToggleButton)
        {
            ShowModdedClientText = ClientOptionItem.Create("ShowModdedClientText", Main.ShowModdedClientText, __instance);
        }
        if (HorseMode == null || !HorseMode.ToggleButton)
        {
            HorseMode = ClientOptionItem.Create("HorseMode", Main.HorseMode, __instance, SwitchHorseMode);

            static void SwitchHorseMode()
            {
                Main.LongMode.Value = false;
                HorseMode.UpdateToggle();
                LongMode.UpdateToggle();

                foreach (PlayerControl pc in Main.EnumeratePlayerControls())
                {
                    pc.MyPhysics.SetBodyType(pc.BodyType);
                    if (pc.BodyType == PlayerBodyTypes.Normal) pc.cosmetics.currentBodySprite.BodySprite.transform.localScale = new(0.5f, 0.5f, 1f);
                }
            }
        }

        if (LongMode == null || !LongMode.ToggleButton)
        {
            LongMode = ClientOptionItem.Create("LongMode", Main.LongMode, __instance, SwitchLongMode);

            static void SwitchLongMode()
            {
                Main.HorseMode.Value = false;
                HorseMode.UpdateToggle();
                LongMode.UpdateToggle();

                foreach (PlayerControl pc in Main.EnumeratePlayerControls())
                {
                    pc.MyPhysics.SetBodyType(pc.BodyType);
                    if (pc.BodyType == PlayerBodyTypes.Normal) pc.cosmetics.currentBodySprite.BodySprite.transform.localScale = new(0.5f, 0.5f, 1f);
                }
            }
        }
        if (EnableCommandHelper == null || !EnableCommandHelper.ToggleButton)
            EnableCommandHelper = ClientOptionItem.Create("EnableCommandHelper", Main.EnableCommandHelper, __instance);
        if (ForceOwnLanguage == null || !ForceOwnLanguage.ToggleButton)
        {
            ForceOwnLanguage = ClientOptionItem.Create("ForceOwnLanguage", Main.ForceOwnLanguage, __instance);
        }
        if (ForceOwnLanguageRoleName == null || !ForceOwnLanguageRoleName.ToggleButton)
        {
            ForceOwnLanguageRoleName = ClientOptionItem.Create("ForceOwnLanguageRoleName", Main.ForceOwnLanguageRoleName, __instance);
        }
        if (EnableCustomButton == null || !EnableCustomButton.ToggleButton)
        {
            EnableCustomButton = ClientOptionItem.Create("EnableCustomButton", Main.EnableCustomButton, __instance);
        }
        if (EnableCustomSoundEffect == null || !EnableCustomSoundEffect.ToggleButton)
        {
            EnableCustomSoundEffect = ClientOptionItem.Create("EnableCustomSoundEffect", Main.EnableCustomSoundEffect, __instance);
        }
        if (EnableCustomDecorations == null || !EnableCustomDecorations.ToggleButton)
        {
            EnableCustomDecorations = ClientOptionItem.Create("EnableCustomDecorations", Main.EnableCustomDecorations, __instance);
        }

#if !ANDROID
        if (SwitchVanilla == null || !SwitchVanilla.ToggleButton)
        {
            SwitchVanilla = ClientOptionItem.Create("SwitchVanilla", Main.SwitchVanilla, __instance, SwitchVanillaButtonToggle);
            static void SwitchVanillaButtonToggle()
            {
                Harmony.UnpatchAll();
                Main.Instance.Unload();
            }
        }
#endif

#if DEBUG
        if (EOSManager.Instance.friendCode.GetDevUser().DeBug)
        {
            if ((VersionCheat == null || !VersionCheat.ToggleButton) && DebugModeManager.AmDebugger)
            {
                VersionCheat = ClientOptionItem.Create("VersionCheat", Main.VersionCheat, __instance);
            }
            if ((GodMode == null || !GodMode.ToggleButton) && DebugModeManager.AmDebugger)
            {
                GodMode = ClientOptionItem.Create("GodMode", Main.GodMode, __instance);
            }
            if ((AutoRehost == null || !AutoRehost.ToggleButton) && DebugModeManager.AmDebugger)
            {
                AutoRehost = ClientOptionItem.Create("AutoRehost", Main.AutoRehost, __instance);
            }
        }
#endif
    }
}

[HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Close))]
public static class OptionsMenuBehaviourClosePatch
{
    public static void Postfix()
    {
        ClientOptionItem.CustomBackground?.gameObject.SetActive(false);
    }
}
