﻿using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Lucky : IAddon
{
    private const int Id = 19500;
    public AddonTypes Type => AddonTypes.Helpful;

    private static OptionItem LuckyProbability;

    private static Dictionary<byte, bool> LuckyAvoid;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Lucky, canSetNum: true, teamSpawnOptions: true);
        LuckyProbability = IntegerOptionItem.Create(Id + 10, "LuckyProbability", new(0, 100, 5), 50, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lucky])
            .SetValueFormat(OptionFormat.Percent);
    }

    public static void Init()
    {
        LuckyAvoid = [];
    }
    public static void Add(byte PlayerId)
    {
        LuckyAvoid.Add(PlayerId, false);
    }
    public static void Remove(byte player)
    {
        LuckyAvoid.Remove(player);
    }

    private static void AvoidDeathChance(PlayerControl killer, PlayerControl target)
    {
        var rd = IRandom.Instance;
        if (rd.Next(0, 101) < LuckyProbability.GetInt())
        {
            killer.RpcGuardAndKill(target);
            LuckyAvoid[target.PlayerId] = true;
        }
    }

    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        AvoidDeathChance(killer, target);
        if (LuckyAvoid[target.PlayerId])
        {
            LuckyAvoid[target.PlayerId] = false;
            return false;
        }

        return true;
    }
}