
using AmongUs.GameOptions;

namespace TOHE.Roles.Vanilla;

internal class EngineerTOHE : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 6100;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateVanilla;
    //==================================================================\\

    private static OptionItem EngineerCD;
    private static OptionItem EngineerInVentTime;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.EngineerTOHE);
        EngineerCD = FloatOptionItem.Create(Id + 2, "VentCooldown", new(0f, 250f, 2.5f), 15f, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.EngineerTOHE])
            .SetValueFormat(OptionFormat.Seconds);
        EngineerInVentTime = FloatOptionItem.Create(Id + 3, "InVentMaxTime", new(0f, 250f, 2.5f), 15f, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.EngineerTOHE])
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
        AURoleOptions.EngineerCooldown = EngineerCD.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = EngineerInVentTime.GetFloat();
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = 300f;
}
