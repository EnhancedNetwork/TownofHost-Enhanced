using AmongUs.GameOptions;
using TOHE.Modules;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Catalyst : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Catalyst;
    private const int Id = 32300;
    public override bool IsExperimental => true;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem CatalyzeCooldown;
    private static OptionItem CatalyzeMax;
    private static OptionItem CatalyzeCDReduction;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Catalyst);
        CatalyzeCooldown = FloatOptionItem.Create(Id + 10, "CatalyzeCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Catalyst])
            .SetValueFormat(OptionFormat.Seconds);
        CatalyzeCDReduction = FloatOptionItem.Create(Id + 11, "CatalyzeCDReduction", new(0f, 90f, 5f), 40f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Catalyst])
            .SetValueFormat(OptionFormat.Percent);
        CatalyzeMax = IntegerOptionItem.Create(Id + 12, "CatalyzeMax", new(1, 30, 1), 3, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Catalyst])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(CatalyzeMax.GetInt());
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CatalyzeCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl player) => player.GetAbilityUseLimit() >= 1;
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.GetAbilityUseLimit() < 1) return false;
        if (killer == null || target == null) return false;

        if (target.PlayerId != _Player.PlayerId)
        {
            if (target.CanAbilityLimitBeManip())
            {
                killer.RpcRemoveAbilityUse();
                target.RpcIncreaseAbilityUseLimitBy(1);
                var targetCooldown = Main.AllPlayerKillCooldown[target.PlayerId] * (1 - CatalyzeCDReduction.GetFloat()/100);
                target.SetKillCooldownV3(targetCooldown);

                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Catalyst), GetString("CatalystCatalyzePlayer")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Catalyst), GetString("PlayerCatalyzed")));
                killer.SetKillCooldown();
            }
            else
            {
                // Target is immune to catalyze
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Catalyst), GetString("CatalystInvalidTarget")));
            }
        }

        return false;
    }

    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.KillButton.OverrideText(GetString("CatalystCatalyzeText"));
    }
    // public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Catalyze");
}
