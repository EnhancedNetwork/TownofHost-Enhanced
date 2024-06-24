using AmongUs.GameOptions;

namespace TOHE.Roles.Vanilla;

internal class NoisemakerTOHE : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 6230;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();

    public override CustomRoles ThisRoleBase => CustomRoles.Noisemaker;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateVanilla;
    //==================================================================\\

    private static OptionItem ImpostorAlert;
    private static OptionItem AlertDuration;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.NoisemakerTOHE);
        ImpostorAlert = BooleanOptionItem.Create(Id + 2, GeneralOption.NoisemakerBase_ImpostorAlert, true, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.NoisemakerTOHE]);
        AlertDuration = IntegerOptionItem.Create(Id + 3, GeneralOption.NoisemakerBase_AlertDuration, new(1, 20, 1), 10, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.NoisemakerTOHE])
            .SetValueFormat(OptionFormat.Seconds);
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
        AURoleOptions.NoisemakerImpostorAlert = ImpostorAlert.GetBool();
        AURoleOptions.NoisemakerAlertDuration = AlertDuration.GetInt();
    }
}
