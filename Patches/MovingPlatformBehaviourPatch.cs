namespace TOHE;

// https://github.com/tukasa0001/TownOfHost/pull/1274/commits/164d1463e46f0ec453e136c7a2f28a8039cd7fc4

[HarmonyPatch(typeof(MovingPlatformBehaviour))]
public static class MovingPlatformBehaviourPatch
{
    private static bool isDisabled = false;

    [HarmonyPatch(nameof(MovingPlatformBehaviour.Start)), HarmonyPrefix]
    public static void Start_Prefix(MovingPlatformBehaviour __instance)
    {
        isDisabled = Options.DisableAirshipMovingPlatform.GetBool();

        if (isDisabled)
        {
            __instance.transform.localPosition = __instance.DisabledPosition;
            ShipStatus.Instance.CastFast<AirshipStatus>().outOfOrderPlat.SetActive(true);
        }
    }
    [HarmonyPatch(nameof(MovingPlatformBehaviour.IsDirty), MethodType.Getter), HarmonyPrefix]
    public static bool GetIsDirty_Prefix(ref bool __result)
    {
        if (isDisabled)
        {
            __result = false;
            return false;
        }
        return true;
    }
    [HarmonyPatch(nameof(MovingPlatformBehaviour.Use), typeof(PlayerControl)), HarmonyPrefix]
    public static bool Use_Prefix() => !isDisabled;
    [HarmonyPatch(nameof(MovingPlatformBehaviour.SetSide)), HarmonyPrefix]
    public static bool SetSide_Prefix() => !isDisabled;
}
