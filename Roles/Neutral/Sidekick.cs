using AmongUs.GameOptions;
using System.Collections.Generic;

namespace TOHE.Roles.Neutral;

internal class Sidekick : RoleBase
{
    public static HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override void Init()
    {
        playerIdList = [];
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
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
}
