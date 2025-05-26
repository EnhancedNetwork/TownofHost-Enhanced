using TOHE.Modules;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class CursedSoul : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.CursedSoul;
    private const int Id = 14000;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralEvil;
    //==================================================================\\

    private static OptionItem CurseCooldown;
    private static OptionItem CurseCooldownIncrese;
    private static OptionItem CurseMax;
    private static OptionItem KnowTargetRoleOpt;
    private static OptionItem CanCurseNeutral;
    private static OptionItem CanCurseCoven;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.CursedSoul, 1, zeroOne: false);
        CurseCooldown = FloatOptionItem.Create(Id + 10, "CursedSoulCurseCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CursedSoul])
            .SetValueFormat(OptionFormat.Seconds);
        CurseCooldownIncrese = FloatOptionItem.Create(Id + 11, "CursedSoulCurseCooldownIncrese", new(0f, 180f, 2.5f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CursedSoul])
            .SetValueFormat(OptionFormat.Seconds);
        CurseMax = IntegerOptionItem.Create(Id + 12, "CursedSoulCurseMax", new(1, 15, 1), 3, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CursedSoul])
            .SetValueFormat(OptionFormat.Times);
        KnowTargetRoleOpt = BooleanOptionItem.Create(Id + 13, "CursedSoulKnowTargetRole", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CursedSoul]);
        CanCurseNeutral = BooleanOptionItem.Create(Id + 16, "CursedSoulCanCurseNeutral", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CursedSoul]);
        CanCurseCoven = BooleanOptionItem.Create(Id + 17, "CursedSoulCanCurseCoven", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CursedSoul]);
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(CurseMax.GetInt());
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = id.GetAbilityUseLimit() >= 1 ? CurseCooldown.GetFloat() + (CurseMax.GetInt() - id.GetAbilityUseLimit()) * CurseCooldownIncrese.GetFloat() : 300f;
    public override bool CanUseKillButton(PlayerControl player) => player.GetAbilityUseLimit() >= 1;

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.GetAbilityUseLimit() < 1) return false;
        if (Mini.Age < 18 && (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cultist), GetString("CantRecruit")));
            return false;
        }
        if (target.CanBeRecruitedBy(killer))
        {
            var addon = killer.GetBetrayalAddon(true);
            killer.RpcRemoveAbilityUse();
            target.RpcSetCustomRole(addon);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(addon), GetString("CursedSoulSoullessPlayer")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(addon), GetString("SoullessByCursedSoul")));

            if (addon is CustomRoles.Admired)
            {
                Admirer.AdmiredList[killer.PlayerId].Add(target.PlayerId);
                Admirer.SendRPC(killer.PlayerId, target.PlayerId); //Sync playerId list
            }

            Utils.NotifyRoles(SpecifySeer: target, SpecifyTarget: killer, ForceLoop: true);
            Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);

            killer.ResetKillCooldown();
            killer.SetKillCooldown();

            if (!DisableShieldAnimations.GetBool())
                killer.RpcGuardAndKill(target);

            Logger.Info($"{target?.Data?.PlayerName} = {target.GetCustomRole()} + {addon}", $"Assign {addon}");
            return false;
        }
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.CursedSoul), GetString("CursedSoulInvalidTarget")));
        return false;
    }
    public override bool KnowRoleTarget(PlayerControl player, PlayerControl target)
        => player.Is(CustomRoles.CursedSoul) && KnowTargetRoleOpt.GetBool() && target.Is(CustomRoles.Soulless);

    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target)
        => KnowRoleTarget(seer, target) ? Main.roleColors[CustomRoles.Soulless] : string.Empty;

    public static bool CanBeSoulless(PlayerControl pc)
    {
        return pc != null && (pc.GetCustomRole().IsCrewmate() || pc.GetCustomRole().IsImpostor() ||
            (CanCurseNeutral.GetBool() && pc.GetCustomRole().IsNeutral()) ||
            (CanCurseCoven.GetBool() && pc.GetCustomRole().IsCoven())) && !pc.Is(CustomRoles.Soulless) && !pc.Is(CustomRoles.Admired) && !pc.Is(CustomRoles.Enchanted) && !pc.Is(CustomRoles.Loyal) && !(CovenManager.HasNecronomicon(pc.PlayerId) && pc.Is(CustomRoles.CovenLeader));
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("CursedSoulKillButtonText"));
    }
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Soul");
}
