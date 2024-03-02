using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TOHE.Roles.Core;
using static TOHE.Options;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.Crewmate;

internal class Observer : RoleBase
{
    private const int Id = 9000;
    private static HashSet<byte> playerIdList = [];
    public static bool On = false;
    public override bool IsEnable => On;
    public static bool HasEnabled => CustomRoles.Observer.IsClassEnable();
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Observer);
    }

    public override void Init()
    {
        playerIdList = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        On = true;
    }
    public static void ObserverSetKillShield(PlayerControl target) => Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Observer) && target.PlayerId != x.PlayerId).Do(x => x.RpcGuardAndKill(target, 11, true));
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
