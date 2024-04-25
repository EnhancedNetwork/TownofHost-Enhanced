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
        var player = PlayerControl.LocalPlayer;
        if (player == null || GameStates.IsHideNSeek || !GameStates.IsModHost) return;
        if (!SetHudActivePatch.IsActive || !player.IsAlive()) return;
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
        if (!Ability) Ability = __instance.AbilityButton.graphic.sprite;
        if (!Vent) Vent = __instance.ImpostorVentButton.graphic.sprite;
        if (!Report) Report = __instance.ReportButton.graphic.sprite;

        Sprite newKillButton = Kill;
        Sprite newAbilityButton = Ability;
        Sprite newVentButton = Vent;
        Sprite newReportButton = Report;

        var playerRoleClass = player.GetRoleClass();
        if (playerRoleClass == null) goto EndOfSelectImg;

        if (playerRoleClass?.GetKillButtonSprite(player, shapeshifting) != null)
            newKillButton = playerRoleClass.GetKillButtonSprite(player, shapeshifting);

        if (playerRoleClass?.ImpostorVentButtonSprite(player) != null)
            newVentButton = playerRoleClass.ImpostorVentButtonSprite(player);

        if (playerRoleClass?.GetAbilityButtonSprite(player, shapeshifting) != null)
            newAbilityButton = playerRoleClass.GetAbilityButtonSprite(player, shapeshifting);

        if (playerRoleClass?.ReportButtonSprite != null)
            newReportButton = playerRoleClass.ReportButtonSprite;

        // CustomButton.Get("Paranoid"); for Paranoid

    EndOfSelectImg:

        __instance.KillButton.graphic.sprite = newKillButton;
        __instance.AbilityButton.graphic.sprite = newAbilityButton;
        __instance.AbilityButton.graphic.SetCooldownNormalizedUvs();
        __instance.ImpostorVentButton.graphic.sprite = newVentButton;
        __instance.ReportButton.graphic.sprite = newReportButton;

    }
}
