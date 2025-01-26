using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;
internal class Investor : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Investor;
    private const int Id = 33300;
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.Madmate;
    //==================================================================\\

    public static OptionItem KillCooldown;
    public static OptionItem ReductionPerTask;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Investor);
        KillCooldown = FloatOptionItem.Create(Id + 10, "InvestorKillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Investor])
            .SetValueFormat(OptionFormat.Seconds);
        ReductionPerTask = FloatOptionItem.Create(Id + 11, "InvestorTaskReduction", new(0f, 10f, 0.5f), 4f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Investor])
            .SetValueFormat(OptionFormat.Seconds);
        OverrideTasksData.Create(Id + 20, TabGroup.ImpostorRoles, CustomRoles.Investor);
    }
    public static float InitialKCD;
    public static float LowerKCD;
    public static float FinalKCD;
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = 1;
    }

    public override void Add(byte playerId)
    {
        InitialKCD = KillCooldown.GetFloat();
        LowerKCD = ReductionPerTask.GetFloat();
        FinalKCD = InitialKCD;
    }

    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        FinalKCD -= LowerKCD;
        return true;
    }
    public override void OnEnterVent(PlayerControl player, Vent vent)
    {
        player.MyPhysics.RpcBootFromVent(vent.Id);
        player.RpcChangeRoleBasis(CustomRoles.Rich);
        player.RpcSetCustomRole(CustomRoles.Rich, true);
    }
    public override bool CanUseKillButton(PlayerControl pc) => false;
    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute) => true;
}

internal class Rich : RoleBase
{
    public override CustomRoles Role => CustomRoles.Rich;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.Madmate;
    public override bool CanUseKillButton(PlayerControl player) => true;
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Investor.FinalKCD;
}
