using AmongUs.GameOptions;
using Hazel;
using UnityEngine;
using TOHE.Roles.AddOns.Impostor;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

internal class Crewpostor : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Crewpostor;
    private const int Id = 5800;
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.Madmate;
    //==================================================================\\

    private static OptionItem CanKillAllies;
    private static OptionItem KnowsAllies;
    private static OptionItem AlliesKnowCrewpostor;
    private static OptionItem LungeKill;
    public static OptionItem KillAfterTask;
    public static OptionItem KillsPerRound;

    private static Dictionary<byte, int> TasksDone = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Crewpostor);
        CanKillAllies = BooleanOptionItem.Create(Id + 2, GeneralOption.CanKillImpostors, true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Crewpostor]);
        KnowsAllies = BooleanOptionItem.Create(Id + 3, "CrewpostorKnowsAllies", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Crewpostor]);
        AlliesKnowCrewpostor = BooleanOptionItem.Create(Id + 4, "AlliesKnowCrewpostor", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Crewpostor]);
        LungeKill = BooleanOptionItem.Create(Id + 5, "CrewpostorLungeKill", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Crewpostor]);
        KillAfterTask = IntegerOptionItem.Create(Id + 6, "CrewpostorKillAfterTask", new(2, 5, 1), 1, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Crewpostor]);
        KillsPerRound = IntegerOptionItem.Create(Id + 7, "CrewpostorKillsPerRound", new(1, 15, 1), 1, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Crewpostor]);
    }

    public override void Init()
    {
        TasksDone.Clear();

    }
    public override void Add(byte playerId)
    {
        TasksDone[playerId] = 0;

    }
    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute) => !ForRecompute && !player.IsDead;
    
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

    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target) => KnowRoleTarget(seer, target) ? Utils.GetRoleColorCode(CustomRoles.Crewpostor) : string.Empty;
    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) => KnowRoleTarget(seer, target);
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target)
        => (AlliesKnowCrewpostor.GetBool() && seer.Is(Custom_Team.Impostor) && target.Is(CustomRoles.Crewpostor) && !Main.PlayerStates[seer.PlayerId].IsNecromancer && !Main.PlayerStates[target.PlayerId].IsNecromancer)
            || (KnowsAllies.GetBool() && seer.Is(CustomRoles.Crewpostor) && target.Is(Custom_Team.Impostor) && !Main.PlayerStates[seer.PlayerId].IsNecromancer && !Main.PlayerStates[target.PlayerId].IsNecromancer);

    public override string GetProgressText(byte playerId, bool comms)
    {
        var color = comms ? Color.gray : Color.red;
        string TaskCompleted = comms ? "?" : $"{TasksDone[playerId]}";
        string DisplayTaskProgress = LastImpostor.currentId == playerId ? 
                                string.Empty : Utils.ColorString(color, $" ({TaskCompleted}/{KillAfterTask.GetInt()})");

        int NumKillsLeft = KillsPerRound.GetInt() - Main.MurderedThisRound.Count(ded => ded.GetRealKillerById() == playerId.GetPlayer());
        string DisplayKillsLeft = Utils.ColorString(Color.red, LastImpostor.currentId == playerId ? $"({Main.AllAlivePlayerControls.Length})" : $"({NumKillsLeft})");

        return DisplayTaskProgress + " - " + DisplayKillsLeft;
    }

    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (!player.IsAlive()) return true;
        int TaskNeededToKill = LastImpostor.currentId == player.PlayerId ? 1 : KillAfterTask.GetInt();

        if (TasksDone.ContainsKey(player.PlayerId))
            TasksDone[player.PlayerId]++;
        else
            TasksDone[player.PlayerId] = 0;

        SendRPC(player.PlayerId, TasksDone[player.PlayerId]);
        List<PlayerControl> list = Main.AllAlivePlayerControls.Where(x => x.PlayerId != player.PlayerId && !(x.GetCustomRole() is CustomRoles.NiceMini or CustomRoles.EvilMini or CustomRoles.Solsticer) && (CanKillAllies.GetBool() || !x.GetCustomRole().IsImpostorTeam())).ToList();

        if (!list.Any())
        {
            Logger.Info($"No target to kill", "Crewpostor");
        }
        else if (TasksDone[player.PlayerId] % TaskNeededToKill != 0 && TasksDone[player.PlayerId] != 0)
        {
            Logger.Info($"Crewpostor task done but kill skipped, tasks completed {TasksDone[player.PlayerId]}, but it kills after {TaskNeededToKill} tasks", "Crewpostor");
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
        var cp = _Player;
        cp.RpcResetTasks();
        TasksDone[cp.PlayerId] = 0;
    }
}
