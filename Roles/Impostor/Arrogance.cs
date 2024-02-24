using System;
using System.Collections.Generic;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

internal class Arrogance : RoleBase
{
    private const int Id = 500;
    public static List<byte> playerIdList = [];
    public static bool On;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    private static OptionItem DefaultKillCooldown;
    private static OptionItem ReduceKillCooldown;
    private static OptionItem MinKillCooldown;
    public static OptionItem BardChance;

    private static Dictionary<byte, float> NowCooldown = [];

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Arrogance);
        DefaultKillCooldown = FloatOptionItem.Create(Id + 10, "ArroganceDefaultKillCooldown", new(0f, 180f, 2.5f), 65f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Arrogance])
            .SetValueFormat(OptionFormat.Seconds);
        ReduceKillCooldown = FloatOptionItem.Create(Id + 11, "ArroganceReduceKillCooldown", new(0f, 180f, 2.5f), 15f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Arrogance])
            .SetValueFormat(OptionFormat.Seconds);
        MinKillCooldown = FloatOptionItem.Create(Id + 12, "ArroganceMinKillCooldown", new(0f, 180f, 2.5f), 2.5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Arrogance])
            .SetValueFormat(OptionFormat.Seconds);
        BardChance = IntegerOptionItem.Create(Id + 13, "BardChance", new(0, 100, 5), 0, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Arrogance])
            .SetValueFormat(OptionFormat.Percent);
    }
    public override void Init()
    {
        playerIdList = [];
        NowCooldown = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        NowCooldown.TryAdd(playerId, DefaultKillCooldown.GetFloat());
        On = true;
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = NowCooldown[id];
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        NowCooldown[killer.PlayerId] = Math.Clamp(NowCooldown[killer.PlayerId] - ReduceKillCooldown.GetFloat(), MinKillCooldown.GetFloat(), DefaultKillCooldown.GetFloat());
        killer.ResetKillCooldown();
        killer.SyncSettings();

        return true;
    }
}