using System;
using System.Collections.Generic;
using static TOHE.Options;

namespace TOHE;

public static class Sans
{
    private static readonly int Id = 600;
    public static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    private static OptionItem DefaultKillCooldown;
    private static OptionItem ReduceKillCooldown;
    private static OptionItem MinKillCooldown;
    public static OptionItem BardChance;

    private static Dictionary<byte, float> NowCooldown;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Sans);
        DefaultKillCooldown = FloatOptionItem.Create(Id + 10, "SansDefaultKillCooldown", new(0f, 180f, 2.5f), 65f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sans])
            .SetValueFormat(OptionFormat.Seconds);
        ReduceKillCooldown = FloatOptionItem.Create(Id + 11, "SansReduceKillCooldown", new(0f, 180f, 2.5f), 15f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sans])
            .SetValueFormat(OptionFormat.Seconds);
        MinKillCooldown = FloatOptionItem.Create(Id + 12, "SansMinKillCooldown", new(0f, 180f, 2.5f), 2.5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sans])
            .SetValueFormat(OptionFormat.Seconds);
        BardChance = IntegerOptionItem.Create(Id + 13, "BardChance", new(0, 100, 5), 0, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Sans])
            .SetValueFormat(OptionFormat.Percent);
    }
    public static void Init()
    {
        playerIdList = new();
        NowCooldown = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        NowCooldown.TryAdd(playerId, DefaultKillCooldown.GetFloat());
        IsEnable = true;
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = NowCooldown[id];
    public static void OnCheckMurder(PlayerControl killer)
    {
        NowCooldown[killer.PlayerId] = Math.Clamp(NowCooldown[killer.PlayerId] - ReduceKillCooldown.GetFloat(), MinKillCooldown.GetFloat(), DefaultKillCooldown.GetFloat());
        killer.ResetKillCooldown();
        killer.SyncSettings();
    }
}