using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Mutant : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Mutant;
    private const int Id = 34800;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\


    private static OptionItem ShapeshiftCD;
    private static OptionItem ShapeshiftDur;
    private static OptionItem KillCooldown;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Mutant, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mutant])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftCD = FloatOptionItem.Create(Id + 15, GeneralOption.ShapeshifterBase_ShapeshiftCooldown, new(1f, 180f, 1f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mutant])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftDur = FloatOptionItem.Create(Id + 16, GeneralOption.ShapeshifterBase_ShapeshiftDuration, new(1f, 180f, 1f), 25f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mutant])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCD.GetFloat();
        AURoleOptions.ShapeshifterDuration = ShapeshiftDur.GetFloat();
    }
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => true;
}
