using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

internal class Sentinel : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Sentinel;
    private const int Id = 33700;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    public static int Uses = 1;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Sentinel);
        OverrideTasksData.Create(Id + 20, TabGroup.CrewmateRoles, CustomRoles.Sentinel);

    }
    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody, PlayerControl killer)
    {
        if (Main.UnreportableBodies.Contains(deadBody.PlayerId)) return false;

        if (Uses == 0)
        {
            return true;
        }

        if (reporter.Is(CustomRoles.Sentinel))
        {
            if (killer != null)
            {
                reporter.RpcMurderPlayer(killer);
                Uses -= 1;
                return false;
            }
            else
            {
                return true;
            }
        }
        return true;
    }
    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (completedTaskCount == totalTaskCount)
            Uses += 1;
        return true;
    }
}
