
namespace TOHE.Roles.Vanilla;

internal class ImpostorTOHE : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 300;
    
    

    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorVanilla;
    //==================================================================\\

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.ImpostorTOHE);
    }

    public override void Init()
    {
        
    }
    public override void Add(byte playerId)
    {
        
    }
}
