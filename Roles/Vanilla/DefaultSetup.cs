
namespace TOHE;

internal class DefaultSetup : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.NotAssigned;

    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.None;
    //==================================================================\\

    public override void Init()
    {

    }
    public override void Add(byte playerId)
    {

    }
}
