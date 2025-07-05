using Hazel;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using static TOHE.MeetingHudStartPatch;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Medium : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Medium;
    private const int Id = 8700;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Medium);
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem ContactLimitOpt;
    private static OptionItem OnlyReceiveMsgFromCrew;

    private static readonly Dictionary<byte, byte> ContactPlayer = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Medium);
        ContactLimitOpt = IntegerOptionItem.Create(Id + 10, GeneralOption.SkillLimitTimes, new(0, 15, 1), 1, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Medium])
            .SetValueFormat(OptionFormat.Times);
        OnlyReceiveMsgFromCrew = BooleanOptionItem.Create(Id + 11, "MediumOnlyReceiveMsgFromCrew", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Medium]);
        MediumAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 12, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Medium])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        ContactPlayer.Clear();
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(ContactLimitOpt.GetFloat());
    }
    private void SendRPC(byte playerId, byte targetId = 0xff)
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(playerId);
        writer.Write(targetId);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte pid = reader.ReadByte();
        byte targetId = reader.ReadByte();

        ContactPlayer.Clear();
        ContactPlayer.TryAdd(targetId, pid);
    }
    public override void OnReportDeadBody(PlayerControl reported, NetworkedPlayerInfo target)
    {
        ContactPlayer.Clear();
        if (target == null || target.Object == null || _Player == null) return;

        var medium = _Player;
        if (medium.GetAbilityUseLimit() > 0)
        {
            medium.RpcRemoveAbilityUse();
            ContactPlayer.TryAdd(target.PlayerId, medium.PlayerId);
            SendRPC(medium.PlayerId, target.PlayerId);

            Logger.Info($"Psychics Make Connections： {medium.GetRealName} => {target.PlayerName}", "Medium");
        }
    }
    public static bool MsMsg(PlayerControl pc, string msg)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsMeeting || pc == null) return false;
        if (!ContactPlayer.ContainsKey(pc.PlayerId)) return false;
        if (OnlyReceiveMsgFromCrew.GetBool() && !pc.GetCustomRole().IsCrewmate()) return false;
        if (pc.IsAlive()) return false;
        msg = msg.ToLower().Trim();
        if (!CheckCommond(ref msg, "通灵|ms|mediumship|medium", false)) return false;

        bool ans;
        if (msg.Contains('n') || msg.Contains(GetString("No")) || msg.Contains('错') || msg.Contains("不是")) ans = false;
        else if (msg.Contains('y') || msg.Contains(GetString("Yes")) || msg.Contains('对')) ans = true;
        else
        {
            SendMessage(GetString("MediumHelp"), pc.PlayerId);
            return true;
        }

        SendMessage(GetString("Medium" + (ans ? "Yes" : "No")), ContactPlayer[pc.PlayerId], ColorString(GetRoleColor(CustomRoles.Medium), GetString("Medium").ToUpper()));
        SendMessage(GetString("MediumDone"), pc.PlayerId, ColorString(GetRoleColor(CustomRoles.Medium), GetString("Medium").ToUpper()));

        ContactPlayer.Remove(pc.PlayerId);

        return true;
    }
    public static bool CheckCommond(ref string msg, string command, bool exact = true)
    {
        var comList = command.Split('|');
        foreach (var comm in comList)
        {
            if (exact)
            {
                if (msg == "/" + comm) return true;
            }
            else
            {
                if (msg.StartsWith("/" + comm))
                {
                    msg = msg.Replace("/" + comm, string.Empty);
                    return true;
                }
            }
        }
        return false;
    }
    public override void OnOthersMeetingHudStart(PlayerControl pc)
    {
        if (!_Player.IsAlive()) return;

        //Self 
        if (ContactPlayer.ContainsValue(pc.PlayerId))
            AddMsg(string.Format(GetString("MediumNotifySelf"), Main.AllPlayerNames[ContactPlayer.Where(x => x.Value == pc.PlayerId).FirstOrDefault().Key], _Player.GetAbilityUseLimit()), pc.PlayerId, ColorString(GetRoleColor(CustomRoles.Medium), GetString("Medium").ToUpper()));

        //For target
        if (ContactPlayer.ContainsKey(pc.PlayerId) && (!OnlyReceiveMsgFromCrew.GetBool() || pc.GetCustomRole().IsCrewmate()))
            AddMsg(string.Format(GetString("MediumNotifyTarget"), Main.AllPlayerNames[ContactPlayer[pc.PlayerId]]), pc.PlayerId, ColorString(GetRoleColor(CustomRoles.Medium), GetString("Medium").ToUpper()));
    }
}
