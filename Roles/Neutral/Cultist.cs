﻿using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.Core;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Cultist : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 14800;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Cultist);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralChaos;
    //==================================================================\\

    private static OptionItem CharmCooldown;
    private static OptionItem CharmCooldownIncrese;
    private static OptionItem CharmMax;
    private static OptionItem KnowTargetRole;
    private static OptionItem TargetKnowOtherTarget;
    private static OptionItem CanCharmNeutral;
    public static OptionItem CharmedCountMode;

    private enum CharmedCountModeSelectList
    {
        Cultist_CharmedCountMode_None,
        Cultist_CharmedCountMode_Cultist,
        Cultist_CharmedCountMode_Original
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
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = CharmMax.GetInt();
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = AbilityLimit >= 1 ? CharmCooldown.GetFloat() + (CharmMax.GetInt() - AbilityLimit) * CharmCooldownIncrese.GetFloat() : 300f;
    public override bool CanUseKillButton(PlayerControl player) => AbilityLimit >= 1;
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (AbilityLimit < 1) return false;
        if (Mini.Age < 18 && (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cultist), GetString("CantRecruit")));
            return false;
        }
        else if (CanBeCharmed(target) && Mini.Age == 18 || CanBeCharmed(target) && Mini.Age < 18 && !(target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        {
            AbilityLimit--;
            SendSkillRPC();
            target.RpcSetCustomRole(CustomRoles.Charmed);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cultist), GetString("CultistCharmedPlayer")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cultist), GetString("CharmedByCultist")));
            
            Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
            Utils.NotifyRoles(SpecifySeer: target, SpecifyTarget: killer, ForceLoop: true);

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);

            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Charmed.ToString(), "Assign " + CustomRoles.Charmed.ToString());
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{AbilityLimit}次魅惑机会", "Cultist");
            return false;
        }
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cultist), GetString("CultistInvalidTarget")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{AbilityLimit}次魅惑机会", "Cultist");
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
    public override string GetProgressText(byte playerid, bool cooms) => Utils.ColorString(AbilityLimit >= 1 ? Utils.GetRoleColor(CustomRoles.Cultist).ShadeColor(0.25f) : Color.gray, $"({AbilityLimit})");
    public static bool CanBeCharmed(PlayerControl pc)
    {
        return pc != null && (pc.GetCustomRole().IsCrewmate() || pc.GetCustomRole().IsImpostor() || 
            (CanCharmNeutral.GetBool() && pc.GetCustomRole().IsNeutral())) && !pc.Is(CustomRoles.Charmed) 
            && !pc.Is(CustomRoles.Admired) && !pc.Is(CustomRoles.Loyal) && !pc.Is(CustomRoles.Infectious) 
            && !pc.Is(CustomRoles.Virus) && !pc.Is(CustomRoles.Cultist)
            && !(pc.GetCustomSubRoles().Contains(CustomRoles.Hurried) && !Hurried.CanBeConverted.GetBool());
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