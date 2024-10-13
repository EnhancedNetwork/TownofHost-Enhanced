using Hazel;
using System;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Benefactor : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 26400;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem TaskMarkPerRoundOpt;
    private static OptionItem ShieldDuration;
    private static OptionItem ShieldIsOneTimeUse;

    private static int maxTasksMarkedPerRound = new();

    private static readonly Dictionary<byte, HashSet<int>> taskIndex = [];
    private static readonly Dictionary<byte, int> TaskMarkPerRound = [];
    private static readonly Dictionary<byte, long> shieldedPlayers = [];

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Benefactor);
        TaskMarkPerRoundOpt = IntegerOptionItem.Create(Id + 10, "TasksMarkPerRound", new(1, 14, 1), 3, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Benefactor])
            .SetValueFormat(OptionFormat.Votes);
        ShieldDuration = FloatOptionItem.Create(Id + 11, "ShieldDuration", new(1, 30, 1), 10, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Benefactor])
            .SetValueFormat(OptionFormat.Votes);
        ShieldIsOneTimeUse = BooleanOptionItem.Create(Id + 12, "ShieldIsOneTimeUse", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Benefactor]);
        Options.OverrideTasksData.Create(Id + 13, TabGroup.CrewmateRoles, CustomRoles.Benefactor);
    }

    public override void Init()
    {
        playerIdList.Clear();
        taskIndex.Clear();
        shieldedPlayers.Clear();
        TaskMarkPerRound.Clear();
        maxTasksMarkedPerRound = TaskMarkPerRoundOpt.GetInt();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        TaskMarkPerRound[playerId] = 0;
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        TaskMarkPerRound.Remove(playerId);
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
            if (!taskIndex.ContainsKey(benefactorId)) taskIndex[benefactorId] = [];
            taskIndex[benefactorId].Add(taskInd);
        }
        if (type == 3)
        {
            byte benefactorId = reader.ReadByte();
            int taskInd = reader.ReadInt32();
            if (!taskIndex.ContainsKey(benefactorId)) taskIndex[benefactorId] = [];
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

    public override string GetProgressText(byte PlayerId, bool comms)
    {
        if (!TaskMarkPerRound.ContainsKey(PlayerId)) TaskMarkPerRound[PlayerId] = 0;
        int markedTasks = TaskMarkPerRound[PlayerId];
        int x = Math.Max(maxTasksMarkedPerRound - markedTasks, 0);
        return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Taskinator).ShadeColor(0.25f), $"({x})");
    }

    public override void AfterMeetingTasks()
    {
        foreach (var playerId in TaskMarkPerRound.Keys.ToArray())
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

    public override void OnOthersTaskComplete(PlayerControl player, PlayerTask task) // runs for every player which compeletes a task
    {
        if (!AmongUsClient.Instance.AmHost) return;
        
        if (!HasEnabled) return;
        if (player == null || _Player == null) return;
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
            if (!taskIndex.ContainsKey(playerId)) taskIndex[playerId] = [];
            taskIndex[playerId].Add(task.Index);
            SendRPC(type: 2, benefactorId: playerId, taskIndex: task.Index); //add in task mark per round and taskindex
            player.Notify(GetString("BenefactorTaskMarked"));
        }
        else
        {
            foreach (var benefactorId in taskIndex.Keys.ToArray())
            {
                if (taskIndex[benefactorId].Contains(task.Index))
                {
                    var benefactorPC = Utils.GetPlayerById(benefactorId);
                    if (benefactorPC == null) continue;

                    player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Benefactor), GetString("BenefactorTargetGotShield")));
                    player.RpcGuardAndKill();

                    long now = Utils.GetTimeStamp();
                    shieldedPlayers[playerId] = now;
                    taskIndex[benefactorId].Remove(task.Index);
                    SendRPC(type: 3, benefactorId: benefactorId, targetId: playerId, taskIndex: task.Index); // remove taskindex and add shieldedPlayer time
                    Logger.Info($"{player.GetAllRoleName()} got a shield from {benefactorPC.GetNameWithRole()}", "Benefactor");
                }
            }
        }
    }

    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        if (!shieldedPlayers.ContainsKey(target.PlayerId)) return false;

        if (ShieldIsOneTimeUse.GetBool())
        {
            shieldedPlayers.Remove(target.PlayerId);
            SendRPC(type: 4, targetId: target.PlayerId);
            Logger.Info($"{target.GetNameWithRole()} shield broken", "BenefactorShieldBroken");
        }
        killer.RpcGuardAndKill();
        killer.SetKillCooldown();
        return true;
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (lowLoad) return;
        foreach (var shieldedData in shieldedPlayers.Where(x => x.Value + ShieldDuration.GetInt() < nowTime).ToArray())
        {
            var targetId = shieldedData.Key;
            var target = targetId.GetPlayer();

            shieldedPlayers.Remove(targetId);
            target?.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Benefactor), GetString("BKProtectOut")));
            target?.RpcGuardAndKill();
            
            SendRPC(type: 4, targetId: targetId);
        }
    }
}

