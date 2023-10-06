using System.Collections.Generic;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

public static class Carnivore
{
    private static readonly int Id = 199411;
    public static List<byte> playerIdList = new();
    public static Dictionary<byte, int> CarnivoreCount = new();

    public static OptionItem CarnivoreToWin;
    private static OptionItem KillCooldown;
    public static OptionItem CanVent;

    public static void SetupCustomOption()
    {
        //Carnivoreは1人固定
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Carnivore, 1, zeroOne: false);
        CarnivoreToWin = IntegerOptionItem.Create(Id + 10, "CarnivoreToWin", new(1, 14, 1), 4, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Carnivore])
            .SetValueFormat(OptionFormat.Times);
        KillCooldown = FloatOptionItem.Create(Id + 11, "KillCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Carnivore])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 12, "CanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Carnivore]);
    }
    public static void Init()
    {
        playerIdList = new();
        CarnivoreCount = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        CarnivoreCount.Add(playerId, 0);

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public static void CanUseVent(PlayerControl player)
    {
        bool Carnivore_canUse = CanVent.GetBool();
        DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(Carnivore_canUse && !player.Data.IsDead);
        player.Data.Role.CanVent = Carnivore_canUse;
    }
}
