using static TOHE.Options;

namespace TOHE.Roles.AddOns.Impostor;

public class Clumsy : IAddon
{
    public CustomRoles Role => CustomRoles.Clumsy;
    private const int Id = 22700;
    public AddonTypes Type => AddonTypes.Impostor;

    private static OptionItem ChanceToMiss;

    private static readonly Dictionary<byte, bool> HasMissed = [];

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Clumsy, canSetNum: true, tab: TabGroup.Addons);
        ChanceToMiss = IntegerOptionItem.Create(22703, "ChanceToMiss", new(0, 100, 5), 50, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Clumsy])
            .SetValueFormat(OptionFormat.Percent);
    }

    public void Init()
    {
        HasMissed.Clear();
    }
    public void Add(byte playerId, bool gameIsLoading = true)
    {
        HasMissed.Add(playerId, false);
    }
    public void Remove(byte playerId)
    {
        HasMissed.Remove(playerId);
    }

    private static void MissChance(PlayerControl killer)
    {
        var miss = IRandom.Instance;
        if (miss.Next(0, 101) < ChanceToMiss.GetInt())
        {
            killer.RpcGuardAndKill(killer);
            killer.SetKillCooldown();
            HasMissed[killer.PlayerId] = true;
        }
    }

    public static bool OnCheckMurder(PlayerControl killer)
    {
        MissChance(killer);
        if (HasMissed[killer.PlayerId])
        {
            HasMissed[killer.PlayerId] = false;
            return false;
        }

        return true;
    }
}
