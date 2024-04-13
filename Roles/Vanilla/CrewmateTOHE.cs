
namespace TOHE.Roles.Vanilla;

internal class CrewmateTOHE : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 6000;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateVanilla;
    //==================================================================\\

    public static void SetupCustomOptions()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.CrewmateTOHE);
    }

    public override void Init()
    {
        playerIdList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
}
