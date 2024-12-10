using System.Text;
using TOHE.Roles.Core;
using static TOHE.MeetingHudStartPatch;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Detective : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 7900;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem DetectiveCanknowKiller;

    private static readonly Dictionary<byte, string> DetectiveNotify = [];
    private static readonly Dictionary<byte, string> InfoAboutDeadPlayerAndKiller = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Detective);
        DetectiveCanknowKiller = BooleanOptionItem.Create(7902, "DetectiveCanknowKiller", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Detective]);
    }

    public override void Init()
    {
        DetectiveNotify.Clear();
        InfoAboutDeadPlayerAndKiller.Clear();
    }

    public override void Add(byte playerId)
    {
        CustomRoleManager.CheckDeadBodyOthers.Add(GetInfoFromDeadBody);
    }
    public override void Remove(byte playerId)
    {
        CustomRoleManager.CheckDeadBodyOthers.Remove(GetInfoFromDeadBody);
    }
    private void GetInfoFromDeadBody(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        if ((target.IsDisconnected() && killer.PlayerId == target.PlayerId) || inMeeting) return;

        InfoAboutDeadPlayerAndKiller[killer.PlayerId] = Utils.GetRoleName(killer.GetCustomRole());
        InfoAboutDeadPlayerAndKiller[target.PlayerId] = Utils.GetRoleName(target.GetCustomRole());
    }
    public override void OnReportDeadBody(PlayerControl player, NetworkedPlayerInfo deadBody)
    {
        if (deadBody == null) return;

        if (player != null && player.Is(CustomRoles.Detective) && player.PlayerId != deadBody.PlayerId)
        {
            var msg = new StringBuilder();
            _ = InfoAboutDeadPlayerAndKiller.TryGetValue(deadBody.PlayerId, out var RoleDeadBodyInfo);
            msg.Append(string.Format(GetString("DetectiveNoticeVictim"), deadBody.PlayerName, RoleDeadBodyInfo));

            if (DetectiveCanknowKiller.GetBool())
            {
                var realKiller = deadBody.PlayerId.GetRealKillerById();
                if (realKiller == null) msg.Append($"；\n{GetString("DetectiveNoticeKillerNotFound")}");
                else
                {
                    _ = InfoAboutDeadPlayerAndKiller.TryGetValue(realKiller.Data.PlayerId, out var RoleKillerInfo);
                    msg.Append($"；\n{string.Format(GetString("DetectiveNoticeKiller"), RoleKillerInfo)}");
                }
            }
            DetectiveNotify.Remove(player.PlayerId);
            DetectiveNotify.Add(player.PlayerId, msg.ToString());
        }
        InfoAboutDeadPlayerAndKiller.Clear();
    }

    public override void OnMeetingHudStart(PlayerControl pc)
    {
        if (DetectiveNotify.ContainsKey(pc.PlayerId))
            AddMsg(DetectiveNotify[pc.PlayerId], pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Detective), GetString("DetectiveNoticeTitle")));
    }
    public override void MeetingHudClear()
    {
        DetectiveNotify.Clear();
        InfoAboutDeadPlayerAndKiller.Clear();
    }
}
