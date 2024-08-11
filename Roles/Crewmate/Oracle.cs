using Hazel;
using System.Text;
using System;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;
using InnerNet;

namespace TOHE.Roles.Crewmate;

internal class Oracle : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 9100;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Oracle);
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem CheckLimitOpt;
    private static OptionItem FailChance;
    private static OptionItem OracleAbilityUseGainWithEachTaskCompleted;
    private static OptionItem ChangeRecruitTeam;

    private readonly HashSet<byte> DidVote = [];
    private static readonly Dictionary<byte, float> TempCheckLimit = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Oracle);
        CheckLimitOpt = IntegerOptionItem.Create(Id + 10, "OracleSkillLimit", new(0, 10, 1), 1, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Oracle])
            .SetValueFormat(OptionFormat.Times);
        FailChance = IntegerOptionItem.Create(Id + 13, "FailChance", new(0, 100, 5), 0, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Oracle])
            .SetValueFormat(OptionFormat.Percent);
        OracleAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 14, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Oracle])
            .SetValueFormat(OptionFormat.Times);
        ChangeRecruitTeam = BooleanOptionItem.Create(Id+15,"OracleCheckAddons",false,TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Oracle]);

    }
    public override void Init()
    {
        TempCheckLimit.Clear();
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = CheckLimitOpt.GetFloat();
    }
    public void SendRPC(byte playerId, bool isTemp = false)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
        writer.Write(playerId);
        writer.Write(isTemp);
        if (!isTemp) writer.Write(AbilityLimit);
        else writer.Write(TempCheckLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte pid = reader.ReadByte();
        bool isTemp = reader.ReadBoolean();
        if (!isTemp)
        {
            float checkLimit = reader.ReadSingle();
            AbilityLimit = checkLimit;
        }
        else
        {
            float tempLimit = reader.ReadSingle();
            TempCheckLimit[pid] = tempLimit;
        }
    }
    public override bool CheckVote(PlayerControl player, PlayerControl target)
    {
        if (player == null || target == null) return true;
        if (DidVote.Contains(player.PlayerId)) return true;
        DidVote.Add(player.PlayerId);

        if (AbilityLimit < 1)
        {
            SendMessage(GetString("OracleCheckReachLimit"), player.PlayerId, ColorString(GetRoleColor(CustomRoles.Oracle), GetString("OracleCheckMsgTitle")));
            return true;
        }

        AbilityLimit -= 1;
        SendRPC(player.PlayerId);

        if (player.PlayerId == target.PlayerId)
        {
            SendMessage(GetString("OracleCheckSelfMsg") + "\n\n" + string.Format(GetString("OracleCheckLimit"), AbilityLimit), player.PlayerId, ColorString(GetRoleColor(CustomRoles.Oracle), GetString("OracleCheckMsgTitle")));
            return true;
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

            SendMessage(GetString("OracleCheck") + "\n" + msg + "\n\n" + string.Format(GetString("OracleCheckLimit"), AbilityLimit), player.PlayerId, ColorString(GetRoleColor(CustomRoles.Oracle), GetString("OracleCheckMsgTitle")));
            SendMessage(GetString("VoteHasReturned"), player.PlayerId, title: ColorString(GetRoleColor(CustomRoles.Oracle), string.Format(GetString("VoteAbilityUsed"), GetString("Oracle"))));
            return false;
        }
    }
    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (player.IsAlive())
        {
            AbilityLimit += OracleAbilityUseGainWithEachTaskCompleted.GetFloat();
            SendRPC(player.PlayerId);
        }
        return true;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo tagret)
    {
        DidVote.Clear();
            
        TempCheckLimit[_state.PlayerId] = AbilityLimit;
        SendRPC(_state.PlayerId, isTemp: true);
        
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
        if (AbilityLimit < 1) TextColor91 = Color.red;
        else TextColor91 = Color.white;
        ProgressText.Append(ColorString(TextColor9, $"({Completed9}/{taskState9.AllTasksCount})"));
        ProgressText.Append(ColorString(TextColor91, $" <color=#ffffff>-</color> {Math.Round(AbilityLimit, 1)}"));
        return ProgressText.ToString();
    }
}