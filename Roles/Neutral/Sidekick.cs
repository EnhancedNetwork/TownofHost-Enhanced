using AmongUs.GameOptions;
using System.Collections.Generic;

namespace TOHE.Roles.Neutral;

public static class Sidekick
{
    public static List<byte> playerIdList = [];
    public static bool IsEnable = false;
    public static void Init()
    {
        playerIdList = [];
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Jackal.KillCooldownSK.GetFloat();
    public static void ApplyGameOptions(IGameOptions opt) => opt.SetVision(Jackal.HasImpostorVision.GetBool());
    public static void SetHudActive(HudManager __instance, bool isActive)
    {
        __instance.SabotageButton.ToggleVisible(isActive && Jackal.CanUseSabotageSK.GetBool());
    }

    public static void CanUseVent(PlayerControl player)
    {
        bool Sidekick_canUse = Jackal.CanVentSK.GetBool();
        DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(Sidekick_canUse && !player.Data.IsDead);
        player.Data.Role.CanVent = Sidekick_canUse;
    }
}
