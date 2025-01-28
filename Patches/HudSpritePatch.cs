using TOHE.Roles.Core;
using UnityEngine;

namespace TOHE;

public static class CustomButton
{
    public static Sprite Get(string name) => Utils.LoadSprite($"TOHE.Resources.Images.Skills.{name}.png", 115f);
}

[HarmonyPriority(520)]
[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class HudSpritePatch
{
    private static Sprite Kill;
    private static Sprite Ability;
    private static Sprite Vent;
    private static Sprite Report;
    public static void Postfix(HudManager __instance)
    {
        if (__instance == null) return;
        var player = PlayerControl.LocalPlayer;
        if (player == null || !Main.EnableCustomButton.Value || AmongUsClient.Instance.IsGameOver || GameStates.IsLobby || GameStates.IsHideNSeek || !GameStates.IsModHost) return;
        if (!SetHudActivePatch.IsActive || !player.IsAlive()) return;

        if (!AmongUsClient.Instance.IsGameStarted || !Main.IntroDestroyed)
        {
            Kill = null;
            Ability = null;
            Vent = null;
            Report = null;
            return;
        }

        bool shapeshifting = Main.CheckShapeshift.GetValueOrDefault(player.PlayerId, false);

        if (!Kill) Kill = __instance.KillButton.graphic.sprite;
        if (!Ability) Ability = __instance.AbilityButton.graphic.sprite;
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
