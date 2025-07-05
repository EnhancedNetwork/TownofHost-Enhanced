using TOHE.Modules;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.Core;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Cultist : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Cultist;
    private const int Id = 14800;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Cultist);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem CharmCooldown;
    private static OptionItem CharmCooldownIncrese;
    private static OptionItem CharmMax;
    private static OptionItem KnowTargetRole;
    private static OptionItem TargetKnowOtherTarget;
    private static OptionItem CanCharmNeutral;
    private static OptionItem CanCharmCoven;
    public static OptionItem CharmedCountMode;

    [Obfuscation(Exclude = true)]
    private enum CharmedCountModeSelectList
    {
        CountMode_None,
        Cultist_CharmedCountMode_Cultist,
        CountMode_Original
    }

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Cultist, 1, zeroOne: false);
        CharmCooldown = FloatOptionItem.Create(Id + 10, "CultistCharmCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cultist])
            .SetValueFormat(OptionFormat.Seconds);
        CharmCooldownIncrese = FloatOptionItem.Create(Id + 11, "CultistCharmCooldownIncrese", new(0f, 180f, 2.5f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cultist])
            .SetValueFormat(OptionFormat.Seconds);
        CharmMax = IntegerOptionItem.Create(Id + 12, "CultistCharmMax", new(1, 15, 1), 15, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cultist])
            .SetValueFormat(OptionFormat.Times);
        KnowTargetRole = BooleanOptionItem.Create(Id + 13, "CultistKnowTargetRole", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cultist]);
        TargetKnowOtherTarget = BooleanOptionItem.Create(Id + 14, "CultistTargetKnowOtherTarget", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cultist]);
        CharmedCountMode = StringOptionItem.Create(Id + 17, "Cultist_CharmedCountMode", EnumHelper.GetAllNames<CharmedCountModeSelectList>(), 1, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cultist]);
        CanCharmNeutral = BooleanOptionItem.Create(Id + 18, "CultistCanCharmNeutral", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cultist]);
        CanCharmCoven = BooleanOptionItem.Create(Id + 19, "CultistCanCharmCoven", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cultist]);
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(CharmMax.GetInt());
    }
    public override void SetKillCooldown(byte id)
    {
        var currentAbilityUse = id.GetAbilityUseLimit();
        Main.AllPlayerKillCooldown[id] = currentAbilityUse >= 1 ? CharmCooldown.GetFloat() + (CharmMax.GetInt() - currentAbilityUse) * CharmCooldownIncrese.GetFloat() : 300f;
    }
    public override bool CanUseKillButton(PlayerControl player) => player.GetAbilityUseLimit() >= 1;
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (Mini.Age < 18 && (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cultist), GetString("CantRecruit")));
            return false;
        }

        else if (target.CanBeRecruitedBy(killer))
        {
            var addon = killer.GetBetrayalAddon(true);
            killer.RpcRemoveAbilityUse();
            target.RpcSetCustomRole(addon);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(addon), GetString("CultistCharmedPlayer")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(addon), GetString("CharmedByCultist")));

            Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
            Utils.NotifyRoles(SpecifySeer: target, SpecifyTarget: killer, ForceLoop: true);

            if (addon is CustomRoles.Admired)
            {
                Admirer.AdmiredList[killer.PlayerId].Add(target.PlayerId);
                Admirer.SendRPC(killer.PlayerId, target.PlayerId);
            }

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);

            Logger.Info(target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + addon.ToString(), "Assign " + addon.ToString());
            return false;
        }
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cultist), GetString("CultistInvalidTarget")));
        return false;
    }
    public static bool TargetKnowOtherTargets => TargetKnowOtherTarget.GetBool();
    public static bool KnowRole(PlayerControl player, PlayerControl target)
    {
        if (player.Is(CustomRoles.Charmed) && target.Is(CustomRoles.Cultist)) return true;

        if (KnowTargetRole.GetBool())
        {
            if (player.Is(CustomRoles.Cultist) && target.Is(CustomRoles.Charmed)) return true;
            if (TargetKnowOtherTarget.GetBool() && player.Is(CustomRoles.Charmed) && target.Is(CustomRoles.Charmed)) return true;
        }
        return false;
    }
    public static bool CanBeCharmed(PlayerControl pc)
    {
        return pc != null && (pc.GetCustomRole().IsCrewmate() || pc.GetCustomRole().IsImpostor() ||
            (CanCharmNeutral.GetBool() && pc.GetCustomRole().IsNeutral()) ||
            (CanCharmCoven.GetBool() && pc.GetCustomRole().IsCoven())) && !pc.Is(CustomRoles.Charmed)
            && !pc.Is(CustomRoles.Admired) && !pc.Is(CustomRoles.Loyal) && !pc.Is(CustomRoles.Infectious)
            && !pc.Is(CustomRoles.Virus) && !pc.Is(CustomRoles.Cultist) && !pc.Is(CustomRoles.Enchanted)
            && !(pc.GetCustomSubRoles().Contains(CustomRoles.Hurried) && !Hurried.CanBeConverted.GetBool()) && !(CovenManager.HasNecronomicon(pc.PlayerId) && pc.Is(CustomRoles.CovenLeader));
    }
    public static bool NameRoleColor(PlayerControl seer, PlayerControl target)
    {
        if (seer.Is(CustomRoles.Charmed) && target.Is(CustomRoles.Cultist)) return true;
        if (seer.Is(CustomRoles.Cultist) && target.Is(CustomRoles.Charmed)) return true;
        if (seer.Is(CustomRoles.Charmed) && target.Is(CustomRoles.Charmed) && TargetKnowOtherTarget.GetBool()) return true;

        return false;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("CultistKillButtonText"));
    }
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Subbus");
}
