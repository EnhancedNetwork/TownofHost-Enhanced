using System.Text;
using UnityEngine;
using TOHE.Roles.Core;
using static TOHE.Utils;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

internal class TaskManager : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 7200;
    private static bool On = false;
    public override bool IsEnable => On;
    public static bool HasEnabled => CustomRoles.TaskManager.HasEnabled();
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;

    //==================================================================\\
    public static void SetupCustomOptions()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.TaskManager);
    }
    public override void Init()
    {
        On = false;
    }
    public override void Add(byte playerId)
    {
        On = true;
    }
    public override string GetProgressText(byte PlayerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        var taskState1 = Main.PlayerStates?[PlayerId].TaskState;
        Color TextColor1;
        var TaskCompleteColor1 = Color.green;
        var NonCompleteColor1 = Color.yellow;
        var NormalColor1 = taskState1.IsTaskFinished ? TaskCompleteColor1 : NonCompleteColor1;
        TextColor1 = comms ? Color.gray : NormalColor1;
        string Completed1 = comms ? "?" : $"{taskState1.CompletedTasksCount}";
        string totalCompleted1 = comms ? "?" : $"{GameData.Instance.CompletedTasks}";
        ProgressText.Append(ColorString(TextColor1, $"({Completed1}/{taskState1.AllTasksCount})"));
        ProgressText.Append($" <color=#777777>-</color> <color=#00ffa5>{totalCompleted1}/{GameData.Instance.TotalTasks}</color>");
        return ProgressText.ToString();
    }
}
