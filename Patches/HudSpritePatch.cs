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
    private static Sprite Sabotage;
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
            Sabotage = null;
            return;
        }

        bool shapeshifting = Main.CheckShapeshift.TryGetValue(player.PlayerId, out bool ss) && ss;

        if (!Kill) Kill = __instance.KillButton.graphic.sprite;
        if (!Ability) Ability = __instance.AbilityButton.graphic.sprite;
        if (!Vent) Vent = __instance.ImpostorVentButton.graphic.sprite;
        if (!Report) Report = __instance.ReportButton.graphic.sprite;
        if (!Sabotage) Sabotage = __instance.SabotageButton.graphic.sprite;

        Sprite newKillButton = Kill;
        Sprite newAbilityButton = Ability;
        Sprite newVentButton = Vent;
        Sprite newReportButton = Report;
        Sprite newSabotageButton = Sabotage;

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

        if (playerRoleClass?.SabotageButtonSprite is Sprite Sabotagebutton)
            newSabotageButton = Sabotagebutton;

        // CustomButton.Get("Paranoid"); for Paranoid

        EndOfSelectImg:

        // Set custom icon for Kill button
        __instance.KillButton.graphic.sprite = newKillButton;
        //  Set custom icon for impostor vent button
        __instance.ImpostorVentButton.graphic.sprite = newVentButton;

        // Set custom icon for Ability button (Shapeshift, Vitals, Engineer Vent)
        __instance.AbilityButton.graphic.sprite = newAbilityButton;

        // Set custom icon for Report button
        __instance.ReportButton.graphic.sprite = newReportButton;

        // Set custom icon for Sabotage button
        __instance.SabotageButton.graphic.sprite = newSabotageButton;

        // Normalized Uvs
        // The sprites after custom icons has a strong overexposure
        __instance.KillButton.graphic.SetCooldownNormalizedUvs();
        __instance.ImpostorVentButton.graphic.SetCooldownNormalizedUvs();
        __instance.AbilityButton.graphic.SetCooldownNormalizedUvs();
        __instance.ReportButton.graphic.SetCooldownNormalizedUvs();
        __instance.SabotageButton.graphic.SetCooldownNormalizedUvs();
    }
}
