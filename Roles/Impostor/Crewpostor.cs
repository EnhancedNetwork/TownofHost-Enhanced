using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using static TOHE.Options;
using Hazel;

namespace TOHE.Roles.Impostor;

internal class Crewpostor : RoleBase
{
    private const int Id = 5800;
    public static bool On;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;

    private static OptionItem CanKillAllies;
    private static OptionItem KnowsAllies;
    private static OptionItem AlliesKnowCrewpostor;
    private static OptionItem LungeKill;
    private static OptionItem KillAfterTask;

    private static Dictionary<byte, int> TasksDone = [];

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Crewpostor);
        CanKillAllies = BooleanOptionItem.Create(Id + 2, "CanKillAllies", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Crewpostor]);
        KnowsAllies = BooleanOptionItem.Create(Id + 3, "CrewpostorKnowsAllies", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Crewpostor]);
        AlliesKnowCrewpostor = BooleanOptionItem.Create(Id + 4, "AlliesKnowCrewpostor", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Crewpostor]);
        LungeKill = BooleanOptionItem.Create(Id + 5, "CrewpostorLungeKill", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Crewpostor]);
        KillAfterTask = IntegerOptionItem.Create(Id + 6, "CrewpostorKillAfterTask", new(1, 50, 1), 1, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Crewpostor]);
        CrewpostorTasks = OverrideTasksData.Create(Id + 7, TabGroup.ImpostorRoles, CustomRoles.Crewpostor);
    }

    public override void Init()
    {
        TasksDone = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        TasksDone[playerId] = 0;
        On = true;
    }

    private static void SendRPC(byte cpID, int tasksDone)
    {
        if (PlayerControl.LocalPlayer.PlayerId == cpID)
        {
            if (TasksDone.ContainsKey(cpID))
                TasksDone[cpID] = tasksDone;
            else TasksDone[cpID] = 0;
        }
        else
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCrewpostorTasksDone, SendOption.Reliable, -1);
            writer.Write(cpID);
            writer.WritePacked(tasksDone);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int tasksDone = reader.ReadInt32();
        if (TasksDone.ContainsKey(PlayerId))
            TasksDone[PlayerId] = tasksDone;
        else
            TasksDone.Add(PlayerId, 0);
    }

    public override bool CanUseKillButton(PlayerControl pc) => false;

    public static bool KnowRole(PlayerControl seer, PlayerControl target)
    {
        if (AlliesKnowCrewpostor.GetBool() && seer.Is(CustomRoleTypes.Impostor) && target.Is(CustomRoles.Crewpostor)
            ||
            KnowsAllies.GetBool() && seer.Is(CustomRoles.Crewpostor) && target.Is(CustomRoleTypes.Impostor))
            return true;

        return false;
    }

    public override void OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (TasksDone.ContainsKey(player.PlayerId))
            TasksDone[player.PlayerId]++;
        else
            TasksDone[player.PlayerId] = 0;

        SendRPC(player.PlayerId, TasksDone[player.PlayerId]);
        List<PlayerControl> list = Main.AllAlivePlayerControls.Where(x => x.PlayerId != player.PlayerId && (CanKillAllies.GetBool() || !x.GetCustomRole().IsImpostorTeam())).ToList();

        if (list.Count <= 0)
        {
            Logger.Info($"No target to kill", "Crewpostor");
        }
        else if (TasksDone[player.PlayerId] % KillAfterTask.GetInt() != 0 && TasksDone[player.PlayerId] != 0)
        {
            Logger.Info($"Crewpostor task done but kill skipped, tasks completed {TasksDone[player.PlayerId]}, but it kills after {KillAfterTask.GetInt()} tasks", "Crewpostor");
        }
        else
        {
            list = [.. list.OrderBy(x => Vector2.Distance(player.transform.position, x.transform.position))];
            var target = list[0];

            if (!target.Is(CustomRoles.Pestilence))
            {
                if (!LungeKill.GetBool())
                {
                    target.SetRealKiller(player);
                    target.RpcCheckAndMurder(target);
                    player.RpcGuardAndKill();
                    Logger.Info("No lunge mode kill", "Crewpostor");
                }
                else
                {
                    target.SetRealKiller(player);
                    player.RpcMurderPlayerV3(target);
                    player.RpcGuardAndKill();
                    Logger.Info("lunge mode kill", "Crewpostor");
                }
                Logger.Info($"Crewpostor completed task to kill：{player.GetNameWithRole().RemoveHtmlTags()} => {target.GetNameWithRole().RemoveHtmlTags()}", "Crewpostor");
            }
            else
            {
                player.SetRealKiller(target);
                target.RpcMurderPlayerV3(player);
                player.RpcGuardAndKill();
                Logger.Info($"Crewpostor tried to kill pestilence (reflected back)：{target.GetNameWithRole().RemoveHtmlTags()} => {player.GetNameWithRole().RemoveHtmlTags()}", "Pestilence Reflect");
            }
        }
    }
}
