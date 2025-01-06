using AmongUs.GameOptions;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Jackal : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 16700;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Jailer);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem CanVent;
    private static OptionItem CanUsesSabotage;
    public static OptionItem CanWinBySabotageWhenNoImpAlive;
    public static OptionItem HasImpostorVision;
    private static OptionItem OptionResetKillCooldownWhenSbGetKilled;
    private static OptionItem ResetKillCooldownWhenSbGetKilled;
    private static OptionItem ResetKillCooldownOn;
    private static OptionItem JackalCanKillSidekick;
    private static OptionItem CanRecruitSidekick;
    private static OptionItem SidekickRecruitLimitOpt;
    public static OptionItem SidekickCountMode;
    private static OptionItem SidekickAssignMode;
    public static OptionItem KillCooldownSK;
    public static OptionItem CanVentSK;
    public static OptionItem CanUseSabotageSK;
    private static OptionItem SidekickCanKillJackal;
    private static OptionItem SidekickCanKillSidekick;
    private enum SidekickAssignModeSelectList
    {
        Jackal_SidekickAssignMode_SidekickAndRecruit,
        Jackal_SidekickAssignMode_Sidekick,
        Jackal_SidekickAssignMode_Recruit,
    }
    private enum SidekickCountModeSelectList
    {
        Jackal_SidekickCountMode_Jackal,
        Jackal_SidekickCountMode_None,
        Jackal_SidekickCountMode_Original,
    }

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Jackal, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jackal])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jackal]);
        CanUsesSabotage = BooleanOptionItem.Create(Id + 12, GeneralOption.CanUseSabotage, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jackal]);
        CanWinBySabotageWhenNoImpAlive = BooleanOptionItem.Create(Id + 14, "JackalCanWinBySabotageWhenNoImpAlive", true, TabGroup.NeutralRoles, false).SetParent(CanUsesSabotage);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jackal]);
        OptionResetKillCooldownWhenSbGetKilled = BooleanOptionItem.Create(Id + 16, "JackalResetKillCooldownWhenPlayerGetKilled", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jackal]);
        ResetKillCooldownOn = FloatOptionItem.Create(Id + 28, "JackalResetKillCooldownOn", new(0f, 180f, 2.5f), 15f, TabGroup.NeutralRoles, false)
            .SetParent(OptionResetKillCooldownWhenSbGetKilled)
            .SetValueFormat(OptionFormat.Seconds);
        JackalCanKillSidekick = BooleanOptionItem.Create(Id + 15, "JackalCanKillSidekick", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jackal]);
        CanRecruitSidekick = BooleanOptionItem.Create(Id + 30, "JackalCanRecruitSidekick", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jackal]);
        SidekickAssignMode = StringOptionItem.Create(Id + 34, "Jackal_SidekickAssignMode", EnumHelper.GetAllNames<SidekickAssignModeSelectList>(), 0, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick)
                .SetHidden(false);
        SidekickRecruitLimitOpt = IntegerOptionItem.Create(Id + 33, "JackalSidekickRecruitLimit", new(0, 15, 1), 2, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick)
                .SetValueFormat(OptionFormat.Times);
        KillCooldownSK = FloatOptionItem.Create(Id + 20, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick)
            .SetValueFormat(OptionFormat.Seconds);
        CanVentSK = BooleanOptionItem.Create(Id + 21, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick);
        CanUseSabotageSK = BooleanOptionItem.Create(Id + 22, GeneralOption.CanUseSabotage, true, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick);
        SidekickCanKillJackal = BooleanOptionItem.Create(Id + 23, "Jackal_SidekickCanKillJackal", false, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick);
        SidekickCanKillSidekick = BooleanOptionItem.Create(Id + 24, "Jackal_SidekickCanKillSidekick", false, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick);
        SidekickCountMode = StringOptionItem.Create(Id + 25, "Jackal_SidekickCountMode", EnumHelper.GetAllNames<SidekickCountModeSelectList>(), 0, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick)
            .SetHidden(false);
    }
    public override void Init()
    {
        ResetKillCooldownWhenSbGetKilled = OptionResetKillCooldownWhenSbGetKilled;
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = CanRecruitSidekick.GetBool() ? SidekickRecruitLimitOpt.GetInt() : 0;

        if (AmongUsClient.Instance.AmHost)
        {
            CustomRoleManager.CheckDeadBodyOthers.Add(OthersPlayersDead);
        }
    }
    public override void ApplyGameOptions(IGameOptions opt, byte babuyaga) => opt.SetVision(HasImpostorVision.GetBool());
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseSabotage(PlayerControl pc) => CanUsesSabotage.GetBool();
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();

    public static bool JackalKnowRole(PlayerControl seer, PlayerControl target)
    {
        if (seer.Is(CustomRoles.Jackal) && (target.Is(CustomRoles.Sidekick) || target.Is(CustomRoles.Recruit))) return true;
        else if (seer.Is(CustomRoles.Sidekick) && (target.Is(CustomRoles.Jackal) || target.Is(CustomRoles.Recruit) || target.Is(CustomRoles.Sidekick))) return true;
        else if (seer.Is(CustomRoles.Recruit) && (target.Is(CustomRoles.Jackal) || target.Is(CustomRoles.Sidekick) || target.Is(CustomRoles.Recruit))) return true;

        return false;
    }
    private bool CanRecruit(byte id) => AbilityLimit > 0;
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        if (CanRecruit(playerId))
            hud.KillButton?.OverrideText($"{GetString("GangsterButtonText")}");
        else
            hud.KillButton?.OverrideText($"{GetString("KillButtonText")}");
    }

    private void OthersPlayersDead(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        if (inMeeting || target.IsDisconnected()) return;

        if (ResetKillCooldownWhenSbGetKilled.GetBool() && !killer.Is(CustomRoles.Sidekick) && !killer.Is(CustomRoles.Jackal) && !target.Is(CustomRoles.Sidekick) && !target.Is(CustomRoles.Jackal) && !GameStates.IsMeeting)
        {
            Main.AllAlivePlayerControls
                .Where(x => !target.Is(CustomRoles.Jackal) && x.Is(CustomRoles.Jackal))
                .Do(x => x.SetKillCooldown(ResetKillCooldownOn.GetFloat()));
        }
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.Is(CustomRoles.Jackal)) return false;
        if (!CanRecruitSidekick.GetBool() || AbilityLimit < 1) return true;
        
        if (SidekickAssignMode.GetValue() != 2)
        {
            if (CanBeSidekick(target))
            {
                AbilityLimit--;
                SendSkillRPC();
                
                target.GetRoleClass()?.OnRemove(target.PlayerId);
                target.RpcSetCustomRole(CustomRoles.Sidekick);
                target.GetRoleClass()?.OnAdd(target.PlayerId);

                Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
                Utils.NotifyRoles(SpecifySeer: target, SpecifyTarget: killer, ForceLoop: true);

                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), GetString("GangsterSuccessfullyRecruited")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), GetString("BeRecruitedByJackal")));

                if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
                target.RpcGuardAndKill(killer);
                target.RpcGuardAndKill(target);

                killer.ResetKillCooldown();
                killer.SetKillCooldown();

                target.ResetKillCooldown();
                target.SetKillCooldown();

                Logger.Info($"Target: {target.GetRealName()} : {target.GetCustomRole()} => {CustomRoles.Sidekick}", "Assign Sidekick");
                
                if (AbilityLimit < 0)
                    HudManager.Instance.KillButton.OverrideText($"{GetString("KillButtonText")}");

                Logger.Info($"{killer.GetNameWithRole().RemoveHtmlTags()} - Recruit limit:{AbilityLimit}", "Jackal");
                return false;
            }
        }
        if (SidekickAssignMode.GetValue() != 1)
        {
            if (!target.GetCustomRole().IsNeutral() && !target.Is(CustomRoles.Sidekick) && !target.Is(CustomRoles.Recruit) && !target.Is(CustomRoles.Loyal) && !target.Is(CustomRoles.Admired))
            {
                AbilityLimit--;
                SendSkillRPC();
                target.RpcSetCustomRole(CustomRoles.Recruit);

                Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
                Utils.NotifyRoles(SpecifySeer: target, SpecifyTarget: killer, ForceLoop: true);

                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), GetString("GangsterSuccessfullyRecruited")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), GetString("BeRecruitedByJackal")));

                if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
                target.RpcGuardAndKill(killer);
                target.RpcGuardAndKill(target);

                killer.ResetKillCooldown();
                killer.SetKillCooldown();

                target.ResetKillCooldown();
                target.SetKillCooldown();

                Logger.Info($"Target: {target.GetRealName()} = {target.GetCustomRole()} => {CustomRoles.Recruit}", "Assign Recruit");
                
                if (AbilityLimit < 0)
                    HudManager.Instance.KillButton.OverrideText($"{GetString("KillButtonText")}");

                Logger.Info($"{killer.GetNameWithRole().RemoveHtmlTags()} - Recruit limit:{AbilityLimit}", "Jackal");
                return false;
            }
        }
        if (AbilityLimit < 0)
            HudManager.Instance.KillButton.OverrideText($"{GetString("KillButtonText")}");
        
        Logger.Info($"{killer.GetNameWithRole().RemoveHtmlTags()} - Recruit limit:{AbilityLimit}", "Jackal");
        return true;
    }

    public static bool CanBeSidekick(PlayerControl pc)
    {
        return pc != null && !pc.Is(CustomRoles.Sidekick) && !pc.Is(CustomRoles.Recruit) 
            && !pc.Is(CustomRoles.Loyal) && !pc.Is(CustomRoles.Admired) && !pc.Is(CustomRoles.Rascal) && !pc.Is(CustomRoles.Madmate) 
            && !pc.Is(CustomRoles.Charmed) && !pc.Is(CustomRoles.Infected) && !pc.Is(CustomRoles.Paranoia) 
            && !pc.Is(CustomRoles.Contagious) && pc.GetCustomRole().IsAbleToBeSidekicked() 
            && !(pc.GetCustomSubRoles().Contains(CustomRoles.Hurried) && !Hurried.CanBeConverted.GetBool());
    }

    private string GetRecruitLimit(byte playerId)
        => Utils.ColorString(CanRecruit(playerId)
            ? Utils.GetRoleColor(CustomRoles.Jackal).ShadeColor(0.25f)
            : Color.gray, $"({AbilityLimit})");
    
    public override string GetProgressText(byte playerId, bool comms)
        => CanRecruitSidekick.GetBool() ? GetRecruitLimit(playerId) : "";

    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        if (!JackalCanKillSidekick.GetBool())
        {
            // Jackal can kill Sidekick/Recruit
            if (killer.Is(CustomRoles.Jackal) && (target.Is(CustomRoles.Sidekick) || target.Is(CustomRoles.Recruit)))
                return true;

            // Sidekick/Recruit can kill Jackal
            else if ((killer.Is(CustomRoles.Sidekick) || killer.Is(CustomRoles.Recruit)) && target.Is(CustomRoles.Jackal))
                return true;
        }

        if (!SidekickCanKillSidekick.GetBool())
        {
            // Sidekick can kill Sidekick/Recruit
            if (killer.Is(CustomRoles.Sidekick) && (target.Is(CustomRoles.Sidekick) || target.Is(CustomRoles.Recruit)))
                return true;

            // Recruit can kill Recruit/Sidekick
            if (killer.Is(CustomRoles.Recruit) && (target.Is(CustomRoles.Recruit) || target.Is(CustomRoles.Sidekick)))
                return true;
        }

        if (!SidekickCanKillJackal.GetBool())
        {
            // Recruit/Sidekick can kill Jackal
            if (target.Is(CustomRoles.Jackal) && (killer.Is(CustomRoles.Sidekick) || killer.Is(CustomRoles.Recruit)))
                return true;
        }
        return false;
    }
}
