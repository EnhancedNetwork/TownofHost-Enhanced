namespace TOHE.Roles.Neutral;

internal class Bankrupt : RoleBase
{
    public override CustomRoles Role => CustomRoles.Bankrupt;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
}
