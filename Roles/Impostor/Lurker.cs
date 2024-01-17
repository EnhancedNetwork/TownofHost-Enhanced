using System.Collections.Generic;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

public static class Lurker
{
    private static readonly int Id = 1900;
    public static List<byte> playerIdList = [];
    public static bool IsEnable = false;

    private static OptionItem DefaultKillCooldown;
    private static OptionItem ReduceKillCooldown;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Lurker);
        DefaultKillCooldown = FloatOptionItem.Create(Id + 10, "SansDefaultKillCooldown", new(20f, 180f, 1f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lurker])
            .SetValueFormat(OptionFormat.Seconds);
        ReduceKillCooldown = FloatOptionItem.Create(Id + 11, "SansReduceKillCooldown", new(0f, 10f, 1f), 2f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lurker])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public static void Init()
    {
        playerIdList = [];
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;
    }

    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = DefaultKillCooldown.GetFloat();

    public static void OnEnterVent(PlayerControl pc)
    {
        if (!IsEnable) return;
        if (!pc.Is(CustomRoles.Lurker)) return;

        float newCd = Main.AllPlayerKillCooldown[pc.PlayerId] - ReduceKillCooldown.GetFloat();
        if (newCd <= 0)
        {
            return;
        }

        Main.AllPlayerKillCooldown[pc.PlayerId] = newCd;
        pc.SyncSettings();
    }

    public static bool OnCheckMurder(PlayerControl killer)
    {
        killer.ResetKillCooldown();
        killer.SyncSettings();
        return true;
    }
}
