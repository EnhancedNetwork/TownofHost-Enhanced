using TOHE.Roles.Core;

namespace TOHE;

[HarmonyPatch(typeof(OneWayShadows), nameof(OneWayShadows.IsIgnored))]
public static class OneWayShadowsIsIgnoredPatch
{
    public static bool Prefix(OneWayShadows __instance, ref bool __result)
    {
        var amDesyncImpostor = PlayerControl.LocalPlayer.HasDesyncRole() || Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].IsNecromancer;

        if (__instance.IgnoreImpostor && amDesyncImpostor)
        {
            __result = true;
            return false;
        }
        return true;
    }
}

