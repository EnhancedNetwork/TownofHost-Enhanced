using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Crewmate;

public static class Workhorse
{
    private const int Id = 23730;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool IsEnable = false;

    public static Color RoleColor = Utils.GetRoleColor(CustomRoles.Workhorse);

    private static OptionItem OptionAssignOnlyToCrewmate;
    private static OptionItem OptionNumLongTasks;
    private static OptionItem OptionNumShortTasks;
    private static OptionItem OptionSnitchCanBeWorkhorse;

    private static bool AssignOnlyToCrewmate;
    private static int NumLongTasks;
    private static int NumShortTasks;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Workhorse, zeroOne: true);
        OptionAssignOnlyToCrewmate = BooleanOptionItem.Create(Id + 10, "AssignOnlyToCrewmate", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Workhorse]);
        OptionNumLongTasks = IntegerOptionItem.Create(Id + 11, "WorkhorseNumLongTasks", new(0, 5, 1), 1, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Workhorse])
            .SetValueFormat(OptionFormat.Pieces);
        OptionNumShortTasks = IntegerOptionItem.Create(Id + 12, "WorkhorseNumShortTasks", new(0, 5, 1), 1, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Workhorse])
            .SetValueFormat(OptionFormat.Pieces);
        OptionSnitchCanBeWorkhorse = BooleanOptionItem.Create(Id + 14, "SnitchCanBeWorkhorse", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Workhorse]);
    }
    public static void Init()
    {
        playerIdList.Clear();
        IsEnable = false;

        AssignOnlyToCrewmate = OptionAssignOnlyToCrewmate.GetBool();
        NumLongTasks = OptionNumLongTasks.GetInt();
        NumShortTasks = OptionNumShortTasks.GetInt();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;
    }
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
    public static (bool, int, int) TaskData => (false, NumLongTasks, NumShortTasks);
    private static bool IsAssignTarget(PlayerControl pc)
    {
        if (!pc.IsAlive() || IsThisRole(pc.PlayerId)) return false;
        if (pc.Is(CustomRoles.Snitch) && !OptionSnitchCanBeWorkhorse.GetBool()) return false;
        if (pc.Is(CustomRoles.LazyGuy) || pc.Is(CustomRoles.Lazy)) return false;

        var taskState = pc.GetPlayerTaskState();
        if (taskState.CompletedTasksCount < taskState.AllTasksCount) return false;

        if (AssignOnlyToCrewmate)
            return pc.Is(Custom_Team.Crewmate);

        return Utils.HasTasks(pc.Data) //Player has task
            && !OverrideTasksData.AllData.ContainsKey(pc.GetCustomRole()); //Has the ability to overwrite tasks
    }
    public static bool OnAddTask(PlayerControl pc)
    {
        if (!CustomRoles.Workhorse.IsEnable() || playerIdList.Count >= CustomRoles.Workhorse.GetCount()) return true;
        if (!IsAssignTarget(pc)) return true;

        pc.RpcSetCustomRole(CustomRoles.Workhorse);
        var taskState = pc.GetPlayerTaskState();
        taskState.AllTasksCount += NumLongTasks + NumShortTasks;
        //taskState.CompletedTasksCount++; //Addition for this completion

        if (AmongUsClient.Instance.AmHost)
        {
            Add(pc.PlayerId);
            pc.Data.RpcSetTasks(new Il2CppStructArray<byte>(0)); // Redistribute tasks
            pc.SyncSettings();
            Utils.NotifyRoles(SpecifySeer: pc);
        }

        return false;
    }
}