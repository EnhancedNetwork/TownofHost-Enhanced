using Hazel;
using Il2CppSystem.Text;
using System.Collections.Generic;
using System.Linq;
using System;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;
using UnityEngine;
using static TOHE.Utils;
using static TOHE.CheckForEndVotingPatch;

namespace TOHE.Roles.Crewmate;

internal class Oracle : RoleBase
{
    private static readonly int Id = 9100;
    private static List<byte> playerIdList = [];
    public static bool On = false;
    public override bool IsEnable => On;
    public static bool HasEnabled => CustomRoles.Oracle.IsClassEnable();
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;

    public static OptionItem CheckLimitOpt;
    //  private static OptionItem OracleCheckMode;
    public static OptionItem HidesVote;
    public static OptionItem FailChance;
    public static OptionItem OracleAbilityUseGainWithEachTaskCompleted;
    public static OptionItem ChangeRecruitTeam;
    public static List<byte> didVote = [];
    public static Dictionary<byte, float> CheckLimit = [];
    public static Dictionary<byte, float> TempCheckLimit = [];

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Oracle);
        CheckLimitOpt = IntegerOptionItem.Create(Id + 10, "OracleSkillLimit", new(0, 10, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Oracle])
            .SetValueFormat(OptionFormat.Times);
        //    OracleCheckMode = BooleanOptionItem.Create(Id + 11, "AccurateCheckMode", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Oracle]);
        HidesVote = BooleanOptionItem.Create(Id + 12, "OracleHideVote", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Oracle]);
        //  OverrideTasksData.Create(Id + 20, TabGroup.CrewmateRoles, CustomRoles.Oracle);
        FailChance = IntegerOptionItem.Create(Id + 13, "FailChance", new(0, 100, 5), 0, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Oracle])
            .SetValueFormat(OptionFormat.Percent);
        OracleAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 14, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Oracle])
            .SetValueFormat(OptionFormat.Times);
        ChangeRecruitTeam = BooleanOptionItem.Create(Id+15,"OracleCheckAddons",false,TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Oracle]);

    }
    public override void Init()
    {
        playerIdList = [];
        CheckLimit = [];
        TempCheckLimit = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        CheckLimit.TryAdd(playerId, CheckLimitOpt.GetInt());
        On = true;
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        CheckLimit.Remove(playerId);
    }
    public override bool HideVote(byte playerId) => CheckRole(playerId, CustomRoles.Oracle) && Oracle.HidesVote.GetBool() && Oracle.TempCheckLimit[playerId] > 0;
    public static void SendRPC(byte playerId, bool isTemp = false)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Oracle);
        writer.Write(playerId);
        writer.Write(isTemp);
        if (!isTemp) writer.Write(CheckLimit[playerId]);
        else writer.Write(TempCheckLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte pid = reader.ReadByte();
        bool isTemp = reader.ReadBoolean();
        if (!isTemp)
        {
            float checkLimit = reader.ReadSingle();
            CheckLimit[pid] = checkLimit;
        }
        else
        {
            float tempLimit = reader.ReadSingle();
            TempCheckLimit[pid] = tempLimit;
        }
    }
    public override void OnVote(PlayerControl player, PlayerControl target)
    {
        if (player == null || target == null) return;
        if (didVote.Contains(player.PlayerId)) return;
        didVote.Add(player.PlayerId);

        if (CheckLimit[player.PlayerId] < 1)
        {
            Utils.SendMessage(GetString("OracleCheckReachLimit"), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Oracle), GetString("OracleCheckMsgTitle")));
            return;
        }

        CheckLimit[player.PlayerId] -= 1;
        SendRPC(player.PlayerId);

        if (player.PlayerId == target.PlayerId)
        {
            Utils.SendMessage(GetString("OracleCheckSelfMsg") + "\n\n" + string.Format(GetString("OracleCheckLimit"), CheckLimit[player.PlayerId]), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Oracle), GetString("OracleCheckMsgTitle")));
            return;
        }

        {
            string msg;

        {

                string text = "Crewmate";
                if (ChangeRecruitTeam.GetBool())
                {
                    if (target.Is(CustomRoles.Admired)) text = "Crewmate";
                    else if (target.GetCustomRole().IsImpostorTeamV2() || target.GetCustomSubRoles().Any(role => role.IsImpostorTeamV2())) text = "Impostor";
                    else if (target.GetCustomRole().IsNeutralTeamV2() || target.GetCustomSubRoles().Any(role => role.IsNeutralTeamV2())) text = "Neutral";
                    else if (target.GetCustomRole().IsCrewmateTeamV2() && (target.GetCustomSubRoles().Any(role => role.IsCrewmateTeamV2()) || (target.GetCustomSubRoles().Count == 0))) text = "Crewmate";
                }
                else 
                { 
                    if (target.GetCustomRole().IsImpostor() && !target.Is(CustomRoles.Trickster)) text = "Impostor";
                    else if (target.GetCustomRole().IsNeutral()) text = "Neutral";
                    else text = "Crewmate";
                }
               
                if (FailChance.GetInt() > 0)
                {
                    int random_number_1 = HashRandom.Next(1, 100);
                    if (random_number_1 <= FailChance.GetInt())
                    {
                        int random_number_2 = HashRandom.Next(1, 3);
                        if (text == "Crewmate")
                        {
                            if (random_number_2 == 1) text = "Neutral";
                            if (random_number_2 == 2) text = "Impostor";
                        }
                        if (text == "Neutral")
                        {
                            if (random_number_2 == 1) text = "Crewmate";
                            if (random_number_2 == 2) text = "Impostor";
                        }
                        if (text == "Impostor")
                        {
                            if (random_number_2 == 1) text = "Neutral";
                            if (random_number_2 == 2) text = "Crewmate";
                        }
                    }
                }
                msg = string.Format(GetString("OracleCheck." + text), target.GetRealName());
        }

        Utils.SendMessage(GetString("OracleCheck") + "\n" + msg + "\n\n" + string.Format(GetString("OracleCheckLimit"), CheckLimit[player.PlayerId]), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Oracle), GetString("OracleCheckMsgTitle")));}
    }
    public override void OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (!player.IsAlive()) return;
        Oracle.CheckLimit[player.PlayerId] += Oracle.OracleAbilityUseGainWithEachTaskCompleted.GetFloat();
        Oracle.SendRPC(player.PlayerId);
    }
    public override void OnReportDeadBody(PlayerControl reporter, PlayerControl tagret)
    {
        didVote.Clear();
        foreach (var oracleId in CheckLimit.Keys)
        {
            TempCheckLimit[oracleId] = CheckLimit[oracleId];
            SendRPC(oracleId, isTemp: true);
        }
    }
    public override string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        var taskState9 = Main.PlayerStates?[playerId].TaskState;
        Color TextColor9;
        var TaskCompleteColor9 = Color.green;
        var NonCompleteColor9 = Color.yellow;
        var NormalColor9 = taskState9.IsTaskFinished ? TaskCompleteColor9 : NonCompleteColor9;
        TextColor9 = comms ? Color.gray : NormalColor9;
        string Completed9 = comms ? "?" : $"{taskState9.CompletedTasksCount}";
        Color TextColor91;
        if (Oracle.CheckLimit[playerId] < 1) TextColor91 = Color.red;
        else TextColor91 = Color.white;
        ProgressText.Append(ColorString(TextColor9, $"({Completed9}/{taskState9.AllTasksCount})"));
        ProgressText.Append(ColorString(TextColor91, $" <color=#ffffff>-</color> {Math.Round(Oracle.CheckLimit[playerId], 1)}"));
        return ProgressText.ToString();
    }
}