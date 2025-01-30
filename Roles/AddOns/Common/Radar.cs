using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Radar : IAddon
{
    public CustomRoles Role => CustomRoles.Radar;
    private const int Id = 28200;
    public AddonTypes Type => AddonTypes.Helpful;

    private static readonly Dictionary<byte, byte> ClosestPlayer = [];

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Radar, canSetNum: true, tab: TabGroup.Addons, teamSpawnOptions: true);
    }
    public void Init()
    {
        ClosestPlayer.Clear();
    }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
    public void OnFixedUpdateLowLoad(PlayerControl seer)
    {
        if (!seer.Is(CustomRoles.Radar) || seer.inVent || !seer.IsAlive() || !GameStates.IsInTask) return;
        if (Main.AllAlivePlayerControls.Length <= 1) return;

        PlayerControl closest = Main.AllAlivePlayerControls.Where(x => x.PlayerId != seer.PlayerId).MinBy(x => Utils.GetDistance(seer.GetCustomPosition(), x.GetCustomPosition()));
        if (ClosestPlayer.TryGetValue(seer.PlayerId, out var targetId))
        {
            if (targetId != closest.PlayerId)
            {
                ClosestPlayer[seer.PlayerId] = closest.PlayerId;
                TargetArrow.Remove(seer.PlayerId, targetId);
                TargetArrow.Add(seer.PlayerId, closest.PlayerId);
            }
        }
        else
        {
            ClosestPlayer[seer.PlayerId] = closest.PlayerId;
            TargetArrow.Add(seer.PlayerId, closest.PlayerId);
        }
    }
    public static string GetPlayerArrow(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (isForMeeting || !seer.Is(CustomRoles.Radar) || seer.PlayerId != target.PlayerId) return string.Empty;
        return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Radar), TargetArrow.GetArrows(seer));
    }
}

