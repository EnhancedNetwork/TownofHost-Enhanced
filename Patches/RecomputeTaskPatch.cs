namespace TOHE;

[HarmonyPatch(typeof(GameData), nameof(GameData.RecomputeTaskCounts))]
class CustomTaskCountsPatch
{
    public static bool Prefix(GameData __instance)
    {
        if (GameStates.IsHideNSeek) return true;

        __instance.TotalTasks = 0;
        __instance.CompletedTasks = 0;
        foreach (var p in __instance.AllPlayers)
        {
            if (p == null) continue;
            var hasTasks = Utils.HasTasks(p) && Main.PlayerStates[p.PlayerId].TaskState.AllTasksCount > 0;
            if (hasTasks)
            {
                foreach (var task in p.Tasks)
                {
                    __instance.TotalTasks++;
                    if (task.Complete) __instance.CompletedTasks++;
                }
            }
        }

        return false;
    }
}