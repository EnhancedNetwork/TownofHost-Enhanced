using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

public static class Benefactor
{
    private static readonly int Id = 24000;
    //private static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    public static Dictionary<byte, List<int>> taskIndex = new();
    public static Dictionary<byte, int> TaskMarkPerRound = new();
    private static int maxTasksMarkedPerRound = new();
    public static Dictionary<byte, long> shieldedPlayers = new();

    public static OptionItem TaskMarkPerRoundOpt;
    public static OptionItem ShieldDuration;
    public static OptionItem ShieldIsOneTimeUse;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Benefactor);
        TaskMarkPerRoundOpt = IntegerOptionItem.Create(Id + 10, "TaskMarkPerRound", new(1, 14, 1), 3, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Benefactor])
            .SetValueFormat(OptionFormat.Votes);
        ShieldDuration = FloatOptionItem.Create(Id + 11, "ShieldDuration", new(1, 30, 1), 10, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Benefactor])
            .SetValueFormat(OptionFormat.Votes);
        ShieldIsOneTimeUse = BooleanOptionItem.Create(Id + 12, "ShieldIsOneTimeUse", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Benefactor]);
        Options.OverrideTasksData.Create(Id + 13, TabGroup.CrewmateRoles, CustomRoles.Benefactor);
    }

    public static void Init()
    {
        //playerIdList = new();
        taskIndex = new();
        shieldedPlayers = new();
        TaskMarkPerRound = new();
        IsEnable = false;
        maxTasksMarkedPerRound = TaskMarkPerRoundOpt.GetInt();
    }
    public static void Add(byte playerId)
    {
        //playerIdList.Add(playerId);
        TaskMarkPerRound[playerId] = 0;
        IsEnable = true;
    }

    private static void SendRPC(int type, byte benefactorId = 0xff, byte targetId = 0xff, int taskIndex = -1)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.BenefactorRPC, SendOption.Reliable, -1);
        writer.Write(type);
        if (type == 0)
        {
            writer.Write(benefactorId);
        }
        if (type == 2)
        {
            writer.Write(benefactorId);
            writer.Write(TaskMarkPerRound[benefactorId]);
            writer.Write(taskIndex);
        }
        if (type == 3)
        {
            writer.Write(benefactorId);
            writer.Write(taskIndex);
            writer.Write(targetId);
            writer.Write(shieldedPlayers[targetId].ToString());
        }
        if (type == 4)
        {
            writer.Write(targetId);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);

    }

    public static void ReceiveRPC(MessageReader reader)
    {
        int type = reader.ReadInt32();
        if (type == 0)
        {
            byte benefactorId = reader.ReadByte();
            TaskMarkPerRound[benefactorId] = 0;
            if (taskIndex.ContainsKey(benefactorId)) taskIndex[benefactorId].Clear();
        }
        if (type == 1) shieldedPlayers.Clear();
        if (type == 2)
        {
            byte benefactorId = reader.ReadByte();
            int taskMarked = reader.ReadInt32();
            TaskMarkPerRound[benefactorId] = taskMarked;
            int taskInd = reader.ReadInt32();
            if (!taskIndex.ContainsKey(benefactorId)) taskIndex[benefactorId] = new();
            taskIndex[benefactorId].Add(taskInd);
        }
        if (type == 3)
        {
            byte benefactorId = reader.ReadByte();
            int taskInd = reader.ReadInt32();
            if (!taskIndex.ContainsKey(benefactorId)) taskIndex[benefactorId] = new();
            taskIndex[benefactorId].Remove(taskInd);
            byte targetId = reader.ReadByte();
            string stimeStamp = reader.ReadString();
            if (long.TryParse(stimeStamp, out long timeStamp)) shieldedPlayers[targetId] = timeStamp;
        }
        if (type == 4)
        {
            byte targetId = reader.ReadByte();
            shieldedPlayers.Remove(targetId);
        }
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
            SendRPC(type: 0, benefactorId: playerId); //clear taskindex
        }
        if (shieldedPlayers.Any())
        {
            shieldedPlayers.Clear();
            SendRPC(type: 1); //clear all shielded players
        }
    }

    public static void OnTasKComplete(PlayerControl player, PlayerTask task)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!IsEnable) return;
        if (player == null) return;
        if (!player.IsAlive()) return;
        byte playerId = player.PlayerId;
        if (player.Is(CustomRoles.Benefactor))
        {
            if (!TaskMarkPerRound.ContainsKey(playerId)) TaskMarkPerRound[playerId] = 0;
            if (TaskMarkPerRound[playerId] >= maxTasksMarkedPerRound)
            {
                TaskMarkPerRound[playerId] = maxTasksMarkedPerRound;
                Logger.Info($"Max task per round ({TaskMarkPerRound[playerId]}) reached for {player.GetNameWithRole()}", "Benefactor");
                return;
            }
            TaskMarkPerRound[playerId]++;
            if (!taskIndex.ContainsKey(playerId)) taskIndex[playerId] = new();
            taskIndex[playerId].Add(task.Index);
            SendRPC(type: 2, benefactorId: playerId, taskIndex: task.Index); //add in task mark per round and taskindex
            player.Notify(GetString("BenefactorTaskMarked"));
        }
        else
        {
            foreach (var benefactorId in taskIndex.Keys)
            {
                if (taskIndex[benefactorId].Contains(task.Index))
                {
                    var benefactorPC = Utils.GetPlayerById(benefactorId);
                    if (benefactorPC == null) continue;

                    player.Notify(GetString("BenefactorTargetGotShield"));

                    long now = Utils.GetTimeStamp();
                    shieldedPlayers[playerId] = now;
                    taskIndex[benefactorId].Remove(task.Index);
                    SendRPC(type: 3, benefactorId: benefactorId, targetId: playerId, taskIndex: task.Index); // remove taskindex and add shieldedPlayer time
                    Logger.Info($"{player.GetAllRoleName()} got a shield from {benefactorPC.GetNameWithRole()}", "Benefactor");
                }
            }
        }
    }

    public static void OnFixedUpdate()
    {
        if (!IsEnable) return;
        var now = Utils.GetTimeStamp();
        foreach (var x in shieldedPlayers.Where(x => x.Value + ShieldDuration.GetInt() < now))
        {
            var target = x.Key;
            shieldedPlayers.Remove(target);
            SendRPC(type: 4, targetId: target); //remove shieldedPlayer
        }
    }

    public static bool OnCheckMurder(PlayerControl target)
    {
        if (!IsEnable) return true;
        if (target == null) return true;
        if (!shieldedPlayers.ContainsKey(target.PlayerId)) return true;
        if (ShieldIsOneTimeUse.GetBool())
        {
            shieldedPlayers.Remove(target.PlayerId);
            SendRPC(type:4 , targetId: target.PlayerId);
            Logger.Info($"{target.GetNameWithRole()} shield broken", "BenefactorShieldBroken");
        }
        return false;

    }

}

