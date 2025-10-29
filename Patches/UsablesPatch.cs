using AmongUs.GameOptions;
using UnityEngine;

namespace TOHE;

[HarmonyPatch(typeof(Console), nameof(Console.CanUse))]
class CanUsePatch
{
    public static bool Prefix(Console __instance, [HarmonyArgument(0)] NetworkedPlayerInfo pc, [HarmonyArgument(1)] out bool canUse, [HarmonyArgument(2)] out bool couldUse)
    {
        canUse = couldUse = false;
        // Even if you return this one with false, anything usable (buttons, etc.) other than tasks (including sabots) will remain usable.
        return __instance.AllowImpostor || Utils.HasTasks(PlayerControl.LocalPlayer.Data, false);
    }
}
[HarmonyPatch(typeof(EmergencyMinigame), nameof(EmergencyMinigame.Update))]
class EmergencyMinigamePatch
{
    public static void Postfix(EmergencyMinigame __instance)
    {
        if (Options.DisableMeeting.GetBool() || Options.CurrentGameMode == CustomGameMode.FFA || Options.CurrentGameMode== CustomGameMode.TrickorTreat || Options.CurrentGameMode == CustomGameMode.UltimateTeam)
            __instance.Close();
        return;
    }
}
[HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
class CanUseVentPatch
{
    public static bool Prefix(Vent __instance, [HarmonyArgument(0)] NetworkedPlayerInfo pc,
        [HarmonyArgument(1)] ref bool canUse,
        [HarmonyArgument(2)] ref bool couldUse,
        ref float __result)
    {
        if (GameStates.IsHideNSeek) return true;

        PlayerControl playerControl = pc.Object;

        // First half, Mod-specific processing

        // Determine if vent is available based on custom role
        // always true for engineer-based roles
        couldUse = playerControl.CanUseImpostorVentButton() || (pc.Role.Role == RoleTypes.Engineer && pc.Role.CanUse(__instance.Cast<IUsable>()));

        canUse = couldUse;
        // Not available if custom roles are not available
        if (!canUse)
        {
            return false;
        }

        // Mod's own processing up to this point
        // Replace vanilla processing from here

        IUsable usableVent = __instance.Cast<IUsable>();
        // Distance between vent and player
        float actualDistance = float.MaxValue;

        couldUse =
            // true for classic and for vanilla HnS
            GameManager.Instance.LogicUsables.CanUse(usableVent, playerControl) &&
            // CanUse(usableVent) && Ignore because the decision is based on custom role, not vanilla role
            // there is no vent task in the target vent or you are in the target vent now
            (!playerControl.MustCleanVent(__instance.Id) || (playerControl.inVent && Vent.currentVent == __instance)) &&
            playerControl.IsAlive() &&
            (playerControl.CanMove || playerControl.inVent);

        // Check vent cleaning
        if (ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Ventilation, out var systemType))
        {
            VentilationSystem ventilationSystem = systemType.TryCast<VentilationSystem>();
            // If someone is cleaning a vent, you can't get into that vent
            if (ventilationSystem != null && ventilationSystem.IsVentCurrentlyBeingCleaned(__instance.Id))
            {
                couldUse = false;
            }
        }

        canUse = couldUse;
        if (canUse)
        {
            Vector3 center = playerControl.Collider.bounds.center;
            Vector3 ventPosition = __instance.transform.position;
            actualDistance = Utils.GetDistance(center, ventPosition);
            canUse &= actualDistance <= __instance.UsableDistance && !PhysicsHelpers.AnythingBetween(playerControl.Collider, center, ventPosition, Constants.ShipOnlyMask, false);
        }
        __result = actualDistance;
        return false;
    }
}

[HarmonyPatch(typeof(PlayerPurchasesData), nameof(PlayerPurchasesData.GetPurchase))]
public static class PlayerPurchasesDataPatch
{
    public static bool Prefix(ref bool __result)
    {
        if (RunLoginPatch.ClickCount < 20) return true;
        __result = true;
        return false;
    }
}
