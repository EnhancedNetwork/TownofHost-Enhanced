using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.MeetingHudStartPatch;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;
internal class Mortician : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Mortician;
    private const int Id = 8900;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem ShowArrows;

    private static readonly Dictionary<byte, string> msgToSend = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Mortician);
        ShowArrows = BooleanOptionItem.Create(Id + 2, "ShowArrows", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mortician]);
    }
    public override void Init()
    {
        msgToSend.Clear();
    }
    public override void Add(byte playerId)
    {
        CustomRoleManager.CheckDeadBodyOthers.Add(CheckDeadBody);
    }
    public override void Remove(byte playerId)
    {
        CustomRoleManager.CheckDeadBodyOthers.Remove(CheckDeadBody);
    }
    private void CheckDeadBody(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        if (inMeeting || target.IsDisconnected()) return;

        var player = _Player;
        if (player == null || !player.IsAlive()) return;
        LocateArrow.Add(player.PlayerId, target.Data.GetDeadBody().transform.position);
    }
    public override void OnReportDeadBody(PlayerControl pc, NetworkedPlayerInfo target)
    {
        if (_Player)
            LocateArrow.RemoveAllTarget(_Player.PlayerId);

        if (pc == null || target == null || !pc.Is(CustomRoles.Mortician) || pc.PlayerId == target.PlayerId) return;

        string name = string.Empty;
        var killer = target.PlayerId.GetRealKillerById();
        if (killer == null)
        {
            name = killer.GetRealName();
        }

        if (name == string.Empty) msgToSend.TryAdd(pc.PlayerId, string.Format(GetString("MorticianGetNoInfo"), target.PlayerName));
        else msgToSend.TryAdd(pc.PlayerId, string.Format(GetString("MorticianGetInfo"), target.PlayerName, name));
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (!ShowArrows.GetBool() || isForMeeting || seer.PlayerId != seen.PlayerId) return string.Empty;

        return Utils.ColorString(Color.white, LocateArrow.GetArrows(seer));
    }
    public override void OnMeetingHudStart(PlayerControl pc)
    {
        if (msgToSend.ContainsKey(pc.PlayerId))
            AddMsg(msgToSend[pc.PlayerId], pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mortician), GetString("MorticianCheckTitle")));
    }
    public override void MeetingHudClear() => msgToSend.Clear();
}
