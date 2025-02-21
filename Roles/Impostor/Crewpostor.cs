using AmongUs.GameOptions;
using Hazel;
using UnityEngine;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Double;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

internal class Crewpostor : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Crewpostor;
    private const int Id = 5800;
    private static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();

    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.Madmate;
    //==================================================================\\

    public static OptionItem CPAndAlliesKnowEachOther;
    private static OptionItem LungeKill;
    public static OptionItem KillAfterTask;
    public static OptionItem KillsPerRound;

    private static Dictionary<byte, int> TasksDone = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Crewpostor);
        CPAndAlliesKnowEachOther = BooleanOptionItem.Create(Id + 4, "CPAndAlliesKnowEachOther", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Crewpostor]);
        LungeKill = BooleanOptionItem.Create(Id + 5, "CrewpostorLungeKill", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Crewpostor]);
        KillAfterTask = IntegerOptionItem.Create(Id + 6, "CrewpostorKillAfterTask", new(1, 50, 1), 1, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Crewpostor]);
        KillsPerRound = IntegerOptionItem.Create(Id + 7, "CrewpostorKillsPerRound", new(1, 15, 1), 3, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Crewpostor]); 
    }

    public override void Init()
    {
        TasksDone = [];
        PlayerIds.Clear();
    }
    public override void Add(byte playerId)
    {
        TasksDone[playerId] = 0;
        PlayerIds.Add(playerId);
    }
    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute)
    {
        if (ForRecompute & !player.IsDead)
            return false;
        if (player.IsDead)
            return false;

        return true;
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

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        opt.SetVision(true);
        AURoleOptions.EngineerCooldown = 0f;
        AURoleOptions.EngineerInVentMaxTime = 0f;
    }

    public override bool CanUseKillButton(PlayerControl pc) => false;

    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        int TasksNeeded = LastImpostor.currentId == player.PlayerId ? 1 : KillAfterTask.GetInt();
        if (!player.IsAlive()) return true;

        if (TasksDone.ContainsKey(player.PlayerId))
            TasksDone[player.PlayerId]++;
        else
            TasksDone[player.PlayerId] = 0;

        SendRPC(player.PlayerId, TasksDone[player.PlayerId]);
        List<PlayerControl> list = [];
        foreach (var cptargets in Main.AllAlivePlayerControls.Where(x => x != player))
        {
            if ((cptargets.GetCustomRole() is CustomRoles.NiceMini or CustomRoles.EvilMini && Mini.Age < 18)
              || (CPAndAlliesKnowEachOther.GetBool() && (cptargets.CheckImpTeamCanSeeTeammates() || (cptargets.Is(CustomRoles.Madmate) && !Madmate.ImpCanKillMadmate.GetBool())))
              || (cptargets.Is(CustomRoles.Sheriff) && player.Is(CustomRoles.Narc))
              || cptargets.Is(CustomRoles.Solsticer))
              continue;
            list.Add(cptargets);
        }

        if (!list.Any())
        {
            Logger.Info($"No target to kill", "Crewpostor");
        }
        else if (TasksDone[player.PlayerId] % TasksNeeded != 0 && TasksDone[player.PlayerId] != 0)
        {
            Logger.Info($"Crewpostor task done but kill skipped, tasks completed {TasksDone[player.PlayerId]}, but it kills after {TasksNeeded} tasks", "Crewpostor");
        }
        else
        {
            list = [.. list.OrderBy(x => Utils.GetDistance(player.transform.position, x.transform.position))];
            var target = list[0];

            if (!target.IsTransformedNeutralApocalypse())
            {
                if (!LungeKill.GetBool())
                {
                    target.RpcCheckAndMurder(target);
                    target.SetRealKiller(player);
                    player.RpcGuardAndKill();
                    Logger.Info("No lunge mode kill", "Crewpostor");
                }
                else
                {
                    player.RpcMurderPlayer(target);
                    target.SetRealKiller(player);
                    player.RpcGuardAndKill();
                    Logger.Info("lunge mode kill", "Crewpostor");
                }
                Logger.Info($"Crewpostor completed task to kill：{player.GetNameWithRole().RemoveHtmlTags()} => {target.GetNameWithRole().RemoveHtmlTags()}", "Crewpostor");
            }
            else if (target.Is(CustomRoles.Pestilence))
            {
                target.RpcMurderPlayer(player);
                target.SetRealKiller(player);
                player.RpcGuardAndKill();
                Logger.Info($"Crewpostor tried to kill pestilence (reflected back)：{target.GetNameWithRole().RemoveHtmlTags()} => {player.GetNameWithRole().RemoveHtmlTags()}", "Pestilence Reflect");
            }
            else
            {
                player.RpcGuardAndKill();
                Logger.Info($"Crewpostor tried to kill Apocalypse Member：{target.GetNameWithRole().RemoveHtmlTags()} => {player.GetNameWithRole().RemoveHtmlTags()}", "Apocalypse Immune");
            }
        }

        return true;
    }
    public override void AfterMeetingTasks()
    {
        if (!_Player.IsAlive()) return;
        var player = _Player;
        TaskState taskState = player.GetPlayerTaskState();
        player.Data.RpcSetTasks(new Il2CppStructArray<byte>(0));
        taskState.CompletedTasksCount = 0;
        taskState.AllTasksCount = player.Data.Tasks.Count;
        TasksDone[player.PlayerId] = 0;
    }
}
