using TOHE.Roles.Core;

namespace TOHE.Roles.Vanilla;

internal class NoisemakerTOHE : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.NoisemakerTOHO;
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
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.NoisemakerTOHO);
        ImpostorAlert = BooleanOptionItem.Create(Id + 2, GeneralOption.NoisemakerBase_ImpostorAlert, true, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.NoisemakerTOHO]);
        AlertDuration = IntegerOptionItem.Create(Id + 3, GeneralOption.NoisemakerBase_AlertDuration, new(1, 20, 1), 10, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.NoisemakerTOHO])
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

    public static void ApplyGameOptionsForOthers(PlayerControl player)
    {
        AURoleOptions.NoisemakerAlertDuration = AlertDuration.GetInt();

        var playerRole = player.GetCustomRole();
        // When impostor alert is off, and player is desync crewamte, make impostor alert as true
        if (player.HasDesyncRole() && !playerRole.IsImpostorTeamV3() && !ImpostorAlert.GetBool())
        {
            AURoleOptions.NoisemakerImpostorAlert = true;
        }
        else
        {
            AURoleOptions.NoisemakerImpostorAlert = ImpostorAlert.GetBool();
        }
    }
}
