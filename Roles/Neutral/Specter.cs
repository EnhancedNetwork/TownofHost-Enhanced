using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Specter : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 14900;
    private static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();
    
    public override CustomRoles ThisRoleBase => CanVent.GetBool() ? CustomRoles.Engineer : CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralChaos;
    //==================================================================\\
    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute) => !ForRecompute;

    private static OptionItem CanVent;
    public static OptionItem SnatchesWin;
    public static OptionItem CanGuess;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(14900, TabGroup.NeutralRoles, CustomRoles.Specter);
        CanVent = BooleanOptionItem.Create(14902, "CanVent", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Specter]);
        SnatchesWin = BooleanOptionItem.Create(14903, "SpecterSnatchesWin", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Specter]);
        CanGuess = BooleanOptionItem.Create(14904, "CanGuess", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Specter]);
        OverrideTasksData.Create(14905, TabGroup.NeutralRoles, CustomRoles.Specter);
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
