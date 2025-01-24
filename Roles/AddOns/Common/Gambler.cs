using static TOHE.Options;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.AddOns.Common;

public class Gambler : IAddon
{
    public CustomRoles Role => CustomRoles.Gambler;
    private const int Id = 33100;
    public AddonTypes Type => AddonTypes.Mixed;
    private static readonly Dictionary<byte, bool> Gamble = [];

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Gambler, canSetNum: true, teamSpawnOptions: true);
    }
    public void Init()
    {
        Gamble.Clear();
    }
    public void Add(byte playerId, bool gameIsLoading = true)
    {
        Gamble[playerId] = false;
    }
    public void Remove(byte player)
    {
        Gamble.Remove(player);
    }
    private static void AvoidDeathChance(PlayerControl killer, PlayerControl target)
    {
        var rd = IRandom.Instance;
        if (rd.Next(1, 3) <= 1)
        {
            killer.RpcGuardAndKill(target);
            Gamble[target.PlayerId] = true;
        }
    }
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        AvoidDeathChance(killer, target);
        if (Gamble[target.PlayerId])
        {
            Gamble[target.PlayerId] = false;
            return false;
        }
        return true;
    }
}

