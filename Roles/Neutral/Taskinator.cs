using Hazel;
using System;
using System.Collections.Generic;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

public static class Taskinator
{
    private static readonly int Id = 13700;
    private static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    public static Dictionary<byte, List<int>> taskIndex = new();
    public static Dictionary<byte, int> TaskMarkPerRound = new();
    private static int maxTasksMarkedPerRound = new();

    public static OptionItem TaskMarkPerRoundOpt;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Taskinator);
        TaskMarkPerRoundOpt = IntegerOptionItem.Create(Id + 10, "TaskMarkPerRound", new(1, 14, 1), 3, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Taskinator])
            .SetValueFormat(OptionFormat.Votes);
        Options.OverrideTasksData.Create(Id + 11, TabGroup.NeutralRoles, CustomRoles.Taskinator);
    }

    public static void Init()
    {
        playerIdList = new();
        taskIndex = new();
        TaskMarkPerRound = new();
        IsEnable = false;
        maxTasksMarkedPerRound = TaskMarkPerRoundOpt.GetInt();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        TaskMarkPerRound[playerId] = 0;
        IsEnable = true;
    }


    private static void SendRPC(byte taskinatorID, int taskIndex = -1, bool isKill = false, bool clearAll = false)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.TaskinatorMarkedTask, SendOption.Reliable, -1);
        writer.Write(taskinatorID);
        writer.Write(taskIndex);
        writer.Write(isKill);
        writer.Write(clearAll);
        if (!isKill)
        {            
            writer.Write(TaskMarkPerRound[taskinatorID]);   
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte taskinatorID = reader.ReadByte();
        int taskInd = reader.ReadInt32();
        bool isKill = reader.ReadBoolean();
        bool clearAll = reader.ReadBoolean();
        if (!isKill)
        {
            int uses = reader.ReadInt32();
            TaskMarkPerRound[taskinatorID] = uses;
            if (!clearAll) 
            { 
                if (!taskIndex.ContainsKey(taskinatorID)) taskIndex[taskinatorID] = new();
                taskIndex[taskinatorID].Add(taskInd);
            }
        }
        else
        {
            if (taskIndex.ContainsKey(taskinatorID)) taskIndex[taskinatorID].Remove(taskInd);
        }
        if (clearAll && taskIndex.ContainsKey(taskinatorID)) taskIndex[taskinatorID].Clear(); 
    }

    public static string GetProgressText(byte playerId)
    {
        if (!TaskMarkPerRound.ContainsKey(playerId)) TaskMarkPerRound[playerId] = 0;
        int markedTasks = TaskMarkPerRound[playerId];
        int x = Math.Max(maxTasksMarkedPerRound - markedTasks, 0);
        return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Taskinator).ShadeColor(0.25f), $"({x})");
    }

    public static void AfterMeetingTasks()
    {
        if (!IsEnable) return;
        foreach (var playerId in TaskMarkPerRound.Keys)
        {
            TaskMarkPerRound[playerId] = 0;
            if (taskIndex.ContainsKey(playerId)) taskIndex[playerId].Clear();
            SendRPC(playerId, clearAll: true);
        }
    }

    public static void OnTasKComplete(PlayerControl player, PlayerTask task)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if(!IsEnable) return;
        if (player == null) return;
        byte playerId = player.PlayerId;
        if (player.Is(CustomRoles.Taskinator))
        {
            if (!TaskMarkPerRound.ContainsKey(playerId)) TaskMarkPerRound[playerId] = 0;
            if (TaskMarkPerRound[playerId] >= maxTasksMarkedPerRound)
            {
                TaskMarkPerRound[playerId] = maxTasksMarkedPerRound;
                Logger.Info($"Max task per round ({TaskMarkPerRound[playerId]}) reached for {player.GetNameWithRole()}", "Taskinator");
                return;
            }
            TaskMarkPerRound[playerId]++;
            if (!taskIndex.ContainsKey(playerId)) taskIndex[playerId] = new();
            taskIndex[playerId].Add(task.Index);
            SendRPC(taskinatorID : playerId, taskIndex : task.Index);
            player.Notify(GetString("TaskinatorBombPlanted"));
        }
        else
        {
            foreach (var taskinatorId in taskIndex.Keys)
            { 
                if (taskIndex[taskinatorId].Contains(task.Index))
                {
                    var taskinatorPC = Utils.GetPlayerById(taskinatorId);
                    if (taskinatorPC == null) continue;

                    Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                    player.SetRealKiller(taskinatorPC);
                    player.RpcMurderPlayerV3(player);

                    taskIndex[taskinatorId].Remove(task.Index);
                    SendRPC(taskinatorID : taskinatorId, taskIndex:task.Index, isKill : true);
                    Logger.Info($"{player.GetAllRoleName()} died because of {taskinatorPC.GetNameWithRole()}", "Taskinator");
                }
            }
        }
    }
}

