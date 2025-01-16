using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Traitor : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Traitor;
    private const int Id = 18200;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => LegacyTraitor.GetBool() ? CustomRoles.Shapeshifter : CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem CanVent;
    private static OptionItem HasImpostorVision;
    private static OptionItem CanUsesSabotage;
    public static OptionItem KnowMadmate;
    private static OptionItem LegacyTraitor;
    private static OptionItem TraitorShapeshiftCD;
    private static OptionItem TraitorShapeshiftDur;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Traitor, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Traitor])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Traitor]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Traitor]);
        CanUsesSabotage = BooleanOptionItem.Create(Id + 15, GeneralOption.CanUseSabotage, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Traitor]);
        KnowMadmate = BooleanOptionItem.Create(Id + 16, "TraitorKnowMadmate", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Traitor]);
        LegacyTraitor = BooleanOptionItem.Create(Id + 17, "LegacyTraitor", false, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Traitor]);
        TraitorShapeshiftCD = FloatOptionItem.Create(Id + 19, GeneralOption.ShapeshifterBase_ShapeshiftCooldown, new(1f, 180f, 1f), 15f, TabGroup.NeutralRoles, false)
                .SetParent(LegacyTraitor)
                .SetValueFormat(OptionFormat.Seconds);
        TraitorShapeshiftDur = FloatOptionItem.Create(Id + 21, GeneralOption.ShapeshifterBase_ShapeshiftDuration, new(1f, 180f, 1f), 30f, TabGroup.NeutralRoles, false)
                .SetParent(LegacyTraitor)
                .SetValueFormat(OptionFormat.Seconds);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        opt.SetVision(HasImpostorVision.GetBool());
        AURoleOptions.ShapeshifterCooldown = TraitorShapeshiftCD.GetFloat();
        AURoleOptions.ShapeshifterDuration = TraitorShapeshiftDur.GetFloat();
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();
    public override bool CanUseSabotage(PlayerControl pc) => CanUsesSabotage.GetBool();

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        return !(target == killer || target.Is(Custom_Team.Impostor));
    }
    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target)
    {
        if (Main.PlayerStates[seer.PlayerId].IsNecromancer || Main.PlayerStates[target.PlayerId].IsNecromancer) return string.Empty;
        if (target.Is(Custom_Team.Impostor))
        {
            return Main.roleColors[CustomRoles.Impostor];
        }
        else if (target.Is(CustomRoles.Madmate) && KnowMadmate.GetBool())
        {
            return "BB0F0F";
        }

        else return string.Empty;

    }
}
