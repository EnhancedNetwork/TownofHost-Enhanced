namespace TOHE.Roles.Neutral;

internal class Useless : RoleBase
{
    public override CustomRoles Role => CustomRoles.Useless;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
}
