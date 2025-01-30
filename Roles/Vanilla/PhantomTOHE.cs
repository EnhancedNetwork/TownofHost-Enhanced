using AmongUs.GameOptions;

namespace TOHE.Roles.Vanilla;

internal class PhantomTOHE : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.PhantomTOHE;
    private const int Id = 450;
    public override CustomRoles ThisRoleBase => CustomRoles.Phantom;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorVanilla;
    //==================================================================\\

    private static OptionItem InvisCooldown;
    private static OptionItem InvisDuration;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.PhantomTOHE);
        InvisCooldown = IntegerOptionItem.Create(Id + 2, GeneralOption.PhantomBase_InvisCooldown, new(1, 180, 1), 15, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.PhantomTOHE])
            .SetValueFormat(OptionFormat.Seconds);
        InvisDuration = IntegerOptionItem.Create(Id + 3, GeneralOption.PhantomBase_InvisDuration, new(5, 180, 5), 30, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.PhantomTOHE])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.PhantomCooldown = InvisCooldown.GetInt();
        AURoleOptions.PhantomDuration = InvisDuration.GetInt();
    }
}
