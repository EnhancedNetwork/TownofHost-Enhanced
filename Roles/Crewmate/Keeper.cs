using Hazel;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static TOHE.Translator;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate;
public static class Keeper
{
    private static readonly int Id = 26500;
    //public static List<byte> playerIdList = [];
    public static bool IsEnable = false;

    public static List<byte> keeperTarget = [];
    public static Dictionary<byte, int> keeperUses = [];
    public static Dictionary<byte, bool> DidVote = [];

    public static OptionItem KeeperUsesOpt;
    public static OptionItem HideVote;
    public static OptionItem AbilityUseGainWithEachTaskCompleted;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Keeper);
        KeeperUsesOpt = IntegerOptionItem.Create(Id + 10, "MaxProtections", new(1, 14, 1), 3, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Keeper])
            .SetValueFormat(OptionFormat.Times);
        HideVote = BooleanOptionItem.Create(Id + 11, "KeeperHideVote", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Keeper]);
        //    AbilityUseGainWithEachTaskCompleted = IntegerOptionItem.Create(Id + 12, "AbilityUseGainWithEachTaskCompleted", new(0, 5, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cleanser])
        //        .SetValueFormat(OptionFormat.Times);

    }
    public static void Init()
    {
        //playerIdList = [];
        keeperTarget = [];
        keeperUses = [];
        DidVote = [];
        IsEnable = false;
    }

    public static void Add(byte playerId)
    {
        //playerIdList.Add(playerId);
        DidVote.Add(playerId, false);
        keeperUses[playerId] = 0;
        IsEnable = true;
    }
    public static void Remove(byte playerId)
    {
        DidVote.Remove(playerId);
        keeperUses.Remove(playerId);
    }

    public static string GetProgressText(byte playerId, bool comms)
    {
        if (playerId == byte.MaxValue) return string.Empty;
        if (!keeperUses.ContainsKey(playerId)) return string.Empty;
        var ProgressText = new StringBuilder();
        var taskState8 = Main.PlayerStates?[playerId].GetTaskState();
        Color TextColor8;
        var TaskCompleteColor8 = Color.green;
        var NonCompleteColor8 = Color.yellow;
        var NormalColor8 = taskState8.IsTaskFinished ? TaskCompleteColor8 : NonCompleteColor8;
        TextColor8 = comms ? Color.gray : NormalColor8;
        string Completed8 = comms ? "?" : $"{taskState8.CompletedTasksCount}";
        Color TextColor81;
        var maxUses = KeeperUsesOpt.GetInt();
        var usesLeft = Math.Max(maxUses - keeperUses[playerId], 0);
        if (usesLeft < 1) TextColor81 = Color.red;
        else TextColor81 = Utils.GetRoleColor(CustomRoles.Keeper);
        ProgressText.Append(Utils.ColorString(TextColor8, $"({Completed8}/{taskState8.AllTasksCount})"));
        ProgressText.Append(Utils.ColorString(TextColor81, $" <color=#ffffff>-</color> {usesLeft}"));
        return ProgressText.ToString();
        
        //Color x;
        //if (KeeperUsesOpt.GetInt() - keeperUses[playerId] > 0)
        //    x = Utils.GetRoleColor(CustomRoles.Cleanser);
        //else x = Color.gray;
        //return (Utils.ColorString(x, $"({KeeperUsesOpt.GetInt() - keeperUses[playerId]})"));
    }

    private static void SendRPC(int type, byte keeperId = 0xff, byte targetId = 0xff)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.KeeperRPC, SendOption.Reliable, -1);
        writer.Write(type);
        if (type == 0)
        {
            writer.Write(keeperId);
            writer.Write(keeperUses[keeperId]);
            writer.Write(targetId);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        int type = reader.ReadInt32();
        if (type == 0)
        {
            byte keeperId = reader.ReadByte();
            DidVote[keeperId] = true;
            
            int uses = reader.ReadInt32();
            keeperUses[keeperId] = uses;

            byte targetId = reader.ReadByte();
            if (!keeperTarget.Contains(targetId)) keeperTarget.Add(targetId);
        }

        else if (type == 1)
        {
            foreach (var pid in DidVote.Keys)
            {
                DidVote[pid] = false;
            }
            keeperTarget.Clear();
        }
    }

    public static bool OnVote(PlayerControl voter, PlayerControl target)
    {
        if (!IsEnable) return true;
        if (voter == null || target == null) return true;
        if (!voter.Is(CustomRoles.Keeper)) return true;
        if (DidVote[voter.PlayerId]) return true;
        DidVote[voter.PlayerId] = true;
        if (keeperTarget.Contains(target.PlayerId)) return true;
        if (keeperUses[voter.PlayerId] >= KeeperUsesOpt.GetInt()) return true;

        keeperUses[voter.PlayerId]++;
        keeperTarget.Add(target.PlayerId);
        Logger.Info($"{voter.GetNameWithRole()} chosen as keeper target by {target.GetNameWithRole()}", "Keeper");
        SendRPC(type:0, keeperId: voter.PlayerId, targetId: target.PlayerId); // add keeperUses, KeeperTarget and DidVote
        Utils.SendMessage(string.Format(GetString("KeeperProtect"), target.GetRealName()), voter.PlayerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.Keeper), GetString("KeeperTitle")));
        return false;
    }

    public static void AfterMeetingTasks()
    {
        foreach (var pid in DidVote.Keys)
        {
            DidVote[pid] = false;
        }
        keeperTarget.Clear();
        SendRPC(type: 1);
    }

    public static bool IsTargetExiled(byte exileId) => keeperTarget.Contains(exileId);
}