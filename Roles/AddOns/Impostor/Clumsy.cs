using MS.Internal.Xml.XPath;
using System.Collections.Generic;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Impostor;
public static class Clumsy
{
    private static readonly int Id = 22700;

    public static OptionItem ChanceToMiss;
    public static Dictionary<byte, bool> HasMissed;
    public static void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Clumsy, canSetNum: true, tab: TabGroup.Addons);
        ChanceToMiss = IntegerOptionItem.Create(22703, "ChanceToMiss", new(0, 100, 5), 50, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Clumsy])
            .SetValueFormat(OptionFormat.Percent);
    }

    public static void Init()
    {
        HasMissed = [];
    }

    public static void Add(byte PlayerId)
    {
        HasMissed.Add(PlayerId, false);
    }
    public static void MissChance(PlayerControl killer)
    {
        var miss = IRandom.Instance;
        if (miss.Next(0, 100) < ChanceToMiss.GetInt())
        {
            killer.RpcGuardAndKill(killer);
            killer.SetKillCooldown();
            HasMissed[killer.PlayerId] = true;
        }
    }
}