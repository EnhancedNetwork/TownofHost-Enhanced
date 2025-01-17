namespace TOHE.Roles.Impostor;

internal class Trickster : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Trickster;
    private const int Id = 4800;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Trickster);
    }
}
