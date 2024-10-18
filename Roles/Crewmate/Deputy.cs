using AmongUs.GameOptions;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Deputy : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 7800;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem HandcuffCooldown;
    private static OptionItem HandcuffMax;
    private static OptionItem DeputyHandcuffCDForTarget;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Deputy);
        HandcuffCooldown = FloatOptionItem.Create(Id + 10, "DeputyHandcuffCooldown", new(0f, 180f, 2.5f), 10f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deputy])
            .SetValueFormat(OptionFormat.Seconds);
        DeputyHandcuffCDForTarget = FloatOptionItem.Create(Id + 14, "DeputyHandcuffCDForTarget", new(0f, 180f, 2.5f), 45f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deputy])
            .SetValueFormat(OptionFormat.Seconds);
        HandcuffMax = IntegerOptionItem.Create(Id + 12, "DeputyHandcuffMax", new(1, 30, 1), 15, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deputy])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(HandcuffMax.GetInt());
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = HandcuffCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl player) => player.GetAbilityUseLimit() >= 1;
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.Is(CustomRoles.SerialKiller)) return false;
        if (killer.GetAbilityUseLimit() < 1) return false;

        if (target.PlayerId != killer.PlayerId)
        {
            killer.RpcRemoveAbilityUse();

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Deputy), GetString("DeputyHandcuffedPlayer")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Deputy), GetString("HandcuffedByDeputy")));

            target.SetKillCooldownV3(DeputyHandcuffCDForTarget.GetFloat());
            if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
            if (!DisableShieldAnimations.GetBool()) target.RpcGuardAndKill(target);

            return false;
        }
        
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Deputy), GetString("DeputyInvalidTarget")));
        return false;
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.KillButton.OverrideText(GetString("DeputyHandcuffText"));
    }
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Deputy");
}
