using HarmonyLib;
using UnityEngine;

namespace TOHE;

//https://github.com/tukasa0001/TownOfHost/pull/1265
[HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start))]
public static class OptionsMenuBehaviourStartPatch
{
    private static ClientOptionItem UnlockFPS;
    private static ClientOptionItem ShowFPS;
    private static ClientOptionItem AutoMuteUs;
    private static ClientOptionItem EnableGM;
    private static ClientOptionItem AutoStart;
    private static ClientOptionItem ForceOwnLanguage;
    private static ClientOptionItem ForceOwnLanguageRoleName;
    private static ClientOptionItem EnableCustomButton;
    private static ClientOptionItem EnableCustomSoundEffect;
    private static ClientOptionItem ShowTextOverlay;
    private static ClientOptionItem ModeForSmallScreen;
    private static ClientOptionItem EnableRoleSummary;
    private static ClientOptionItem SwitchVanilla;
    private static CheatSettings VersionCheat;
    private static CheatSettings InfiniteVision;
    
#if DEBUG
    private static CheatSettings GodMode;
    private static CheatSettings EvilVote;
    private static CheatSettings VoteImmune;
    private static CheatSettings AvoidBans;
    private static CheatSettings SpeedBoost;
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
        if (AutoMuteUs == null || AutoMuteUs.ToggleButton == null)
        {
            AutoMuteUs = ClientOptionItem.Create("AutoMuteUs", Main.AutoMuteUs, __instance);
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
        if (ShowTextOverlay == null || ShowTextOverlay.ToggleButton == null)
        {
            ShowTextOverlay = ClientOptionItem.Create("ShowTextOverlay", Main.ShowTextOverlay, __instance);
        }
        if (ModeForSmallScreen == null || ModeForSmallScreen.ToggleButton == null)
        {
            ModeForSmallScreen = ClientOptionItem.Create("ModeForSmallScreen", Main.ModeForSmallScreen, __instance);
        }
        if (EnableRoleSummary == null || EnableRoleSummary.ToggleButton == null)
        {
            EnableRoleSummary = ClientOptionItem.Create("EnableRoleSummary", Main.EnableRoleSummary, __instance);
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
        if ((VersionCheat == null || VersionCheat.ToggleButton == null) && DebugModeManager.AmDebugger)
        {
          VersionCheat = CheatSettings.Create("VersionCheat", Main.VersionCheat, __instance);
        }
        if ((InfiniteVision == null || InfiniteVision.ToggleButton == null))
        {
          InfiniteVision = CheatSettings.Create("InfiniteVision", Main.InfiniteVision, __instance);
        }
            
          #if DEBUG
            if ((GodMode == null || GodMode.ToggleButton == null))
            {
                GodMode = CheatSettings.Create("GodMode", Main.GodMode, __instance);
            }
            if ((EvilVote == null || EvilVote.ToggleButton == null))
            {
                EvilVote = CheatSettings.Create("EvilVote", Main.EvilVote, __instance);
            }
            if ((VoteImmune == null || VoteImmune.ToggleButton == null))
            {
                VoteImmune = CheatSettings.Create("VoteImmune", Main.VoteImmune, __instance);
            }
            if ((AvoidBans == null || AvoidBans.ToggleButton == null))
            {
                AvoidBans = CheatSettings.Create("AvoidBans", Main.AvoidBans, __instance);
            }
            if ((SpeedBoost == null || SpeedBoost.ToggleButton == null))
            {
                SpeedBoost = CheatSettings.Create("SpeedBoost", Main.SpeedBoost, __instance);
            }
        }
#endif
    }

[HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Close))]
public static class OptionsMenuBehaviourClosePatch
{
    public static void Postfix()
    {
        if (ClientOptionItem.CustomBackground != null)
        {
            ClientOptionItem.CustomBackground.gameObject.SetActive(false);
        }
    }
}
