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
    public override string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        var TextColor = GetRoleColor(CustomRoles.TaskManager);

        ProgressText.Append(GetTaskCount(playerId, comms));
        ProgressText.Append(ColorString(TextColor, ColorString(Color.white, " - ") + $"({GameData.Instance.CompletedTasks}/{GameData.Instance.TotalTasks})"));
        return ProgressText.ToString();
    }
}
