using TOHE.Modules;
using static TOHE.Options;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.Crewmate;

internal class Sentinel : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Sentinel;
    private const int Id = 33700;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    private static OptionItem AbilityUses;


    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Sentinel);
        AbilityUses = IntegerOptionItem.Create(Id + 10, "AbilityUses337", new(1, 5, 1), 2, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sentinel]).SetValueFormat(OptionFormat.Times);
        SentinelAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 11, "AbilityUseGainWithEachTaskCompleted", new(0f, 2f, 0.5f), 1f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sentinel])
            .SetValueFormat(OptionFormat.Times);
        OverrideTasksData.Create(Id + 20, TabGroup.CrewmateRoles, CustomRoles.Sentinel);
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(AbilityUses.GetInt());
    }
    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody, PlayerControl killer)
    {
        if (Main.UnreportableBodies.Contains(deadBody.PlayerId)) return false;

        if (reporter.GetAbilityUseLimit() < 1)
        {
            return true;
        }
        reporter.RpcRemoveAbilityUse();

        if (reporter.Is(CustomRoles.Sentinel))
        {
            if (killer != null)
            {
                reporter.RpcMurderPlayer(killer);
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

        return true;
    }
}
