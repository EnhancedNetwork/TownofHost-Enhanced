using System.Text;
using TOHE.Roles.Core;
using static TOHE.MeetingHudStartPatch;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Analyst : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Analyst;
    private const int Id = 7900;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem AnalystCanknowKiller;

    private static readonly Dictionary<byte, string> AnalystNotify = [];
    private static readonly Dictionary<byte, string> InfoAboutDeadPlayerAndKiller = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Analyst);
        AnalystCanknowKiller = BooleanOptionItem.Create(7902, "AnalystCanknowKiller", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Analyst]);
    }

    public override void Init()
    {
        AnalystNotify.Clear();
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

        if (player != null && player.Is(CustomRoles.Analyst) && player.PlayerId != deadBody.PlayerId)
        {
            var msg = new StringBuilder();
            _ = InfoAboutDeadPlayerAndKiller.TryGetValue(deadBody.PlayerId, out var RoleDeadBodyInfo);
            msg.Append(string.Format(GetString("AnalystNoticeVictim"), deadBody.PlayerName, RoleDeadBodyInfo));

            if (AnalystCanknowKiller.GetBool())
            {
                var realKiller = deadBody.PlayerId.GetRealKillerById();
                if (realKiller == null) msg.Append($"；\n{GetString("AnalystNoticeKillerNotFound")}");
                else
                {
                    _ = InfoAboutDeadPlayerAndKiller.TryGetValue(realKiller.Data.PlayerId, out var RoleKillerInfo);
                    msg.Append($"；\n{string.Format(GetString("AnalystNoticeKiller"), RoleKillerInfo)}");
                }
            }
            AnalystNotify.Remove(player.PlayerId);
            AnalystNotify.Add(player.PlayerId, msg.ToString());
        }
        InfoAboutDeadPlayerAndKiller.Clear();
    }

    public override void OnMeetingHudStart(PlayerControl pc)
    {
        if (AnalystNotify.TryGetValue(pc.PlayerId, out var notify))
            AddMsg(notify, pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Analyst), GetString("AnalystNoticeTitle")));
    }
    public override void MeetingHudClear()
    {
        AnalystNotify.Clear();
        InfoAboutDeadPlayerAndKiller.Clear();
    }
}
