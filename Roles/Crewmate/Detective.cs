using static TOHE.Options;
using static TOHE.MeetingHudStartPatch;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Detective : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 7900;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem DetectiveCanknowKiller;

    private static readonly Dictionary<byte, string> DetectiveNotify = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Detective);
        DetectiveCanknowKiller = BooleanOptionItem.Create("DetectiveCanknowKiller", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Detective]);
    }

    public override void Init()
    {
        playerIdList.Clear();
        DetectiveNotify.Clear();
    }

    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public override void OnReportDeadBody(PlayerControl player, PlayerControl deadBody)
    {
        if (deadBody == null || deadBody.IsAlive()) return;

        if (player != null && player.Is(CustomRoles.Detective) && player != deadBody)
        {
            string msg;
            msg = string.Format(GetString("DetectiveNoticeVictim"), deadBody?.GetRealName(), deadBody?.GetDisplayRoleAndSubName(deadBody, false));
            if (DetectiveCanknowKiller.GetBool())
            {
                var realKiller = deadBody?.GetRealKiller();
                if (realKiller == null) msg += "；" + GetString("DetectiveNoticeKillerNotFound");
                else msg += "；" + string.Format(GetString("DetectiveNoticeKiller"), realKiller.GetDisplayRoleAndSubName(realKiller, false));
            }
            DetectiveNotify.Add(player.PlayerId, msg);
        }
    }

    public override void OnMeetingHudStart(PlayerControl pc)
    {
        if (DetectiveNotify.ContainsKey(pc.PlayerId))
            AddMsg(DetectiveNotify[pc.PlayerId], pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Detective), GetString("DetectiveNoticeTitle")));
    }
    public override void MeetingHudClear() => DetectiveNotify.Clear();
}
