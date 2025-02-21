using AmongUs.GameOptions;

namespace TOHE.Roles.Impostor;

internal class Parasite : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Parasite;
    private const int Id = 5900;

    public override CustomRoles ThisRoleBase => LegacyParasite.GetBool() ? CustomRoles.Shapeshifter : CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.Madmate;
    //==================================================================\\

    private static OptionItem ParasiteCD;
    private static OptionItem LegacyParasite;
    private static OptionItem ParasiteShapeshiftCD;
    private static OptionItem ParasiteShapeshiftDur;

    public override void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Parasite, zeroOne: false);
        ParasiteCD = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Parasite])
            .SetValueFormat(OptionFormat.Seconds);
        LegacyParasite = BooleanOptionItem.Create(Id + 11, "LegacyParasite", false, TabGroup.ImpostorRoles, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Parasite]);
        ParasiteShapeshiftCD = FloatOptionItem.Create(Id + 12, GeneralOption.ShapeshifterBase_ShapeshiftCooldown, new(1f, 180f, 1f), 15f, TabGroup.ImpostorRoles, false)
                .SetParent(LegacyParasite)
                .SetValueFormat(OptionFormat.Seconds);
        ParasiteShapeshiftDur = FloatOptionItem.Create(Id + 13, GeneralOption.ShapeshifterBase_ShapeshiftDuration, new(1f, 180f, 1f), 30f, TabGroup.ImpostorRoles, false)
                .SetParent(LegacyParasite)
                .SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        opt.SetVision(true);
        AURoleOptions.ShapeshifterCooldown = ParasiteShapeshiftCD.GetFloat();
        AURoleOptions.ShapeshifterDuration = ParasiteShapeshiftDur.GetFloat();
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = ParasiteCD.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => true;
    public override bool CanUseSabotage(PlayerControl pc) => true;
}
