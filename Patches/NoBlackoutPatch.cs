namespace TOHE;

[HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.IsGameOverDueToDeath))]
class DontBlackoutPatch
{
    public static void Postfix(ref bool __result)
    {
        __result = false;
    }
}

[HarmonyPatch(typeof(LogicGameFlowHnS), nameof(LogicGameFlowHnS.IsGameOverDueToDeath))]
class DontBlackoutPatch2
{
    public static void Postfix(ref bool __result)
    {
        __result = false;
    }
}

[HarmonyPatch(typeof(GameManager), nameof(GameManager.CheckTaskCompletion))]
class DontBlackoutPatch3
{
    // This stupid function is now only created and used in report dead body, that can end the game unexpectedly
    // Maybe InnerSloth will use it somewhere else in the future, keep this in mind
    public static bool Prefix(ref bool __result)
    {
        __result = false;
        return false;
    }
}
