using Hazel;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Spiritualist : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Spiritualist;
    private const int Id = 9600;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem ShowGhostArrowEverySeconds;
    private static OptionItem ShowGhostArrowForSeconds;

    private static byte SpiritualistTarget = new();
    private static readonly Dictionary<byte, long> ShowGhostArrowUntil = [];
    private static readonly Dictionary<byte, long> LastGhostArrowShowTime = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Spiritualist);
        ShowGhostArrowEverySeconds = FloatOptionItem.Create(Id + 10, "SpiritualistShowGhostArrowEverySeconds", new(1f, 60f, 1f), 15f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritualist])
            .SetValueFormat(OptionFormat.Seconds);
        ShowGhostArrowForSeconds = FloatOptionItem.Create(Id + 11, "SpiritualistShowGhostArrowForSeconds", new(1f, 60f, 1f), 2f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritualist])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        LastGhostArrowShowTime.Clear();
        ShowGhostArrowUntil.Clear();
        SpiritualistTarget = new();
    }
    public override void Add(byte playerId)
    {
        SpiritualistTarget = byte.MaxValue;
        LastGhostArrowShowTime.Add(playerId, 0);
        ShowGhostArrowUntil.Add(playerId, 0);
    }
    public override void Remove(byte playerId)
    {
        LastGhostArrowShowTime.Remove(playerId);
        ShowGhostArrowUntil.Remove(playerId);
    }

    private static bool ShowArrow(byte playerId)
    {
        long timestamp = Utils.GetTimeStamp();

        if (LastGhostArrowShowTime[playerId] == 0 || LastGhostArrowShowTime[playerId] + (long)ShowGhostArrowEverySeconds.GetFloat() <= timestamp)
        {
            LastGhostArrowShowTime[playerId] = timestamp;
            ShowGhostArrowUntil[playerId] = timestamp + (long)ShowGhostArrowForSeconds.GetFloat();
            return true;
        }
        else if (ShowGhostArrowUntil[playerId] >= timestamp) return true;

        return false;
    }

    public override void OnReportDeadBody(PlayerControl reported, NetworkedPlayerInfo target)
    {
        if (target == null) return;

        if (SpiritualistTarget != byte.MaxValue)
            RemoveTarget(SpiritualistTarget);

        SpiritualistTarget = target.PlayerId;
    }

    public override void AfterMeetingTasks()
    {
        PlayerControl player = _Player;

        if (!player || !player.IsAlive()) return;

        LastGhostArrowShowTime[player.PlayerId] = 0;
        ShowGhostArrowUntil[player.PlayerId] = 0;

        PlayerControl target = Main.AllPlayerControls.FirstOrDefault(a => a.PlayerId == SpiritualistTarget);

        if (target == null) return;

        TargetArrow.Add(player.PlayerId, target.PlayerId);

        var writer = CustomRpcSender.Create("SpiritualistSendMessage", SendOption.None);
        writer.StartMessage(target.GetClientId());
        writer.StartRpc(target.NetId, (byte)RpcCalls.SetName)
            .Write(target.Data.NetId)
            .Write(GetString("SpiritualistNoticeTitle"))
            .EndRpc();
        writer.StartRpc(target.NetId, (byte)RpcCalls.SendChat)
            .Write(GetString("SpiritualistNoticeMessage"))
            .EndRpc();
        writer.StartRpc(target.NetId, (byte)RpcCalls.SetName)
            .Write(target.Data.NetId)
            .Write(target.Data.PlayerName)
            .EndRpc();
        writer.EndMessage();
        writer.SendMessage();
    }

    public override string GetSuffix(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false)
    {
        if (!seer.Is(CustomRoles.Spiritualist) || !seer.IsAlive()) return string.Empty;
        if (target != null && seer.PlayerId != target.PlayerId) return string.Empty;
        if (GameStates.IsMeeting) return string.Empty;
        if (SpiritualistTarget != byte.MaxValue && ShowArrow(seer.PlayerId))
        {
            return Utils.ColorString(seer.GetRoleColor(), TargetArrow.GetArrows(seer, SpiritualistTarget));
        }
        return string.Empty;
    }

    public static void RemoveTarget(byte player)
    {
        if (SpiritualistTarget != player) return;

        if (AmongUsClient.Instance.AmHost)
            foreach (var spiritualist in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Spiritualist)).Select(x => x.PlayerId).ToList())
            {
                TargetArrow.Remove(spiritualist, SpiritualistTarget);
            }

        SpiritualistTarget = byte.MaxValue;
    }
}
