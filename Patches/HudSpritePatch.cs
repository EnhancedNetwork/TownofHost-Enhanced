using HarmonyLib;
using TOHE.Roles.Core;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
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

        switch (player.GetCustomRole())
        {
            case CustomRoles.Hangman:
                if (shapeshifting) newAbilityButton = CustomButton.Get("Hangman");
                break;
            case CustomRoles.Paranoia:
                newAbilityButton = CustomButton.Get("Paranoid");
                break;
            case CustomRoles.Puppeteer:
                newKillButton = CustomButton.Get("Puttpuer");
                break;
            case CustomRoles.Succubus:
                newKillButton = CustomButton.Get("Subbus");
                break;
            case CustomRoles.Vampire:
                newKillButton = CustomButton.Get("Bite");
                break;
            case CustomRoles.Witness:
                newKillButton = CustomButton.Get("Examine");
                break;
            case CustomRoles.Amnesiac:
                newReportButton = CustomButton.Get("Amnesiac");
                break;
            case CustomRoles.Mario:
                newAbilityButton = CustomButton.Get("Happy");
                break;
            case CustomRoles.Pirate:
                newKillButton = CustomButton.Get("Challenge");
                break;
            case CustomRoles.SoulCatcher:
                newKillButton = CustomButton.Get("Teleport");
                break;
            case CustomRoles.Swooper:
                newAbilityButton = CustomButton.Get("invisible");
                break;
            case CustomRoles.Chameleon:
                newAbilityButton = CustomButton.Get("invisible");
                break;
            case CustomRoles.Vulture:
                newReportButton = CustomButton.Get("Eat");
                break;
            case CustomRoles.Pursuer:
                newAbilityButton = CustomButton.Get("Pursuer");
                break;
            case CustomRoles.Warlock:
                if (!shapeshifting)
                {
                    newKillButton = CustomButton.Get("Curse");
                    if (Main.isCurseAndKill.TryGetValue(player.PlayerId, out bool curse) && curse)
                        newAbilityButton = CustomButton.Get("CurseKill");
                }
                break;
        }

    EndOfSelectImg:

        __instance.KillButton.graphic.sprite = newKillButton;
        __instance.AbilityButton.graphic.sprite = newAbilityButton;
        __instance.ImpostorVentButton.graphic.sprite = newVentButton;
        __instance.ReportButton.graphic.sprite = newReportButton;

    }
}
