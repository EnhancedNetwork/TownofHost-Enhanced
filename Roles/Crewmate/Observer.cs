using System.Collections.Generic;
using System.Linq;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

public static class Observer
{
    private static readonly int Id = 9000;
    private static HashSet<byte> playerIdList = [];
    public static bool IsEnable = false;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Observer);
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

    public static void ActivateGuardAnimation(byte killerId, PlayerControl target, int colorId)
    {
        foreach (var observerId in playerIdList.ToArray())
        {
            if (observerId == killerId) continue;
            var observer = Utils.GetPlayerById(observerId);
            if (observer == null) continue;

            observer.RpcGuardAndKill(target, colorId, true);
        }
    }
}
