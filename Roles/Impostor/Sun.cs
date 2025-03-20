using TOHE.Modules;
using static TOHE.Options;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.Impostor;

internal class Sun : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Sun;
    private const int Id = 34600;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem AbilityUses;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Sun);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Sun])
            .SetValueFormat(OptionFormat.Seconds);
        AbilityUses = IntegerOptionItem.Create(Id + 11, "AbilityUses346", new(1, 5, 1), 3, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Sun])
            .SetValueFormat(OptionFormat.Times);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;

    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(AbilityUses.GetInt());
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        killer.RpcIncreaseAbilityUseLimitBy(1);
        return true;
    }

    public override void AfterMeetingTasks()
    {
        var sun = _Player;
        if (sun.GetAbilityUseLimit() <= 0) return;
        sun.RpcRemoveAbilityUse();
        if (sun.IsAlive())
        {
            List<PlayerControl> targetList = [];
            var rand = IRandom.Instance;
            foreach (var target in Main.AllAlivePlayerControls)
            {
                if (target.GetCustomRole().IsImpostor()) continue;
                targetList.Add(target);
            }
            if (targetList.Any())
            {
                var selectedTarget = targetList.RandomElement();
                Main.PlayerStates[selectedTarget.PlayerId].deathReason = PlayerState.DeathReason.Torched;
                selectedTarget.RpcExileV2();
                Main.PlayerStates[selectedTarget.PlayerId].SetDead();
                selectedTarget.Data.IsDead = true;
                selectedTarget.SetRealKiller(sun);
            }
        }
    }
}
