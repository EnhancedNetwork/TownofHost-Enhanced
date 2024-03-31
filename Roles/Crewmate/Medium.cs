using Hazel;
using System;
using System.Text;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Utils;
using static TOHE.Translator;
using static TOHE.MeetingHudStartPatch;

namespace TOHE.Roles.Crewmate;

internal class Medium : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 8700;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    //==================================================================\\

    private static OptionItem ContactLimitOpt;
    private static OptionItem OnlyReceiveMsgFromCrew;
    private static OptionItem MediumAbilityUseGainWithEachTaskCompleted;

    private static readonly Dictionary<byte, byte> ContactPlayer = [];
    private static readonly Dictionary<byte, float> ContactLimit = [];

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Medium);
        ContactLimitOpt = IntegerOptionItem.Create(Id + 10, "MediumContactLimit", new(0, 15, 1), 1, TabGroup.CrewmateRoles, false)
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
        playerIdList.Clear();
        ContactPlayer.Clear();
        ContactLimit.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        ContactLimit.Add(playerId, ContactLimitOpt.GetInt());

        if (AmongUsClient.Instance.AmHost)
        {
            CustomRoleManager.CheckDeadBodyOthers.Add(CheckDeadBody);
        }
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        ContactLimit.Remove(playerId);
    }
    public static void SendRPC(byte playerId, byte targetId = 0xff, bool isUsed = false)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Medium);
        writer.Write(playerId);
        writer.Write(ContactLimit[playerId]);
        writer.Write(isUsed);
        if (isUsed) writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte pid = reader.ReadByte();
        float limit = reader.ReadSingle();
        bool isUsed = reader.ReadBoolean();

        ContactLimit[pid] = limit;

        if (isUsed)
        {
            byte targetId = reader.ReadByte();
            ContactPlayer.Clear();
            ContactPlayer.TryAdd(targetId, pid);
        }
    }
    public override void OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (!player.IsAlive()) return;
        ContactLimit[player.PlayerId] += MediumAbilityUseGainWithEachTaskCompleted.GetFloat();
        SendRPC(player.PlayerId);
    }
    public override void OnReportDeadBody(PlayerControl reported, PlayerControl target)
    {
        ContactPlayer.Clear();
        if (target == null) return;

        foreach (var pc in Main.AllAlivePlayerControls.Where(x => playerIdList.Contains(x.PlayerId) && x.PlayerId != target.PlayerId).ToArray())
        {
            if (ContactLimit[pc.PlayerId] < 1) continue;
            ContactLimit[pc.PlayerId] -= 1;
            ContactPlayer.TryAdd(target.PlayerId, pc.PlayerId);
            SendRPC(pc.PlayerId, target.PlayerId, true);
            Logger.Info($"Psychics Make Connections： {pc.GetNameWithRole()} => {target.GetRealName}", "Medium");
        }
    }
    public static void CheckDeadBody(PlayerControl Killer, PlayerControl target, bool inMeeting)
    {
        if (inMeeting) return;

        foreach (var mediumId in playerIdList.ToArray())
        {
            var mediun = GetPlayerById(mediumId);
            if (mediun == null) continue;

            mediun.Notify(ColorString(GetRoleColor(CustomRoles.Medium), GetString("MediumKnowPlayerDead")));
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
        if (ContactLimit[playerId] < 1) TextColor71 = Color.red;
        else TextColor71 = Color.white;
        ProgressText.Append(ColorString(TextColor7, $"({Completed7}/{taskState7.AllTasksCount})"));
        ProgressText.Append(ColorString(TextColor71, $" <color=#ffffff>-</color> {Math.Round(ContactLimit[playerId], 1)}"));
        return ProgressText.ToString();
    }
    public override void OnOthersMeetingHudStart(PlayerControl pc)
    {
        //Self 
        if (ContactPlayer.ContainsValue(pc.PlayerId))
            AddMsg(string.Format(GetString("MediumNotifySelf"), Main.AllPlayerNames[ContactPlayer.Where(x => x.Value == pc.PlayerId).FirstOrDefault().Key], ContactLimit[pc.PlayerId]), pc.PlayerId, ColorString(GetRoleColor(CustomRoles.Medium), GetString("MediumTitle")));

        //For target
        if (ContactPlayer.ContainsKey(pc.PlayerId) && (!OnlyReceiveMsgFromCrew.GetBool() || pc.GetCustomRole().IsCrewmate()))
            AddMsg(string.Format(GetString("MediumNotifyTarget"), Main.AllPlayerNames[ContactPlayer[pc.PlayerId]]), pc.PlayerId, ColorString(GetRoleColor(CustomRoles.Medium), GetString("MediumTitle")));
    }
}