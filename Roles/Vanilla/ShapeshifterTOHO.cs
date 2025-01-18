using AmongUs.GameOptions;

namespace TOHE.Roles.Vanilla;

internal class ShapeshifterTOHE : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.ShapeshifterTOHO;
    private const int Id = 400;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorVanilla;
    //==================================================================\\

    private static OptionItem ShapeshiftCooldown;
    private static OptionItem ShapeshiftDuration;
    private static OptionItem LeaveShapeshiftingEvidence;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.ShapeshifterTOHO);
        ShapeshiftCooldown = IntegerOptionItem.Create(Id + 2, GeneralOption.ShapeshifterBase_ShapeshiftCooldown, new(1, 180, 1), 15, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.ShapeshifterTOHO])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftDuration = IntegerOptionItem.Create(Id + 3, GeneralOption.ShapeshifterBase_ShapeshiftDuration, new(1, 180, 1), 30, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.ShapeshifterTOHO])
            .SetValueFormat(OptionFormat.Seconds);
        LeaveShapeshiftingEvidence = BooleanOptionItem.Create(Id + 4, GeneralOption.ShapeshifterBase_LeaveShapeshiftingEvidence, false, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.ShapeshifterTOHO]);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetInt();
        AURoleOptions.ShapeshifterDuration = ShapeshiftDuration.GetInt();
        AURoleOptions.ShapeshifterLeaveSkin = LeaveShapeshiftingEvidence.GetBool();
    }
}
