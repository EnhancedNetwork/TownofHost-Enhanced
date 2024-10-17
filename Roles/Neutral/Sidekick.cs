using AmongUs.GameOptions;

namespace TOHE.Roles.Neutral;

internal class Sidekick : RoleBase
{
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;

    public override void Init()
    {
        playerIdList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Jackal.KillCooldownSK.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte ico) => opt.SetVision(Jackal.HasImpostorVision.GetBool());

    public override bool CanUseKillButton(PlayerControl player) => true;
    public override bool CanUseImpostorVentButton(PlayerControl player) => Jackal.CanVentSK.GetBool();
    public override bool CanUseSabotage(PlayerControl player) => Jackal.CanUseSabotageSK.GetBool();

    //public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target) => SidekickKnowRole(target);
    //public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target) => SidekickKnowRole(target) ? Main.roleColors[CustomRoles.Jackal] : string.Empty;

    //private static bool SidekickKnowRole(PlayerControl target)
    //{
    //    return target.Is(CustomRoles.Jackal) || target.Is(CustomRoles.Recruit) || target.Is(CustomRoles.Sidekick);
    //}

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(Translator.GetString("KillButtonText"));
        hud.SabotageButton.OverrideText(Translator.GetString("SabotageButtonText"));
    }
}
