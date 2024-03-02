using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Utils;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Mediumshiper : RoleBase
{
    private static readonly int Id = 8700;
    public static List<byte> playerIdList = [];
    public static bool On = false;
    public override bool IsEnable => On;
    public static bool HasEnabled => CustomRoles.Mediumshiper.IsClassEnable();
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;

    public static OptionItem ContactLimitOpt;
    public static OptionItem OnlyReceiveMsgFromCrew;
    public static OptionItem MediumAbilityUseGainWithEachTaskCompleted;

    public static Dictionary<byte, byte> ContactPlayer = [];
    public static Dictionary<byte, float> ContactLimit = [];

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Mediumshiper);
        ContactLimitOpt = IntegerOptionItem.Create(Id + 10, "MediumshiperContactLimit", new(0, 15, 1), 1, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mediumshiper])
            .SetValueFormat(OptionFormat.Times);
        OnlyReceiveMsgFromCrew = BooleanOptionItem.Create(Id + 11, "MediumshiperOnlyReceiveMsgFromCrew", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mediumshiper]);
        MediumAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 12, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mediumshiper])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        playerIdList = [];
        ContactPlayer = [];
        ContactLimit = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        ContactLimit.Add(playerId, ContactLimitOpt.GetInt());
        On = true;
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        ContactLimit.Remove(playerId);
    }
    public static void SendRPC(byte playerId, byte targetId = 0xff, bool isUsed = false)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Mediumshiper);
        writer.Write(playerId);
        writer.Write(ContactLimit[playerId]);
        writer.Write(isUsed);
        if (isUsed) writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte pid = reader.ReadByte();
        float limit = reader.ReadSingle();
        ContactLimit[pid] = limit;
        bool isUsed = reader.ReadBoolean();
        if (isUsed)
        {
            byte targetId = reader.ReadByte();
            ContactPlayer = [];
            ContactPlayer.TryAdd(targetId, pid);
        }
    }
    public override void OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (!player.IsAlive()) return;
        Mediumshiper.ContactLimit[player.PlayerId] += Mediumshiper.MediumAbilityUseGainWithEachTaskCompleted.GetFloat();
        Mediumshiper.SendRPC(player.PlayerId);
    }
    public override void OnReportDeadBody(PlayerControl reported, PlayerControl target)
    {
        ContactPlayer = [];
        if (target == null) return;
        foreach (var pc in Main.AllAlivePlayerControls.Where(x => playerIdList.Contains(x.PlayerId) && x.PlayerId != target.PlayerId).ToArray())
        {
            if (ContactLimit[pc.PlayerId] < 1) continue;
            ContactLimit[pc.PlayerId] -= 1;
            ContactPlayer.TryAdd(target.PlayerId, pc.PlayerId);
            SendRPC(pc.PlayerId, target.PlayerId, true);
            Logger.Info($"通灵师建立联系：{pc.GetNameWithRole()} => {target.GetRealName}", "Mediumshiper");
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
            Utils.SendMessage(GetString("MediumshipHelp"), pc.PlayerId);
            return true;
        }

        Utils.SendMessage(GetString("Mediumship" + (ans ? "Yes" : "No")), ContactPlayer[pc.PlayerId], Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mediumshiper), GetString("MediumshipTitle")));
        Utils.SendMessage(GetString("MediumshipDone"), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mediumshiper), GetString("MediumshipTitle")));

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
        if (Mediumshiper.ContactLimit[playerId] < 1) TextColor71 = Color.red;
        else TextColor71 = Color.white;
        ProgressText.Append(ColorString(TextColor7, $"({Completed7}/{taskState7.AllTasksCount})"));
        ProgressText.Append(ColorString(TextColor71, $" <color=#ffffff>-</color> {Math.Round(Mediumshiper.ContactLimit[playerId], 1)}"));
        return ProgressText.ToString();
    }
}