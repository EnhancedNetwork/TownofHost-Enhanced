using AmongUs.GameOptions;
using TOHE.Roles.Core;
using UnityEngine;

namespace TOHE;

public static class SpeedRun
{
    private const int Id = 67_225_001;

    public static OptionItem SpeedRun_NumCommonTasks;
    public static OptionItem SpeedRun_NumShortTasks;
    public static OptionItem SpeedRun_NumLongTasks;

    public static OptionItem SpeedRun_RunnerNormalSpeed;
    public static OptionItem SpeedRun_RunnerKcd;
    public static OptionItem SpeedRun_RunnerKcdPerDeadPlayer;
    public static OptionItem SpeedRun_RunnerSpeedAfterFinishTasks;
    public static OptionItem SpeedRun_AllowCloseDoor;

    public static OptionItem SpeedRun_ArrowPlayers;
    public static OptionItem SpeedRun_ArrowPlayersPlayerLiving; // Only * players left show arrows

    public static OptionItem SpeedRun_SpeedBoostAfterTask;
    public static OptionItem SpeedRun_SpeedBoostSpeed;
    public static OptionItem SpeedRun_SpeedBoostDuration;

    public static OptionItem SpeedRun_ProtectAfterTask;
    public static OptionItem SpeedRun_ProtectDuration;
    public static OptionItem SpeedRun_ProtectOnlyOnce;
    public static OptionItem SpeedRun_ProtectKcd;


