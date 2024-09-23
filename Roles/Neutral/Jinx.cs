using AmongUs.GameOptions;
using TOHE.Roles.Core;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Jinx : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 16800;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Jinx);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem CanVent;
    private static OptionItem HasImpostorVision;
    private static OptionItem JinxSpellTimes;
    private static OptionItem killAttacker;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Jinx, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jinx])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jinx]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jinx]);
        JinxSpellTimes = IntegerOptionItem.Create(Id + 14, "JinxSpellTimes", new(1, 15, 1), 3, TabGroup.NeutralRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Jinx])
        .SetValueFormat(OptionFormat.Times);
        killAttacker = BooleanOptionItem.Create(Id + 15, GeneralOption.KillAttackerWhenAbilityRemaining, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jinx]);

    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(JinxSpellTimes.GetInt());
    }
    private static bool CanJinx(byte id) => id.GetAbilityUseLimit() > 0;
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (!CanJinx(killer.PlayerId) || killer.IsTransformedNeutralApocalypse()) return true;
        if (killer == target) return true;
        
        killer.RpcGuardAndKill(target);
        target.RpcGuardAndKill(target);

        killer.RpcRemoveAbilityUse();

        if (killAttacker.GetBool() && target.RpcCheckAndMurder(killer, true))
        {
            killer.SetDeathReason(PlayerState.DeathReason.Jinx);
            killer.RpcMurderPlayer(killer);
            killer.SetRealKiller(target);
        }
        return false;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte babushka) => opt.SetVision(HasImpostorVision.GetBool());

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl player) => CanVent.GetBool();
}
