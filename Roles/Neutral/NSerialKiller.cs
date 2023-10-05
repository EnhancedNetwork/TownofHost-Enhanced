using AmongUs.GameOptions;
using System.Collections.Generic;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

public static class NSerialKiller
{
    private static readonly int Id = 12800;
    public static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    public static OptionItem KillCooldown;
    public static OptionItem BloodlustKillCD;
    public static OptionItem CanVent;
    private static OptionItem HasImpostorVision;
    public static OptionItem HasSerialKillerBuddy;
    public static OptionItem ChanceToSpawn;
   // public static OptionItem ChanceToSpawnAnother;
    public static OptionItem BloodlustPlayerCount;
    public static OptionItem ReflectHarmfulInteractions;

    public static void SetupCustomOption()
    {
        //NSerialKillerは1人固定
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.NSerialKiller, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NSerialKiller])
            .SetValueFormat(OptionFormat.Seconds);
    /*    BloodlustKillCD = FloatOptionItem.Create(Id + 12, "BloodlustKillCD", new(0f, 180f, 2.5f), 12.5f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NSerialKiller])
            .SetValueFormat(OptionFormat.Seconds); */
    /*    BloodlustPlayerCount = IntegerOptionItem.Create(Id + 15, "BloodlustPlayerCount", new(0, 15, 1), 7, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.NSerialKiller])
            .SetValueFormat(OptionFormat.Players); */
        CanVent = BooleanOptionItem.Create(Id + 11, "CanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NSerialKiller]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NSerialKiller]);
        HasSerialKillerBuddy = BooleanOptionItem.Create(Id + 16, "HasSerialKillerBuddy", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NSerialKiller]);
        ChanceToSpawn = IntegerOptionItem.Create(Id + 14, "ChanceToSpawn", new(0, 100, 5), 100, TabGroup.NeutralRoles, false)
            .SetParent(HasSerialKillerBuddy)
            .SetValueFormat(OptionFormat.Percent);
      /*  ChanceToSpawnAnother = IntegerOptionItem.Create(Id + 17, "ChanceToSpawnAnother", new(0, 100, 5), 30, TabGroup.NeutralRoles, false)
            .SetParent(ChanceToSpawn)
            .SetValueFormat(OptionFormat.Percent); */
    //    ReflectHarmfulInteractions = BooleanOptionItem.Create(Id + 18, "ReflectHarmfulInteractions", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NSerialKiller]);
    }
    public static void Init()
    {
        playerIdList = new();
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
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public static void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision.GetBool());
    public static void CanUseVent(PlayerControl player)
    {
        bool NSerialKiller_canUse = CanVent.GetBool();
        DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(NSerialKiller_canUse && !player.Data.IsDead);
        player.Data.Role.CanVent = NSerialKiller_canUse;
    }
}
