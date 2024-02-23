using System;
using System.Collections.Generic;

namespace TOHE.Roles.Impostor;

internal class Capitalist : RoleBase
{
    private const int Id = 1;

    public static bool On;
    public override bool IsEnable => On;

    private static Dictionary<byte, int> CapitalistAddTask = [];
    private static Dictionary<byte, int> CapitalistAssignTask = [];

    public static void SetupCustomOption()
    {

    }
    public override void Init()
    {
        CapitalistAddTask = [];
        CapitalistAssignTask = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        On = true;
    }

    public override void SetAbilityButtonText(HudManager hud, byte id) => hud.KillButton.OverrideText(Translator.GetString("CapitalismButtonText"));

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!CapitalistAddTask.ContainsKey(target.PlayerId))
            CapitalistAddTask.Add(target.PlayerId, 0);

        CapitalistAddTask[target.PlayerId]++;

        if (!CapitalistAssignTask.ContainsKey(target.PlayerId))
            CapitalistAssignTask.Add(target.PlayerId, 0);

        CapitalistAssignTask[target.PlayerId]++;

        Logger.Info($"Capitalist: {killer.GetRealName()} add task：{target.GetRealName()}", "Capitalism Add Task");

        if (!Options.DisableShieldAnimations.GetBool())
            killer.RpcGuardAndKill(killer);

        killer.SetKillCooldown();
        return false;
    }

    public static bool OnAddTask(PlayerControl player)
    {
        // Capitalist add task
        if (CapitalistAddTask.TryGetValue(player.PlayerId, out var task))
        {
            var taskState = player.GetPlayerTaskState();
            taskState.AllTasksCount += task;

            CapitalistAddTask.Remove(player.PlayerId);
            taskState.CompletedTasksCount++;

            GameData.Instance.RpcSetTasks(player.PlayerId, Array.Empty<byte>()); // Redistribute tasks
            player.SyncSettings();
            
            Utils.NotifyRoles(SpecifySeer: player);
            return true;
        }

        return false;
    }

    public static void OnTaskAssign(PlayerControl player, ref int NumShortTasks)
    {
        if (player != null && !CapitalistAssignTask.ContainsKey(player.PlayerId)) return;
        
        NumShortTasks += CapitalistAssignTask[player.PlayerId];
        CapitalistAssignTask.Remove(player.PlayerId);
    }
}
