
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

    private static OptionItem VentUseCooldown;
    private static OptionItem InVentMaxTime;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.EngineerTOHE);
        VentUseCooldown = FloatOptionItem.Create(Id + 2, GeneralOption.EngineerBase_VentCooldown, new(0f, 250f, 2.5f), 15f, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.EngineerTOHE])
            .SetValueFormat(OptionFormat.Seconds);
        InVentMaxTime = FloatOptionItem.Create(Id + 3, GeneralOption.EngineerBase_InVentMaxTime, new(0f, 250f, 2.5f), 15f, TabGroup.CrewmateRoles, false)
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
        AURoleOptions.EngineerCooldown = VentUseCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = InVentMaxTime.GetFloat();
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = 300f;
}
