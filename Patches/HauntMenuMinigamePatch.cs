namespace TOHE.Patches;

[HarmonyPatch(typeof(HauntMenuMinigame), nameof(HauntMenuMinigame.SetFilterText))]
public static class HauntMenuMinigameSetFilterTextPatch
{
    public static bool Prefix(HauntMenuMinigame __instance)
    {
        if (__instance.HauntTarget != null && Options.GhostCanSeeOtherRoles.GetBool() && GameStates.IsNormalGame)
        {
            // Override job title display with custom role name
            __instance.FilterText.text = Utils.GetDisplayRoleAndSubName(PlayerControl.LocalPlayer.PlayerId, __instance.HauntTarget.PlayerId, false);
            return false;
        }
        return true;
    }
}
