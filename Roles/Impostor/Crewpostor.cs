using AmongUs.GameOptions;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
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

    public static OptionItem CPAndAlliesKnowEachOther;
    public static OptionItem KillAfterTask;
    private static OptionItem LungeKill;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Crewpostor);
        CPAndAlliesKnowEachOther = BooleanOptionItem.Create(Id + 4, "CPAndAlliesKnowEachOther", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Crewpostor]);
        KillAfterTask = IntegerOptionItem.Create(Id + 5, "CrewpostorKillAfterTask", new(2, 10, 1), 3, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Crewpostor]);
        LungeKill = BooleanOptionItem.Create(Id + 6, "CrewpostorLungeKill", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Crewpostor]);
    }

    public override void Init()
    { }
    public override void Add(byte playerId)
    { }
    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute)
    {
        if (ForRecompute & !player.IsDead)
            return false;
        if (player.IsDead)
            return false;

        return true;

    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        opt.SetVision(true);
        AURoleOptions.EngineerCooldown = 0f;
        AURoleOptions.EngineerInVentMaxTime = 0f;
    }

    public override bool CanUseKillButton(PlayerControl pc) => false;

    public static void CrewpostorResetTasks(PlayerControl player)
    {
        TaskState taskState = player.GetPlayerTaskState();
        player.Data.RpcSetTasks(new Il2CppStructArray<byte>(0));
        taskState.CompletedTasksCount = 0;
        taskState.AllTasksCount = player.Data.Tasks.Count;
    }
    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (!player.IsAlive()) return true;

        List<PlayerControl> list = Main.AllAlivePlayerControls.Where
        (x => x.PlayerId != player.PlayerId 
        && x.GetCustomRole() is not CustomRoles.NiceMini and not CustomRoles.EvilMini and not CustomRoles.Solsticer
        && !(CPAndAlliesKnowEachOther.GetBool() && (x.Is(CustomRoles.Madmate) || x.CheckMMCanSeeImp()))).ToList();

        if (!list.Any())
        {
            Logger.Info($"No target to kill", "Crewpostor");
        }
        else if (completedTaskCount < totalTaskCount)
        {
            Logger.Info($"Crewpostor task done but kill skipped, tasks completed {completedTaskCount}, but it kills after {KillAfterTask.GetInt()} tasks", "Crewpostor");
        }
        else
        {
            CrewpostorResetTasks(player);
            list = [.. list.OrderBy(x => Utils.GetDistance(player.transform.position, x.transform.position))];
            var target = list[0];

            if (!target.IsTransformedNeutralApocalypse() && !(player.Is(CustomRoles.Narc) && target.Is(CustomRoles.Sheriff)))
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
                player.SetRealKiller(target);
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
