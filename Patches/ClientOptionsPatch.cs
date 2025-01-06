using UnityEngine;

namespace TOHE;

//��Դ��https://github.com/tukasa0001/TownOfHost/pull/1265
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
    private static ClientOptionItem HorseMode;
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
        if (__instance.DisableMouseMovement == null) return;

        Main.SwitchVanilla.Value = false;
        if (Main.ResetOptions || !DebugModeManager.AmDebugger)
        {
            Main.ResetOptions = false;
            Main.VersionCheat.Value = false;
            Main.GodMode.Value = false;
            Main.AutoRehost.Value = false;
        }

        if (UnlockFPS == null || UnlockFPS.ToggleButton == null)
        {
            UnlockFPS = ClientOptionItem.Create("UnlockFPS", Main.UnlockFPS, __instance, UnlockFPSButtonToggle);
            static void UnlockFPSButtonToggle()
            {
                Application.targetFrameRate = Main.UnlockFPS.Value ? 165 : 60;
                Logger.SendInGame(string.Format(Translator.GetString("FPSSetTo"), Application.targetFrameRate));
            }
        }
        if (ShowFPS == null || ShowFPS.ToggleButton == null)
        {
            ShowFPS = ClientOptionItem.Create("ShowFPS", Main.ShowFPS, __instance);
        }
        if (EnableGM == null || EnableGM.ToggleButton == null)
        {
            EnableGM = ClientOptionItem.Create("GM", Main.EnableGM, __instance);
        }
        if (AutoStart == null || AutoStart.ToggleButton == null)
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
        if (DarkTheme == null || DarkTheme.ToggleButton == null)
        {
            DarkTheme = ClientOptionItem.Create("DarkTheme", Main.DarkTheme, __instance);
        }
        if (DisableLobbyMusic == null || DisableLobbyMusic.ToggleButton == null)
        {
            DisableLobbyMusic = ClientOptionItem.Create("DisableLobbyMusic", Main.DisableLobbyMusic, __instance);
        }
        if (ShowTextOverlay == null || ShowTextOverlay.ToggleButton == null)
        {
            ShowTextOverlay = ClientOptionItem.Create("ShowTextOverlay", Main.ShowTextOverlay, __instance);
        }
        if (HorseMode == null || HorseMode.ToggleButton == null)
        {
            HorseMode = ClientOptionItem.Create("HorseMode", Main.HorseMode, __instance, SwitchHorseMode);
            static void SwitchHorseMode()
            {
                HorseMode.UpdateToggle();
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    pc.MyPhysics.SetBodyType(pc.BodyType);
                    if (pc.BodyType == PlayerBodyTypes.Normal)
                    {
                        pc.cosmetics.currentBodySprite.BodySprite.transform.localScale = new(0.5f, 0.5f, 1f);
                    }
                }
            }
        }
        if (ForceOwnLanguage == null || ForceOwnLanguage.ToggleButton == null)
        {
            ForceOwnLanguage = ClientOptionItem.Create("ForceOwnLanguage", Main.ForceOwnLanguage, __instance);
        }
        if (ForceOwnLanguageRoleName == null || ForceOwnLanguageRoleName.ToggleButton == null)
        {
            ForceOwnLanguageRoleName = ClientOptionItem.Create("ForceOwnLanguageRoleName", Main.ForceOwnLanguageRoleName, __instance);
        }
        if (EnableCustomButton == null || EnableCustomButton.ToggleButton == null)
        {
            EnableCustomButton = ClientOptionItem.Create("EnableCustomButton", Main.EnableCustomButton, __instance);
        }
        if (EnableCustomSoundEffect == null || EnableCustomSoundEffect.ToggleButton == null)
        {
            EnableCustomSoundEffect = ClientOptionItem.Create("EnableCustomSoundEffect", Main.EnableCustomSoundEffect, __instance);
        }
        if (EnableCustomDecorations == null || EnableCustomDecorations.ToggleButton == null)
        {
            EnableCustomDecorations = ClientOptionItem.Create("EnableCustomDecorations", Main.EnableCustomDecorations, __instance);
        }
        if (SwitchVanilla == null || SwitchVanilla.ToggleButton == null)
        {
            SwitchVanilla = ClientOptionItem.Create("SwitchVanilla", Main.SwitchVanilla, __instance, SwitchVanillaButtonToggle);
            static void SwitchVanillaButtonToggle()
            {
                Harmony.UnpatchAll();
                Main.Instance.Unload();
            }
        }

#if DEBUG
        if (EOSManager.Instance.friendCode.GetDevUser().DeBug)
        {
            if ((VersionCheat == null || VersionCheat.ToggleButton == null) && DebugModeManager.AmDebugger)
            {
                VersionCheat = ClientOptionItem.Create("VersionCheat", Main.VersionCheat, __instance);
            }
            if ((GodMode == null || GodMode.ToggleButton == null) && DebugModeManager.AmDebugger)
            {
                GodMode = ClientOptionItem.Create("GodMode", Main.GodMode, __instance);
            }
            if ((AutoRehost == null || AutoRehost.ToggleButton == null) && DebugModeManager.AmDebugger)
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