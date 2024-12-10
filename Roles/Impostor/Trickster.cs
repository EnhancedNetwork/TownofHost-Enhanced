namespace TOHE.Roles.Impostor;

internal class Trickster : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 4800;



    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Trickster);
    }
    public override void Init()
    {

    }
    public override void Add(byte playerId)
    {

    }
}