    public static void SetupCustomOption()
    {
        SpeedRun_NumCommonTasks = IntegerOptionItem.Create(Id + 1, "SpeedRun_NumCommonTasks", new(1, 10, 1), 1, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun);
        SpeedRun_NumShortTasks = IntegerOptionItem.Create(Id + 2, "SpeedRun_NumShortTasks", new(1, 15, 1), 1, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun);
        SpeedRun_NumLongTasks = IntegerOptionItem.Create(Id + 3, "SpeedRun_NumLongTasks", new(1, 15, 1), 1, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun);

        SpeedRun_RunnerNormalSpeed = FloatOptionItem.Create(Id + 4, "SpeedRun_RunnerNormalSpeed", new(0.5f, 60f, 0.5f), 15f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetValueFormat(OptionFormat.Multiplier);
        SpeedRun_RunnerKcd = FloatOptionItem.Create(Id + 5, "SpeedRun_RunnerKcd", new(0.5f, 60f, 0.5f), 15f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetValueFormat(OptionFormat.Seconds);
        SpeedRun_RunnerKcdPerDeadPlayer = FloatOptionItem.Create(Id + 6, "SpeedRun_RunnerKcdPerDeadPlayer", new(0f, 60f, 0.1f), 0f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetValueFormat(OptionFormat.Seconds);
        SpeedRun_RunnerSpeedAfterFinishTasks = FloatOptionItem.Create(Id + 7, "SpeedRun_RunnerSpeedAfterFinishTasks", new(0.25f, 5f, 0.25f), 2.5f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetValueFormat(OptionFormat.Multiplier);
        SpeedRun_AllowCloseDoor = BooleanOptionItem.Create(Id + 8, "SpeedRun_AllowCloseDoor", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun);

        SpeedRun_ArrowPlayers = BooleanOptionItem.Create(Id + 9, "SpeedRun_ArrowPlayers", true, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun);
        SpeedRun_ArrowPlayersPlayerLiving = IntegerOptionItem.Create(Id + 10, "SpeedRun_ArrowPlayersPlayerLiving", new(1, 127, 1), 1, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun);

        SpeedRun_SpeedBoostAfterTask = BooleanOptionItem.Create(Id + 11, "SpeedRun_SpeedBoostAfterTask", true, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun);
        SpeedRun_SpeedBoostSpeed = FloatOptionItem.Create(Id + 12, "SpeedRun_SpeedBoostSpeed", new(0.25f, 5f, 0.25f), 2.5f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetValueFormat(OptionFormat.Multiplier);
        SpeedRun_SpeedBoostDuration = FloatOptionItem.Create(Id + 13, "SpeedRun_SpeedBoostDuration", new(0.5f, 60f, 0.5f), 2.5f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetValueFormat(OptionFormat.Seconds);

        SpeedRun_ProtectAfterTask = BooleanOptionItem.Create(Id + 14, "SpeedRun_ProtectAfterTask", true, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun);
        SpeedRun_ProtectDuration = FloatOptionItem.Create(Id + 15, "SpeedRun_ProtectDuration", new(0.5f, 60f, 0.5f), 2.5f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetValueFormat(OptionFormat.Seconds);
        SpeedRun_ProtectOnlyOnce = BooleanOptionItem.Create(Id + 16, "SpeedRun_ProtectOnlyOnce", true, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun);
        SpeedRun_ProtectKcd = FloatOptionItem.Create(Id + 17, "SpeedRun_ProtectKcd", new(0.5f, 60f, 0.5f), 5f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetValueFormat(OptionFormat.Seconds);
    }
}

public class Runner : RoleBase
{
    public override CustomRoles Role => CustomRoles.Runner;
    public override CustomRoles ThisRoleBase => BasisChanged ? CustomRoles.Impostor : CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.None;
    public override bool IsDesyncRole => true;

    public (bool, float) ProtectState = (false, 0f);
    public (bool, float) SpeedBoostState = (false, 0f);
    private (int, int) LastTaskCount = (0, 0);
    private bool BasisChanged = false;

    public override void Add(byte playerId)
    {
        ProtectState = (false, 0f);
        SpeedBoostState = (false, 0f);
        LastTaskCount = (0, 0);
        BasisChanged = false;
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        opt.SetVision(true);

        float speed;

        if (BasisChanged)
        {
            speed = SpeedRun.SpeedRun_RunnerSpeedAfterFinishTasks.GetFloat();
        }
        else
        {
            if (SpeedBoostState.Item1)
            {
                speed = SpeedRun.SpeedRun_SpeedBoostSpeed.GetFloat();
            }
            else
            {
                speed = SpeedRun.SpeedRun_RunnerNormalSpeed.GetFloat();
            }
        }

        AURoleOptions.PlayerSpeedMod = speed;
    }

    public override void SetKillCooldown(byte id)
    {
        var deadnum = Main.AllPlayerControls.Count(x => x.Is(CustomRoles.Runner)) - Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.Runner));
        Main.AllPlayerKillCooldown[id] = SpeedRun.SpeedRun_RunnerKcd.GetFloat() - SpeedRun.SpeedRun_RunnerKcdPerDeadPlayer.GetFloat() * deadnum;
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (ProtectState.Item1)
        {
            ProtectState.Item2 -= Time.fixedDeltaTime;

            if (ProtectState.Item2 <= 0)
            {
                ProtectState = (false, 0f);
            }
        }

        if (SpeedBoostState.Item1)
        {
            SpeedBoostState.Item2 -= Time.fixedDeltaTime;
            if (SpeedBoostState.Item2 <= 0)
            {
                SpeedBoostState = (false, 0f);
                player.MarkDirtySettings();
            }
        }
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!BasisChanged)
        {
            return false;
        }

        var targetRoleClass = target.GetRoleClass();

        if (targetRoleClass.Role != CustomRoles.Runner)
        {
            return false;
        }

        Runner targetRole = targetRoleClass as Runner;

        if (targetRole.ProtectState.Item1)
        {
            if (SpeedRun.SpeedRun_ProtectOnlyOnce.GetBool())
            {
                targetRole.ProtectState = (false, 0f);
            }

            killer.SetKillCooldown(SpeedRun.SpeedRun_ProtectKcd.GetFloat(), target, true);
            killer.ResetKillCooldown();

            target.RpcGuardAndKill(target);
            return false;
        }

        return true;
    }

    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (!player.IsAlive()) return true;

        LastTaskCount = (completedTaskCount, totalTaskCount);

        if (SpeedRun.SpeedRun_SpeedBoostAfterTask.GetBool() && !BasisChanged)
        {
            SpeedBoostState = (true, SpeedRun.SpeedRun_SpeedBoostDuration.GetFloat());
            player.MarkDirtySettings();
        }

        if (SpeedRun.SpeedRun_ProtectAfterTask.GetBool() && !BasisChanged)
        {
            ProtectState = (true, SpeedRun.SpeedRun_ProtectDuration.GetFloat());
            player.RpcSpecificProtectPlayer(player, player.CurrentOutfit.ColorId);
        }

        if (completedTaskCount >= totalTaskCount && !BasisChanged)
        {
            BasisChanged = true;
            player.RpcChangeRoleBasis(CustomRoles.Runner);
            player.MarkDirtySettings();
        }

        return true;
    }
}
