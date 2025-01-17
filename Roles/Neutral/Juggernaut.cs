using AmongUs.GameOptions;
using System;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Juggernaut : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Juggernaut;
    private const int Id = 16900;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem DefaultKillCooldown;
    private static OptionItem ReduceKillCooldown;
    private static OptionItem MinKillCooldown;
    private static OptionItem HasImpostorVision;
    private static OptionItem CanVent;

    private static readonly Dictionary<byte, float> NowCooldown = [];

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Juggernaut, 1, zeroOne: false);
        DefaultKillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.DefaultKillCooldown, new(0f, 180f, 2.5f), 65f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Juggernaut])
            .SetValueFormat(OptionFormat.Seconds);
        ReduceKillCooldown = FloatOptionItem.Create(Id + 11, GeneralOption.ReduceKillCooldown, new(0f, 180f, 2.5f), 15f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Juggernaut])
            .SetValueFormat(OptionFormat.Seconds);
        MinKillCooldown = FloatOptionItem.Create(Id + 12, GeneralOption.MinKillCooldown, new(0f, 180f, 2.5f), 2.5f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Juggernaut])
            .SetValueFormat(OptionFormat.Seconds);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Juggernaut]);
        CanVent = BooleanOptionItem.Create(Id + 14, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Juggernaut]);
    }
    public override void Init()
    {
        NowCooldown.Clear();
    }
    public override void Add(byte playerId)
    {
        NowCooldown.TryAdd(playerId, DefaultKillCooldown.GetFloat());
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = NowCooldown[id];
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        NowCooldown[killer.PlayerId] = Math.Clamp(NowCooldown[killer.PlayerId] - ReduceKillCooldown.GetFloat(), MinKillCooldown.GetFloat(), DefaultKillCooldown.GetFloat());
        killer.ResetKillCooldown();
        killer.SyncSettings();
        return true;
    }
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(HasImpostorVision.GetBool());
}
