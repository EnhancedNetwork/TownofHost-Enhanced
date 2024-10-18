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
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.TaskManager);

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
