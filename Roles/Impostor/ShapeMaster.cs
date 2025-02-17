using AmongUs.GameOptions;

namespace TOHE.Roles.Impostor;

internal class ShapeMaster : RoleBase // Should be deleted tbh, because it's litteraly vanilla shapeshifter
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.ShapeMaster;
    private const int Id = 4500;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\

    private static OptionItem ShapeMasterShapeshiftDuration;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.ShapeMaster);
        ShapeMasterShapeshiftDuration = FloatOptionItem.Create(Id + 2, GeneralOption.ShapeshifterBase_ShapeshiftDuration, new(1, 60, 1), 10, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.ShapeMaster])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = 1f;
        AURoleOptions.ShapeshifterLeaveSkin = false;
        AURoleOptions.ShapeshifterDuration = ShapeMasterShapeshiftDuration.GetFloat();
    }
}
