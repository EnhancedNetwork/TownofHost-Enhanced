using Hazel;
using InnerNet;
using System;
using System.Text;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.MeetingHudStartPatch;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Medium : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 8700;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Medium);
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem ContactLimitOpt;
    private static OptionItem OnlyReceiveMsgFromCrew;
    private static OptionItem MediumAbilityUseGainWithEachTaskCompleted;

    private static readonly Dictionary<byte, byte> ContactPlayer = [];

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Medium);
        ContactLimitOpt = IntegerOptionItem.Create(Id + 10, GeneralOption.SkillLimitTimes, new(0, 15, 1), 1, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Medium])
            .SetValueFormat(OptionFormat.Times);
        OnlyReceiveMsgFromCrew = BooleanOptionItem.Create(Id + 11, "MediumOnlyReceiveMsgFromCrew", true, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Medium]);
        MediumAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 12, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Medium])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        ContactPlayer.Clear();
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = ContactLimitOpt.GetFloat();
    }
    public void SendRPC(byte playerId, byte targetId = 0xff, bool isUsed = false)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
        writer.Write(playerId);
        writer.Write(AbilityLimit);
        writer.Write(isUsed);
        if (isUsed) writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte pid = reader.ReadByte();
        float limit = reader.ReadSingle();
        bool isUsed = reader.ReadBoolean();

        AbilityLimit = limit;

        if (isUsed)
        {
            byte targetId = reader.ReadByte();
            ContactPlayer.Clear();
            ContactPlayer.TryAdd(targetId, pid);
        }
    }
    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (player.IsAlive())
        {
            AbilityLimit += MediumAbilityUseGainWithEachTaskCompleted.GetFloat();
            SendRPC(player.PlayerId);
        }
        return true;
    }
    public override void OnReportDeadBody(PlayerControl reported, NetworkedPlayerInfo target)
    {
        ContactPlayer.Clear();
        if (target == null || target.Object == null || _Player == null) return;

        if (AbilityLimit > 0)
        {
            AbilityLimit--;
            ContactPlayer.TryAdd(target.PlayerId, _Player.PlayerId);
            SendRPC(_Player.PlayerId, target.PlayerId, true);
            Logger.Info($"Psychics Make Connections： {_Player.GetRealName} => {target.PlayerName}", "Medium");
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

        SendMessage(GetString("Medium" + (ans ? "Yes" : "No")), ContactPlayer[pc.PlayerId], ColorString(GetRoleColor(CustomRoles.Medium), GetString("MediumTitle")));
        SendMessage(GetString("MediumDone"), pc.PlayerId, ColorString(GetRoleColor(CustomRoles.Medium), GetString("MediumTitle")));

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
    public override string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        var taskState7 = Main.PlayerStates?[playerId].TaskState;
        Color TextColor7;
        var TaskCompleteColor7 = Color.green;
        var NonCompleteColor7 = Color.yellow;
        var NormalColor7 = taskState7.IsTaskFinished ? TaskCompleteColor7 : NonCompleteColor7;
        TextColor7 = comms ? Color.gray : NormalColor7;
        string Completed7 = comms ? "?" : $"{taskState7.CompletedTasksCount}";
        Color TextColor71;
        if (AbilityLimit < 1) TextColor71 = Color.red;
        else TextColor71 = Color.white;
        ProgressText.Append(ColorString(TextColor7, $"({Completed7}/{taskState7.AllTasksCount})"));
        ProgressText.Append(ColorString(TextColor71, $" <color=#ffffff>-</color> {Math.Round(AbilityLimit, 1)}"));
        return ProgressText.ToString();
    }
    public override void OnOthersMeetingHudStart(PlayerControl pc)
    {
        if (!_Player.IsAlive()) return;

        //Self 
        if (ContactPlayer.ContainsValue(pc.PlayerId))
            AddMsg(string.Format(GetString("MediumNotifySelf"), Main.AllPlayerNames[ContactPlayer.Where(x => x.Value == pc.PlayerId).FirstOrDefault().Key], AbilityLimit), pc.PlayerId, ColorString(GetRoleColor(CustomRoles.Medium), GetString("MediumTitle")));

        //For target
        if (ContactPlayer.ContainsKey(pc.PlayerId) && (!OnlyReceiveMsgFromCrew.GetBool() || pc.GetCustomRole().IsCrewmate()))
            AddMsg(string.Format(GetString("MediumNotifyTarget"), Main.AllPlayerNames[ContactPlayer[pc.PlayerId]]), pc.PlayerId, ColorString(GetRoleColor(CustomRoles.Medium), GetString("MediumTitle")));
    }
}