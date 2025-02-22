using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Core;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Taskinator : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Taskinator;
    private const int Id = 13700;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Taskinator);
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\

    private static OptionItem TaskMarkPerRoundOpt;

    private readonly HashSet<int> TaskIndex = [];

    private static int maxTasksMarkedPerRound = new();

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Taskinator);
        TaskMarkPerRoundOpt = IntegerOptionItem.Create(Id + 10, "TasksMarkPerRound", new(1, 14, 1), 3, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Taskinator])
            .SetValueFormat(OptionFormat.Votes);
        Options.OverrideTasksData.Create(Id + 11, TabGroup.NeutralRoles, CustomRoles.Taskinator);
    }

    public override void Init()
    {
        TaskIndex.Clear();
        maxTasksMarkedPerRound = TaskMarkPerRoundOpt.GetInt();
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(0);
    }
    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute) => !ForRecompute;

    public override void AfterMeetingTasks()
    {
        if (_Player == null) return;
        var playerId = _Player.PlayerId;

        TaskIndex.Clear();
        playerId.SetAbilityUseLimit(0);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = 1f;
        AURoleOptions.EngineerInVentMaxTime = 0f;
    }
    public override void OnOthersTaskComplete(PlayerControl player, PlayerTask task, bool playerIsOverridden, PlayerControl realPlayer)
    {
        if (player == null || _Player == null) return;
        if (!player.IsAlive()) return;

        var taskinator = _Player;
        byte playerId = player.PlayerId;

        if (player.Is(CustomRoles.Taskinator))
        {
            var abilityUseLimit = playerId.GetAbilityUseLimit();
            if (abilityUseLimit >= maxTasksMarkedPerRound)
            {
                Logger.Info($"Max task per round ({abilityUseLimit}) reached for {player.GetNameWithRole()}", "Taskinator");
                return;
            }
            TaskIndex.Add(task.Index);
            player.RpcIncreaseAbilityUseLimitBy(1);
            player.Notify(GetString("TaskinatorBombPlanted"));
        }
        else if (TaskIndex.Contains(task.Index) && taskinator.RpcCheckAndMurder(player, true))
        {
            TaskIndex.Remove(task.Index);

            player.SetDeathReason(PlayerState.DeathReason.Bombed);
            player.RpcMurderPlayer(player);
            player.SetRealKiller(taskinator);

            Logger.Info($"{player.GetAllRoleName()} died because of {taskinator.GetNameWithRole()}", "Taskinator");
        }
    }
}

