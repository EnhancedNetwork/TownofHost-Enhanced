using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TOHE.Roles.Core;
using UnityEngine;

namespace TOHE;

public static class CustomButton
{
    public static Sprite Get(string name) => Utils.LoadSprite($"TOHE.Resources.Images.Skills.{name}.png", 115f);
}

[HarmonyPriority(Priority.First)]
[HarmonyPatch(typeof(HudManager), nameof(HudManager.SetHudActive))]
[HarmonyPatch([typeof(PlayerControl), typeof(RoleBehaviour), typeof(bool)])]
public static class HudSpritePatch
{
    private static Sprite OriginalKill;
    public static Sprite OriginalAbility;
    private static Sprite OriginalImpVent;
    private static Sprite OriginalReport;
    private static Sprite OriginalSabotage;
    public static void Postfix(HudManager __instance, [HarmonyArgument(0)] PlayerControl localPlayer, [HarmonyArgument(2)] bool isActive)
    {
        if (!Main.EnableCustomButton.Value || __instance == null || !isActive || !localPlayer.IsAlive()) return;
        if (GameStates.IsEnded || GameStates.IsLobby || GameStates.IsHideNSeek || !GameStates.IsModHost) return;

        if (!AmongUsClient.Instance.IsGameStarted || !Main.IntroDestroyed)
        {
            OriginalKill = __instance.KillButton.graphic.sprite;
            OriginalAbility = __instance.AbilityButton.graphic.sprite;
            OriginalImpVent = __instance.ImpostorVentButton.graphic.sprite;
            OriginalReport = __instance.ReportButton.graphic.sprite;
            OriginalSabotage = __instance.SabotageButton.graphic.sprite;
            return;
        }
        OriginalKill ??= __instance.KillButton.graphic.sprite;
        OriginalAbility ??= __instance.AbilityButton.graphic.sprite;
        OriginalImpVent ??= __instance.ImpostorVentButton.graphic.sprite;
        OriginalReport ??= __instance.ReportButton.graphic.sprite;
        OriginalSabotage ??= __instance.SabotageButton.graphic.sprite;

        bool shapeshifting = Main.CheckShapeshift.GetValueOrDefault(localPlayer.PlayerId, false);

        Sprite newKillButton = OriginalKill;
        Sprite newAbilityButton = OriginalAbility;
        Sprite newVentButton = OriginalImpVent;
        Sprite newReportButton = OriginalReport;
        Sprite newSabotageButton = OriginalSabotage;

        var playerRoleClass = localPlayer.GetRoleClass();
        if (playerRoleClass == null) goto EndOfSelectImg;

        if (playerRoleClass?.GetKillButtonSprite(localPlayer, shapeshifting) is Sprite killbutton)
            newKillButton = killbutton;

        if (playerRoleClass?.ImpostorVentButtonSprite(localPlayer) is Sprite Ventbutton)
            newVentButton = Ventbutton;

        if (playerRoleClass?.GetAbilityButtonSprite(localPlayer, shapeshifting) is Sprite Abilitybutton)
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
[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.SetFromSettings))]
public static class AbilityButtonPatch
{
    public static bool Prefix(AbilityButton __instance, AbilityButtonSettings settings)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (!localPlayer.IsAlive() || localPlayer.Data.IsDead) return true;

        __instance.SetInfiniteUses();
        if (localPlayer.GetRoleClass()?.GetAbilityButtonSprite(localPlayer, Main.CheckShapeshift.GetValueOrDefault(localPlayer.PlayerId, false)) is Sprite Abilitybutton)
        {
            __instance.graphic.sprite = Abilitybutton;
        }
        else
        {
            __instance.graphic.sprite = settings.Image ?? HudSpritePatch.OriginalAbility;
        }
        __instance.graphic.SetCooldownNormalizedUvs();
        __instance.buttonLabelText.fontSharedMaterial = settings.FontMaterial;
        __instance.buttonLabelText.text = DestroyableSingleton<TranslationController>.Instance.GetString(settings.Text, new Il2CppReferenceArray<Il2CppSystem.Object>(0));
        return false;
    }
}
