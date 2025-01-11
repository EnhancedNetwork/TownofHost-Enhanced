using AmongUs.GameOptions;
using Hazel;
using TOHE.Roles.AddOns.Impostor;
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
    private static OptionItem KillAfterTask;

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
        OverrideTasksData.Create(Id + 7, TabGroup.ImpostorRoles, CustomRoles.Crewpostor);
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
        AURoleOptions.EngineerCooldown = 0f;
        AURoleOptions.EngineerInVentMaxTime = 0f;
    }

    public override bool CanUseKillButton(PlayerControl pc) => false;

    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (!player.IsAlive()) return true;

        if (TasksDone.ContainsKey(player.PlayerId))
            TasksDone[player.PlayerId]++;
        else
            TasksDone[player.PlayerId] = 0;

        SendRPC(player.PlayerId, TasksDone[player.PlayerId]);
        List<PlayerControl> list = Main.AllAlivePlayerControls
        .Where(x => x.PlayerId != player.PlayerId 
        && !(x.GetCustomRole() is CustomRoles.NiceMini or CustomRoles.EvilMini or CustomRoles.Solsticer) 
        && (CrewpostorAndAlliesKnowEachOther.GetBool() || !(x.CheckMMCanSeeImp() || (x.Is(CustomRoles.Madmate) && !Madmate.ImpCanKillMadmate.GetBool()))) 
        && !(player.Is(CustomRoles.Narc) && x.Is(CustomRoles.Sheriff))).ToList();

        if (!list.Any())
        {
            Logger.Info($"No target to kill", "Crewpostor");
        }
        else if (TasksDone[player.PlayerId] % KillAfterTask.GetInt() != 0 && TasksDone[player.PlayerId] != 0)
        {
            Logger.Info($"Crewpostor task done but kill skipped, tasks completed {TasksDone[player.PlayerId]}, but it kills after {KillAfterTask.GetInt()} tasks", "Crewpostor");
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
}
