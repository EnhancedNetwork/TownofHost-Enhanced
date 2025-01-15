using System.Text;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class TaskManager : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.TaskManager;
    private const int Id = 7200;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateBasic;
    //==================================================================\\

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.TaskManager);
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
