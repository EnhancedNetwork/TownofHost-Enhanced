using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Double;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor;

internal class Detonator : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Detonator;
    private const int Id = 34300;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.KillButton.buttonLabelText.text = GetString("DetonatorButtonText");
    }

    private static OptionItem DetonatorKillCooldown;
    private static OptionItem AbilityUses;
    private static OptionItem FreezeTime;
    private static OptionItem ExplosionRadius;

    public static PlayerControl Frozen = null;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Detonator);
        DetonatorKillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(1f, 180f, 1f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Detonator])
            .SetValueFormat(OptionFormat.Seconds);
        AbilityUses = IntegerOptionItem.Create(Id + 11, "AbilityUses343", new(1, 8, 1), 3, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Detonator])
            .SetValueFormat(OptionFormat.Times);
        FreezeTime = FloatOptionItem.Create(Id + 12, "FreezeTime343", new(1f, 25f, 1.5f), 10f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Detonator])
            .SetValueFormat(OptionFormat.Seconds);
        ExplosionRadius = FloatOptionItem.Create(Id + 10, "ExplosionRadius343", new(0.5f, 1.5f, 0.1f), 1.3f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Detonator])
            .SetValueFormat(OptionFormat.Multiplier);
    }

    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(AbilityUses.GetInt());

        // Double Trigger
        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();
    }
    public override void SetKillCooldown(byte id) => DetonatorKillCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = 1f;
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.CheckDoubleTrigger(target, () => { }))
        {
            return true;
        }
        if (killer.GetAbilityUseLimit() < 1)
        {
            return true;
        }
        if (Frozen != null)
        {
            return true;
        }
        killer.RpcRemoveAbilityUse();
        var tmpSpeed = Main.AllPlayerSpeed[target.PlayerId];
        Main.AllPlayerSpeed[target.PlayerId] = Main.MinSpeed;
        ReportDeadBodyPatch.CanReport[target.PlayerId] = false;
        target.MarkDirtySettings();
        killer.ResetKillCooldown();
        killer.RpcGuardAndKill(killer);
        Frozen = target;
        _ = new LateTask(() =>
        {
            Main.AllPlayerSpeed[target.PlayerId] = Main.AllPlayerSpeed[target.PlayerId] - Main.MinSpeed + tmpSpeed;
            ReportDeadBodyPatch.CanReport[target.PlayerId] = true;
            target.MarkDirtySettings();
            RPC.PlaySoundRPC(target.PlayerId, Sounds.TaskComplete);
            Frozen = null;
        }, FreezeTime.GetFloat(), "Detonator Freeze");
        return false;
    }
    public override void UnShapeShiftButton(PlayerControl shapeshifter)
    {
        if (Frozen == null) return;
        _ = new LateTask(() =>
        {
            foreach (var target in Main.AllAlivePlayerControls)
            {
                if (target.IsTransformedNeutralApocalypse()) continue;
                else if ((target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)) && Mini.Age < 18) continue;

                if (GetDistance(Frozen.transform.position, target.transform.position) <= ExplosionRadius.GetFloat())
                {
                    target.SetDeathReason(PlayerState.DeathReason.Bombed);
                    Frozen.RpcMurderPlayer(target);
                    target.SetRealKiller(Frozen);
                }
            }
        }, 0.1f, "Detonator Explosion Bug Fix");
    }
}
