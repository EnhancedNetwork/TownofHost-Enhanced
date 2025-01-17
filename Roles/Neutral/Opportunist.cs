using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Opportunist : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Opportunist;
    private const int Id = 13300;
    public override CustomRoles ThisRoleBase => OpportunistCanUseVent.GetBool() ? CustomRoles.Engineer : CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\
    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute) => OppoImmuneToAttacksWhenTasksDone.GetBool() && !ForRecompute;

    public static OptionItem OppoImmuneToAttacksWhenTasksDone;
    private static OptionItem OpportunistCanUseVent;
    private static OptionItem VentCoolDown;
    private static OptionItem VentDuration;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Opportunist);
        OppoImmuneToAttacksWhenTasksDone = BooleanOptionItem.Create(Id + 10, "ImmuneToAttacksWhenTasksDone", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Opportunist]);
        OpportunistCanUseVent = BooleanOptionItem.Create(Id + 11, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Opportunist]);
        VentCoolDown = FloatOptionItem.Create(Id + 12, GeneralOption.EngineerBase_VentCooldown, new(0f, 60f, 2.5f), 10f, TabGroup.NeutralRoles, false)
            .SetParent(OpportunistCanUseVent);
        VentDuration = FloatOptionItem.Create(Id + 13, GeneralOption.EngineerBase_InVentMaxTime, new(0f, 180f, 2.5f), 15f, TabGroup.NeutralRoles, false)
            .SetParent(OpportunistCanUseVent);
        OverrideTasksData.Create(Id + 20, TabGroup.NeutralRoles, CustomRoles.Opportunist);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte id)
    {
        AURoleOptions.EngineerCooldown = VentCoolDown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = VentDuration.GetFloat();
    }
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
        => !(OppoImmuneToAttacksWhenTasksDone.GetBool() && target.AllTasksCompleted());
}
