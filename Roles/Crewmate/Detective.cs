using System.Text;
using TOHE.Roles.Core;
using static TOHE.MeetingHudStartPatch;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Detective : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Detective;
    private const int Id = 7900;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem DetectiveCanknowKiller;

    private string Notify;
    private readonly Dictionary<byte, string> InfoAboutDeadPlayerAndKiller = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Detective);
        DetectiveCanknowKiller = BooleanOptionItem.Create(7902, "DetectiveCanknowKiller", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Detective]);
    }

    public override void Init()
    {
        Notify = string.Empty;
        InfoAboutDeadPlayerAndKiller.Clear();
    }

    public override void Add(byte playerId)
    {
        CustomRoleManager.CheckDeadBodyOthers.Add(GetInfoFromDeadBody);
    }
    private void GetInfoFromDeadBody(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        if (!_Player.IsAlive() || inMeeting || (target.IsDisconnected() && killer.PlayerId == target.PlayerId)) return;

        InfoAboutDeadPlayerAndKiller[killer.PlayerId] = Utils.GetRoleName(killer.GetCustomRole());
        InfoAboutDeadPlayerAndKiller[target.PlayerId] = Utils.GetRoleName(target.GetCustomRole());
    }
    public override void OnReportDeadBody(PlayerControl player, NetworkedPlayerInfo deadBody)
    {
        if (deadBody == null) return;

        if (player != null && player.Is(CustomRoles.Detective) && player.PlayerId != deadBody.PlayerId)
        {
            var msg = new StringBuilder();
            var RoleDeadBodyInfo = InfoAboutDeadPlayerAndKiller.GetValueOrDefault(deadBody.PlayerId);
            msg.Append(string.Format(GetString("DetectiveNoticeVictim"), deadBody.PlayerName, RoleDeadBodyInfo));

            if (DetectiveCanknowKiller.GetBool())
            {
                var realKiller = deadBody.PlayerId.GetRealKillerById();
                
                if (realKiller == null 
                    || realKiller.Data == null 
                    || deadBody.PlayerId == realKiller.Data.PlayerId)
                    msg.Append($"；\n{GetString("DetectiveNoticeKillerNotFound")}");
                
                else
                {
                    var RoleKillerInfo = InfoAboutDeadPlayerAndKiller.GetValueOrDefault(realKiller.Data.PlayerId);
                    if (string.IsNullOrEmpty(RoleKillerInfo))
                    {
                        RoleKillerInfo = Main.PlayerStates.TryGetValue(realKiller.Data.PlayerId, out var killerState) 
                            ? Utils.GetRoleName(killerState.MainRole) : string.Empty;

                        if (string.IsNullOrEmpty(RoleKillerInfo))
                            Logger.Warn($"Killer role still empty - role: {killerState?.MainRole} - from translations: {Utils.GetRoleName(killerState.MainRole) ?? string.Empty}", "Detective");
                    }
                    msg.Append($"；\n{string.Format(GetString("DetectiveNoticeKiller"), RoleKillerInfo)}");
                }
            }
            Notify = msg.ToString();
        }
        InfoAboutDeadPlayerAndKiller.Clear();
    }

    public override void OnMeetingHudStart(PlayerControl pc)
    {
        if (!_Player.IsAlive() || _Player.PlayerId != pc.PlayerId || string.IsNullOrEmpty(Notify)) return;

        AddMsg(Notify, pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Detective), GetString("DetectiveNoticeTitle")));
    }
    public override void MeetingHudClear()
    {
        Notify = string.Empty;
        InfoAboutDeadPlayerAndKiller.Clear();
    }
}
