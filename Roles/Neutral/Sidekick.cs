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
    public override void SetHudActive(HudManager __instance, bool isActive)
    {
        __instance.SabotageButton.ToggleVisible(isActive && Jackal.CanUseSabotageSK.GetBool());
    }

    public override bool CanUseImpostorVentButton(PlayerControl player)
    {
        bool Sidekick_canUse = Jackal.CanVentSK.GetBool();
        DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(Sidekick_canUse && !player.Data.IsDead);
        return Sidekick_canUse;
    }
    public override bool CanUseSabotage(PlayerControl pc) => Jackal.CanUseSabotageSK.GetBool();
}
