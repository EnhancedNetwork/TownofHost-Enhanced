
using AmongUs.GameOptions;

namespace TOHE.Roles.Vanilla;

internal class ScientistTOHE : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 6200;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Scientist;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateVanilla;
    //==================================================================\\

    private static OptionItem ScientistCD;
    private static OptionItem ScientistDur;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.ScientistTOHE);
        ScientistCD = FloatOptionItem.Create(Id + 2, "VitalsCooldown", new(1f, 250f, 1f), 3f, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.ScientistTOHE])
            .SetValueFormat(OptionFormat.Seconds);
        ScientistDur = FloatOptionItem.Create(Id + 3, "VitalsDuration", new(1f, 250f, 1f), 15f, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.ScientistTOHE])
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
        AURoleOptions.ScientistCooldown = ScientistCD.GetFloat();
        AURoleOptions.ScientistBatteryCharge = ScientistDur.GetFloat();
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = 300f;
}
