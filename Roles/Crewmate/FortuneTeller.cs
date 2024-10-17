using Hazel;
using InnerNet;
using System;
using System.Text;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class FortuneTeller : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 8000;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.FortuneTeller);
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem CheckLimitOpt;
    private static OptionItem AccurateCheckMode;
    private static OptionItem ShowSpecificRole;
    private static OptionItem AbilityUseGainWithEachTaskCompleted;
    private static OptionItem RandomActiveRoles;


    private readonly HashSet<byte> didVote = [];
    private float TempCheckLimit;
    private readonly HashSet<byte> targetList = [];


    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.FortuneTeller);
        CheckLimitOpt = IntegerOptionItem.Create(Id + 10, GeneralOption.SkillLimitTimes, new(0, 20, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.FortuneTeller])
            .SetValueFormat(OptionFormat.Times);
        RandomActiveRoles = BooleanOptionItem.Create(Id + 11, "RandomActiveRoles", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.FortuneTeller]);
        AccurateCheckMode = BooleanOptionItem.Create(Id + 12, "AccurateCheckMode", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.FortuneTeller]);
        ShowSpecificRole = BooleanOptionItem.Create(Id + 13, "ShowSpecificRole", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.FortuneTeller]);
        AbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 15, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.FortuneTeller])
            .SetValueFormat(OptionFormat.Times);
        OverrideTasksData.Create(Id + 20, TabGroup.CrewmateRoles, CustomRoles.FortuneTeller);
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = CheckLimitOpt.GetInt();
    }

    public void SendRPC(byte playerId, bool isTemp = false, bool voted = false)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
        writer.Write(isTemp);

        if (!isTemp)
        {
            writer.Write(playerId);
            writer.Write(AbilityLimit);
            writer.Write(voted);
        }
        else
        {
            writer.Write(playerId);
            writer.Write(TempCheckLimit);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        bool isTemp = reader.ReadBoolean();
        byte playerId = reader.ReadByte();
        float limit = reader.ReadSingle();
        if (!isTemp)
        {
            AbilityLimit = limit;
            bool voted = reader.ReadBoolean();
            if (voted && !didVote.Contains(playerId)) didVote.Add(playerId);
        }
        else
        {
            TempCheckLimit = limit;
            didVote.Remove(playerId);
        }
    }

    private static string GetTargetRoleList(CustomRoles[] roles)
    {
        return roles != null ? string.Join("\n", roles.Select(role => $"    ★ {GetRoleName(role)}")) : "";
    }
    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (player.Is(CustomRoles.FortuneTeller) && player.IsAlive())
        {
            AbilityLimit += AbilityUseGainWithEachTaskCompleted.GetFloat();
            SendRPC(player.PlayerId);
        }
        return true;
    }
    public override bool CheckVote(PlayerControl player, PlayerControl target)
    {
        if (player == null || target == null) return true;
        if (didVote.Contains(player.PlayerId)) return true;
        didVote.Add(player.PlayerId);

        if (AbilityLimit < 1)
        {
            SendMessage(GetString("FortuneTellerCheckReachLimit"), player.PlayerId, ColorString(GetRoleColor(CustomRoles.FortuneTeller), GetString("FortuneTellerCheckMsgTitle")));
            return true;
        }

        if (RandomActiveRoles.GetBool())
        {
            if (targetList.Contains(target.PlayerId))
            {
                SendMessage(GetString("FortuneTellerAlreadyCheckedMsg") + "\n\n" + string.Format(GetString("FortuneTellerCheckLimit"), AbilityLimit), player.PlayerId, ColorString(GetRoleColor(CustomRoles.FortuneTeller), GetString("FortuneTellerCheckMsgTitle")));
                return true;
            }
        }

        AbilityLimit -= 1;
        SendRPC(player.PlayerId, voted: true);

        if (player.PlayerId == target.PlayerId)
        {
            SendMessage(GetString("FortuneTellerCheckSelfMsg") + "\n\n" + string.Format(GetString("FortuneTellerCheckLimit"), AbilityLimit), player.PlayerId, ColorString(GetRoleColor(CustomRoles.FortuneTeller), GetString("FortuneTellerCheckMsgTitle")));
            return true;
        }

        string msg;

        if ((player.AllTasksCompleted() || AccurateCheckMode.GetBool()) && ShowSpecificRole.GetBool())
        {
            msg = string.Format(GetString("FortuneTellerCheck.TaskDone"), target.GetRealName(), GetString(target.GetCustomRole().ToString()));
        }
        else if (RandomActiveRoles.GetBool())
        {
            targetList.Add(target.PlayerId);
            var targetRole = target.GetCustomRole();
            var activeRoleList = CustomRolesHelper.AllRoles.Where(role => (role.IsEnable() || role.RoleExist(countDead: true)) && role != targetRole && !role.IsAdditionRole()).ToList();
            var count = Math.Min(4, activeRoleList.Count);
            List<CustomRoles> roleList = [targetRole];
            var rand = IRandom.Instance;
            for (int i = 0; i < count; i++)
            {
                int randomIndex = rand.Next(activeRoleList.Count);
                roleList.Add(activeRoleList[randomIndex]);
                activeRoleList.RemoveAt(randomIndex);
            }
            for (int i = roleList.Count - 1; i > 0; i--)
            {
                int j = rand.Next(0, i + 1);
                (roleList[j], roleList[i]) = (roleList[i], roleList[j]);
            }
            var text = GetTargetRoleList([.. roleList]);
            msg = string.Format(GetString("FortuneTellerCheck.Result"), target.GetRealName(), text);
        }
        else
        {
            List<CustomRoles[]> completeRoleList = EnumHelper.Achunk<CustomRoles>(chunkSize: 6, shuffle: true, exclude: (x) => !x.IsGhostRole() && !x.IsAdditionRole() && !x.IsVanilla() && x is not CustomRoles.NotAssigned and not CustomRoles.ChiefOfPolice and not CustomRoles.Killer and not CustomRoles.GM);

            var targetRole = target.GetCustomRole();
            string text = string.Empty;

            text = GetTargetRoleList(completeRoleList.FirstOrDefault(x => x.Contains(targetRole)));

            if (text == string.Empty)
            {
                msg = string.Format(GetString("FortuneTellerCheck.Null"), target.GetRealName());
            }
            else
            {
                msg = string.Format(GetString("FortuneTellerCheck.Result"), target.GetRealName(), text);
            }
        }

        SendMessage(GetString("FortuneTellerCheck") + "\n" + msg + "\n\n" + string.Format(GetString("FortuneTellerCheckLimit"), AbilityLimit), player.PlayerId, ColorString(GetRoleColor(CustomRoles.FortuneTeller), GetString("FortuneTellerCheckMsgTitle")));
        SendMessage(GetString("VoteHasReturned"), player.PlayerId, title: ColorString(GetRoleColor(CustomRoles.FortuneTeller), string.Format(GetString("VoteAbilityUsed"), GetString("FortuneTeller"))));
        return false;
    }
    public override string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        var taskState4 = Main.PlayerStates?[playerId].TaskState;
        Color TextColor4;
        var TaskCompleteColor4 = Color.green;
        var NonCompleteColor4 = Color.yellow;
        var NormalColor4 = taskState4.IsTaskFinished ? TaskCompleteColor4 : NonCompleteColor4;
        TextColor4 = comms ? Color.gray : NormalColor4;
        string Completed4 = comms ? "?" : $"{taskState4.CompletedTasksCount}";
        Color TextColor41;
        if (AbilityLimit < 1) TextColor41 = Color.red;
        else TextColor41 = Color.white;
        ProgressText.Append(ColorString(TextColor4, $"({Completed4}/{taskState4.AllTasksCount})"));
        ProgressText.Append(ColorString(TextColor41, $" <color=#ffffff>-</color> {Math.Round(AbilityLimit)}"));
        return ProgressText.ToString();
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        didVote.Clear();

        TempCheckLimit = AbilityLimit;
        SendRPC(_state.PlayerId, isTemp: true);

    }
}
