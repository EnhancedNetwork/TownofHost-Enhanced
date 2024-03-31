using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

internal class Observer : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 9000;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    //==================================================================\\

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Observer);
    }

    public override void Init()
    {
        playerIdList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
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
