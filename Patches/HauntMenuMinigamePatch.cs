using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;

namespace TOHE.Patches;

[HarmonyPatch(typeof(HauntMenuMinigame), nameof(HauntMenuMinigame.SetFilterText))]
public static class HauntMenuMinigameSetFilterTextPatch
{
    public static bool Prefix(HauntMenuMinigame __instance)
    {
        if (__instance.HauntTarget != null && DeadKnowRole(PlayerControl.LocalPlayer) && GameStates.IsNormalGame)
        {
            // Override job title display with custom role name
            __instance.FilterText.text = Utils.GetDisplayRoleAndSubName(PlayerControl.LocalPlayer.PlayerId, __instance.HauntTarget.PlayerId, false, false);
            return false;
        }
        return true;

        static bool DeadKnowRole(PlayerControl seer)
        {
            if (Main.VisibleTasksCount && !seer.IsAlive())
            {
                if (Nemesis.PreventKnowRole(seer)) return false;
                if (Retributionist.PreventKnowRole(seer)) return false;

                if (!Options.GhostCanSeeOtherRoles.GetBool())
                    return false;
                else if (Options.PreventSeeRolesImmediatelyAfterDeath.GetBool() && !Main.DeadPassedMeetingPlayers.Contains(seer.PlayerId))
                    return false;
                return true;
            }
            return false;
        }
    }
}
