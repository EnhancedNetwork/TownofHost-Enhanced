using System.Text;
using TOHE.Modules;
using TOHE.Roles.AddOns;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class TaskManager : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.TaskManager;
    private const int Id = 7200;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.TaskManager);
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateBasic;
    //==================================================================\\

    private static OptionItem CanCompleteTaskAfterDeath;
    private static OptionItem LimitGetsAddOns;
    private static OptionItem CanGetHelpfulAddons;
    private static OptionItem CanGetHarmfulAddons;
    private static OptionItem CanGetMixedAddons;
    private static OptionItem CanSeeAllCompletedTasks;

    private int LastTarget = byte.MaxValue;

    private static List<CustomRoles> Addons = [];
    private static readonly Dictionary<int, byte> Target = [];
    private static readonly Dictionary<TaskTypes, string> VisualTasksCompleted = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.TaskManager);
        CanCompleteTaskAfterDeath = BooleanOptionItem.Create(Id + 2, "TaskManager_OptionCanCompleteTaskAfterDeath", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.TaskManager]);
        LimitGetsAddOns = IntegerOptionItem.Create(Id + 3, "TaskManager_LimitGetsAddOns", new(1, 10, 1), 3, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.TaskManager])
            .SetValueFormat(OptionFormat.Times);
        CanGetHelpfulAddons = BooleanOptionItem.Create(Id + 4, "TaskManager_OptionCanGetHelpfulAddons", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.TaskManager]);
        CanGetHarmfulAddons = BooleanOptionItem.Create(Id + 5, "TaskManager_OptionCanGetHarmfulAddons", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.TaskManager]);
        CanGetMixedAddons = BooleanOptionItem.Create(Id + 6, "TaskManager_OptionCanGetMixedAddons", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.TaskManager]);
        CanSeeAllCompletedTasks = BooleanOptionItem.Create(Id + 7, "TaskManager_OptionCanSeeAllCompletedTasks", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.TaskManager]);
        OverrideTasksData.Create(Id + 10, TabGroup.CrewmateRoles, CustomRoles.TaskManager);
    }
    public override void Init()
    {
        Addons.Clear();
        Target.Clear();
        VisualTasksCompleted.Clear();

        LastTarget = byte.MaxValue;

        if (CanGetHelpfulAddons.GetBool())
        {
            Addons.AddRange(GroupedAddons[AddonTypes.Helpful]);
        }
        if (CanGetHarmfulAddons.GetBool())
        {
            Addons.AddRange(GroupedAddons[AddonTypes.Harmful]);
        }
        if (CanGetMixedAddons.GetBool())
        {
            Addons.AddRange(GroupedAddons[AddonTypes.Mixed]);
        }

        Addons = Addons.Where(role => role.GetMode() != 0).Shuffle().ToList();
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(LimitGetsAddOns.GetInt());
    }
    public override void Remove(byte playerId)
    {
        Target.Remove(LastTarget);
        LastTarget = byte.MaxValue;
    }
    public override bool OnTaskComplete(PlayerControl taskManager, int completedTaskCount, int totalTaskCount)
    {
        if (!taskManager.IsAlive() && !CanCompleteTaskAfterDeath.GetBool()) return true;

        var randomPlayer = Main.AllAlivePlayerControls.Where(pc => taskManager.PlayerId != pc.PlayerId && pc.Is(Custom_Team.Crewmate) && Utils.HasTasks(pc.Data, false)).ToList().RandomElement();

        if (randomPlayer == null)
        {
            taskManager.Notify(GetString("TaskManager_FailCompleteRandomTasks"));
            return true;
        }

        var allNotCompletedTasks = randomPlayer.Data.Tasks.ToArray().Where(pcTask => !pcTask.Complete).ToList();

        if (allNotCompletedTasks.Count > 0)
        {
            LastTarget = randomPlayer.PlayerId;
            Target[LastTarget] = taskManager.PlayerId;
            randomPlayer.RpcCompleteTask(allNotCompletedTasks.RandomElement().Id);

            taskManager.Notify(GetString("TaskManager_YouCompletedRandomTask"));
            randomPlayer.Notify(GetString("TaskManager_CompletedRandomTaskForPlayer"));
        }
        return true;
    }
    public override void OnOthersTaskComplete(PlayerControl player, PlayerTask task, bool playerIsOverridden, PlayerControl realPlayer)
    {
        if (!_Player.IsAlive() || !_Player.Is(CustomRoles.TaskManager)) return;

        if (!playerIsOverridden)
            VisualTaskIsCompleted(task.TaskType);

        var abilityLimit = _Player.GetAbilityUseLimit();
        if (realPlayer.PlayerId == _Player.PlayerId || !realPlayer.GetPlayerTaskState().IsTaskFinished || abilityLimit < 1) return;

        var taskManager = _Player;

        Addons.RemoveAll(taskManager.Is);
        taskManager.CheckConflictedAddOnsFromList(ref Addons);

        if (Addons.Count == 0)
        {
            taskManager.Notify(GetString("TaskManager_FailGetAddon"), time: 10);
        }
        else
        {
            abilityLimit--;
            taskManager.RpcRemoveAbilityUse();
            var randomAddOn = Addons.RandomElement();

            taskManager.RpcSetCustomRole(randomAddOn, false, false);
            taskManager.Notify(string.Format(GetString("TaskManager_YouGetAddon"), abilityLimit), time: 10);
        }
    }
    public static bool GetTaskManager(byte targetId, out byte taskManager)
    {
        taskManager = Target.GetValueOrDefault(targetId, byte.MaxValue);
        return taskManager != byte.MaxValue;
    }
    public static void ClearData(byte targetId)
    {
        Target[targetId] = byte.MaxValue;
    }
    private static void VisualTaskIsCompleted(TaskTypes taskType)
    {
        if (VisualTasksCompleted.ContainsKey(taskType)) return;

        if (!(taskType is
            TaskTypes.EmptyGarbage or
            TaskTypes.PrimeShields or
            TaskTypes.ClearAsteroids or
            TaskTypes.SubmitScan)) return;

        var taskTypeStr = taskType.ToString();
        switch (taskType)
        {
            case TaskTypes.EmptyGarbage:
                taskTypeStr = "ClearGarbage";
                break;
            case TaskTypes.PrimeShields:
                taskTypeStr = "ActivatingShields";
                break;
            case TaskTypes.ClearAsteroids:
                taskTypeStr = "ShootingAtAsteroids";
                break;
            case TaskTypes.SubmitScan:
                taskTypeStr = "MedbayScan";
                break;
        }

        VisualTasksCompleted[taskType] = taskTypeStr;
    }
    public override void MeetingHudClear() => VisualTasksCompleted.Clear();
    public override void OnMeetingHudStart(PlayerControl pc)
    {
        if (!pc.IsAlive()) return;

        if (VisualTasksCompleted.Count > 0)
            MeetingHudStartPatch.AddMsg(string.Format(GetString("TaskManager_ListCompletedVisualTasksMessage"), GetVisualTaskList()), pc.PlayerId, ColorString(GetRoleColor(CustomRoles.TaskManager), GetString("TaskManager").ToUpper()));
        else
            MeetingHudStartPatch.AddMsg(GetString("TaskManager_NoOneCompletedVisualTasksMessage"), pc.PlayerId, ColorString(GetRoleColor(CustomRoles.TaskManager), GetString("TaskManager").ToUpper()));
    }
    private static string GetVisualTaskList() => string.Join(", ", VisualTasksCompleted.Values.Select(str => GetString(str)));

    public override string GetProgressText(byte playerId, bool comms)
    {
        if (!CanSeeAllCompletedTasks.GetBool()) return string.Empty;

        var ProgressText = new StringBuilder();
        var TextColor = GetRoleColor(CustomRoles.TaskManager);

        ProgressText.Append(GetTaskCount(playerId, comms));
        ProgressText.Append(ColorString(TextColor, ColorString(Color.white, " - ") + $"({GameData.Instance.CompletedTasks}/{GameData.Instance.TotalTasks})"));
        return ProgressText.ToString();
    }
}
