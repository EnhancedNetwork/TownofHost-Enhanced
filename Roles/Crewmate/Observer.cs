using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

internal class Observer : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Observer;
    private const int Id = 9000;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();

    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Observer);
    }

    public override void Init()
    {
        playerIdList.Clear();
    }
    public override void Add(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
    }
    public static void ActivateGuardAnimation(byte killerId, PlayerControl target)
    {
        foreach (var observerId in playerIdList.ToArray())
        {
            if (observerId == killerId) continue;
            var observer = Utils.GetPlayerById(observerId);
            if (observer == null) continue;

            observer.RpcGuardAndKill(target, forObserver: true);
        }
    }
}
