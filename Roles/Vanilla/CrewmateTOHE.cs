
namespace TOHE.Roles.Vanilla;

internal class CrewmateTOHE : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 6000;



    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateVanilla;
    //==================================================================\\

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.CrewmateTOHE);
    }

    public override void Init()
    {

    }
    public override void Add(byte playerId)
    {

    }
}
