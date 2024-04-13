
using AmongUs.GameOptions;

namespace TOHE.Roles.Vanilla;

internal class EngineerTOHE : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 6100;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateVanilla;
    //==================================================================\\

    public static void SetupCustomOptions()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.EngineerTOHE);
    }

    public override void Init()
    {
        playerIdList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = 0f;
        AURoleOptions.EngineerInVentMaxTime = 0f;
    }
}
