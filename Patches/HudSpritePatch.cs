using TOHE.Roles.Core;
using UnityEngine;
using System;
using TOHE.Modules;

namespace TOHE;

public static class CustomButton
{
    public static Sprite Get(string name) => Utils.LoadSprite($"TOHE.Resources.Images.Skills.{name}.png", 115f);
}

[HarmonyPatch(typeof(HudManager), nameof(HudManager.SetHudActive))]
[HarmonyPatch(new Type[] { typeof(PlayerControl), typeof(RoleBehaviour), typeof(bool) })]
public static class HudSpritePatch
{
    private static Sprite Kill;
    private static Sprite Ability;
    private static Sprite Vent;
    private static Sprite Report;
    public static void Postfix(HudManager __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(2)] bool isActive)
    {
        if (player == null || GameStates.IsHideNSeek || !GameStates.IsModHost) return;
        if (!isActive || !player.IsAlive()) return;
        if (!Main.EnableCustomButton.Value) return;

        if (!AmongUsClient.Instance.IsGameStarted || !Main.introDestroyed)
        {
            Kill = null;
            Ability = null;
            Vent = null;
            Report = null;
            return;
        }

        bool shapeshifting = Main.CheckShapeshift.TryGetValue(player.PlayerId, out bool ss) && ss;

        if (!Kill) Kill = __instance.KillButton.graphic.sprite;
        if (!Ability) Ability = AbilityButtonSetFromSettingsPatch.abilityButton;
        if (!Vent) Vent = __instance.ImpostorVentButton.graphic.sprite;
        if (!Report) Report = __instance.ReportButton.graphic.sprite;

        Sprite newKillButton = Kill;
        Sprite newAbilityButton = Ability;
        Sprite newVentButton = Vent;
        Sprite newReportButton = Report;

        var playerRoleClass = player.GetRoleClass();
        if (playerRoleClass == null) goto EndOfSelectImg;

        if (playerRoleClass?.GetKillButtonSprite(player, shapeshifting) is Sprite killbutton)
            newKillButton = killbutton;

        if (playerRoleClass?.ImpostorVentButtonSprite(player) is Sprite Ventbutton)
            newVentButton = Ventbutton;

        if (playerRoleClass?.GetAbilityButtonSprite(player, shapeshifting) is Sprite Abilitybutton)
            newAbilityButton = Abilitybutton;

        if (playerRoleClass?.ReportButtonSprite is Sprite Reportbutton)
            newReportButton = Reportbutton;

        // CustomButton.Get("Paranoid"); for Paranoid

        EndOfSelectImg:

        // Set custom icon for kill button
        __instance.KillButton.graphic.sprite = newKillButton;
        //  Set custom icon for impostor vent button
        __instance.ImpostorVentButton.graphic.sprite = newVentButton;

        // Set custom icon for ability button (Shapeshift, Vitals, Engineer Vent)
        __instance.AbilityButton.graphic.sprite = newAbilityButton;

        // This code replaces the sprite that displays the quantity next to the button (for example, like the Engineer)
        //__instance.AbilityButton.usesRemainingSprite.sprite = newAbilityButton;

        // Set custom icon for report button
        __instance.ReportButton.graphic.sprite = newReportButton;

        // Normalized Uvs
        // The sprites after custom icons has a strong overexposure
        __instance.KillButton.graphic.SetCooldownNormalizedUvs();
        __instance.ImpostorVentButton.graphic.SetCooldownNormalizedUvs();
        __instance.AbilityButton.graphic.SetCooldownNormalizedUvs();
        __instance.ReportButton.graphic.SetCooldownNormalizedUvs();
    }
}

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.SetFromSettings))]
public static class AbilityButtonSetFromSettingsPatch
{
    public static Sprite abilityButton;
    public static bool Prefix(AbilityButton __instance, AbilityButtonSettings settings)
    {
        // When Custom Buttons is disabled run vanilla code
        if (!Main.EnableCustomButton.Value || __instance == null || settings == null) return true;

        __instance.SetInfiniteUses();

        abilityButton = settings.Image;
        // When ability button is initialize or player is dead, set default image
        if (!Main.introDestroyed || PlayerControl.LocalPlayer.Data.IsDead || AmongUsClient.Instance.IsGameOver || RoleBasisChanger.SkipTasksAfterAssignRole)
        {
            __instance.graphic.sprite = settings.Image;
        }

        __instance.graphic.SetCooldownNormalizedUvs();
        __instance.buttonLabelText.fontSharedMaterial = settings.FontMaterial;
        __instance.buttonLabelText.text = TranslationController.Instance.GetString(settings.Text);
        return false;
    }
}