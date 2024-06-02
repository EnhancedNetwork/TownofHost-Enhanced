using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Phantom : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 14900;
    private static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();
    
    public override CustomRoles ThisRoleBase => PhantomCanVent.GetBool() ? CustomRoles.Engineer : CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralChaos;
    //==================================================================\\
    public override bool HasTasks(GameData.PlayerInfo player, CustomRoles role, bool ForRecompute) => !ForRecompute;

    private static OptionItem PhantomCanVent;
    public static OptionItem PhantomSnatchesWin;
    public static OptionItem PhantomCanGuess;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(14900, TabGroup.NeutralRoles, CustomRoles.Phantom);
        PhantomCanVent = BooleanOptionItem.Create(14902, "CanVent", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Phantom]);
        PhantomSnatchesWin = BooleanOptionItem.Create(14903, "SnatchesWin", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Phantom]);
        PhantomCanGuess = BooleanOptionItem.Create(14904, "CanGuess", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Phantom]);
        OverrideTasksData.Create(14905, TabGroup.NeutralRoles, CustomRoles.Phantom);
    }
    public override void Init()
    {
        PlayerIds.Clear();
    }
    public override void Add(byte playerId)
    {
        PlayerIds.Add(playerId);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = 1f;
        AURoleOptions.EngineerInVentMaxTime = 0f;
    }
}
