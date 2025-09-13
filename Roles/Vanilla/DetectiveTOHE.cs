using AmongUs.GameOptions;

namespace TOHE.Roles.Vanilla;

internal class DetectiveTOHE : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.DetectiveTOHE;
    private const int Id = 32200;
    public override CustomRoles ThisRoleBase => CustomRoles.Detective;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateVanilla;
    //==================================================================\\

    private static OptionItem DetectiveSuspectLimit;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.DetectiveTOHE);
        DetectiveSuspectLimit = IntegerOptionItem.Create(Id + 2, GeneralOption.DetectiveBase_DetectiveSuspectLimit, new(2, 4, 1), 2, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.DetectiveTOHE])
            .SetValueFormat(OptionFormat.Players);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.DetectiveSuspectLimit = DetectiveSuspectLimit.GetInt();
    }
}
