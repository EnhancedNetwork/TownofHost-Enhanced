using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

internal class LazyGuy : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.LazyGuy;
    private const int Id = 6800;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateBasic;
    //==================================================================\\

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.LazyGuy);
    }
}
