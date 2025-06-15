using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Core;
using TOHE.Roles.Crewmate;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Jackal : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Jackal;
    private const int Id = 16700;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Jackal);
    public static readonly HashSet<byte> Playerids = [];
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
    public static OptionItem SidekickRecruitLimitOpt;
    public static OptionItem SidekickCountMode;
    private static OptionItem SidekickAssignMode;
    public static OptionItem KillCooldownSK;
    public static OptionItem SidekickCanKillWhenJackalAlive;
    public static OptionItem SidekickTurnIntoJackal;
    public static OptionItem RestoreLimitOnNewJackal;
    public static OptionItem CanVentSK;
    public static OptionItem CanUseSabotageSK;
    private static OptionItem SidekickCanKillJackal;
    private static OptionItem SidekickCanKillSidekick;
    private static OptionItem CanRecruitImpostor;
    private static OptionItem CanRecruitNeutral;
    private static OptionItem CanRecruitCoven;

    private bool hasConverted;

    [Obfuscation(Exclude = true)]
    private enum SidekickAssignModeSelectList
    {
        Jackal_SidekickAssignMode_SidekickAndRecruit,
        Jackal_SidekickAssignMode_Sidekick,
        Jackal_SidekickAssignMode_Recruit,
    }
    [Obfuscation(Exclude = true)]
    private enum SidekickCountModeSelectList
    {
        Jackal_SidekickCountMode_Jackal,
        CountMode_None,
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

        CanRecruitSidekick = BooleanOptionItem.Create(Id + 30, "JackalCanRecruitSidekick", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jackal]);
        JackalCanKillSidekick = BooleanOptionItem.Create(Id + 15, "JackalCanKillSidekick", false, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick);
        SidekickAssignMode = StringOptionItem.Create(Id + 34, "Jackal_SidekickAssignMode", EnumHelper.GetAllNames<SidekickAssignModeSelectList>(), 0, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick)
                .SetHidden(false);
        SidekickRecruitLimitOpt = IntegerOptionItem.Create(Id + 33, "JackalSidekickRecruitLimit", new(0, 15, 1), 1, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick)
                .SetValueFormat(OptionFormat.Times);

        SidekickCanKillWhenJackalAlive = BooleanOptionItem.Create(Id + 35, "Jackal_SidekickCanKillWhenJackalAlive", false, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick);
        SidekickTurnIntoJackal = BooleanOptionItem.Create(Id + 36, "Jackal_SidekickTurnIntoJackal", true, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick);
        RestoreLimitOnNewJackal = BooleanOptionItem.Create(Id + 37, "Jackal_RestoreLimitOnNewJackal", true, TabGroup.NeutralRoles, false).SetParent(SidekickTurnIntoJackal);

        KillCooldownSK = FloatOptionItem.Create(Id + 20, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick)
            .SetValueFormat(OptionFormat.Seconds);
        CanVentSK = BooleanOptionItem.Create(Id + 21, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick);
        CanUseSabotageSK = BooleanOptionItem.Create(Id + 22, GeneralOption.CanUseSabotage, true, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick);

        SidekickCanKillJackal = BooleanOptionItem.Create(Id + 23, "Jackal_SidekickCanKillJackal", false, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick);
        SidekickCanKillSidekick = BooleanOptionItem.Create(Id + 24, "Jackal_SidekickCanKillSidekick", false, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick);
        SidekickCountMode = StringOptionItem.Create(Id + 25, "Jackal_SidekickCountMode", EnumHelper.GetAllNames<SidekickCountModeSelectList>(), 0, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick)
            .SetHidden(false);

        CanRecruitImpostor = BooleanOptionItem.Create(Id + 40, "JackalCanRecruitImpostor", true, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick);
        CanRecruitNeutral = BooleanOptionItem.Create(Id + 41, "JackalCanRecruitNeutral", true, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick);
        CanRecruitCoven = BooleanOptionItem.Create(Id + 42, "JackalCanRecruitCoven", true, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick);
    }
    public override void Init()
    {
        ResetKillCooldownWhenSbGetKilled = OptionResetKillCooldownWhenSbGetKilled;
        Playerids.Clear();
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(SidekickRecruitLimitOpt.GetInt());
        hasConverted = false;

        if (!Playerids.Contains(playerId))
            Playerids.Add(playerId);

        if ((Playerids.Count > 1 && !RestoreLimitOnNewJackal.GetBool())
        || !CanRecruitSidekick.GetBool())
            playerId.SetAbilityUseLimit(0);

        if (AmongUsClient.Instance.AmHost)
        {
            CustomRoleManager.CheckDeadBodyOthers.Add(OthersPlayersDead);
            if (_Player.Is(CustomRoles.Recruit))
            {
                Main.PlayerStates[playerId].RemoveSubRole(CustomRoles.Recruit);
            }
        }
    }

    public override void Remove(byte playerId)
    {
        CustomRoleManager.CheckDeadBodyOthers.Remove(OthersPlayersDead);
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
    private bool CanRecruit() => _Player?.GetAbilityUseLimit() > 0;
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        if (CanRecruit())
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
        var addon = killer.GetBetrayalAddon(true);
        var role = addon switch
        {
            CustomRoles.Admired => CustomRoles.Sheriff,
            CustomRoles.Madmate => CustomRoles.Refugee,
            _ => CustomRoles.Sidekick
        };

        if (target.Is(CustomRoles.Jackal)) return false;
        if (target.Is(addon) || target.Is(role)) return JackalCanKillSidekick.GetBool();

        if (!CanRecruitSidekick.GetBool() || !CanRecruit())
        {
            Logger.Info("Jackal run out of recruits or Recruit disabled?", "Jackal");
            return true;
        }
        bool TargetCanBeSidekick = (CanRecruitCoven.GetBool() && target.IsPlayerCovenTeam())
        || (CanRecruitNeutral.GetBool() && target.IsPlayerNeutralTeam() && !target.GetCustomRole().IsNA())
        || (CanRecruitImpostor.GetBool() && target.IsPlayerImpostorTeam())
        || target.IsPlayerCrewmateTeam();

        switch (SidekickAssignMode.GetInt())
        {
            case 1: // Only SideKick
                if (!TargetCanBeSidekick)
                {
                    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), GetString("Jackal_RecruitFailed")));
                    return true;
                }
                killer.RpcRemoveAbilityUse();

                Logger.Info($"Jackal {killer.GetNameWithRole()} assigned {role} to {target.GetNameWithRole()}", "Jackal");

                // Remove other team converted roles first
                foreach (var x in target.GetCustomSubRoles())
                {
                    if (x.IsBetrayalAddonV2() && x != addon)
                    {
                        Main.PlayerStates[target.PlayerId].RemoveSubRole(x);
                        Main.PlayerStates[target.PlayerId].SubRoles.Remove(CustomRoles.Rascal);
                    }
                }

                target.GetRoleClass()?.OnRemove(target.PlayerId);
                target.RpcChangeRoleBasis(role);
                target.RpcSetCustomRole(role);
                target.GetRoleClass()?.OnAdd(target.PlayerId);

                killer.Notify(Utils.ColorString(Utils.GetRoleColor(role), GetString("GangsterSuccessfullyRecruited")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(role), GetString("BeRecruitedByJackal")));

                if (role is CustomRoles.Sidekick && killer.GetBetrayalAddon() != CustomRoles.NotAssigned)
                    target.RpcSetCustomRole(addon);

                Utils.NotifyRoles(killer, target, true);
                Utils.NotifyRoles(target, killer, true);

                target.ResetKillCooldown();
                target.SetKillCooldown(forceAnime: true);
                killer.ResetKillCooldown();
                killer.SetKillCooldown(forceAnime: !DisableShieldAnimations.GetBool());
                break;
            case 2: // Only Recruit
                if (!target.CanBeRecruitedBy(killer))
                {
                    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), GetString("Jackal_RecruitFailed")));
                    return true;
                }

                killer.RpcRemoveAbilityUse();
                Logger.Info($"Jackal {killer.GetNameWithRole()} assigned {addon} to {target.GetNameWithRole()}", "Jackal");
                target.RpcSetCustomRole(addon);

                killer.Notify(Utils.ColorString(Utils.GetRoleColor(addon), GetString("GangsterSuccessfullyRecruited")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(addon), GetString("BeRecruitedByJackal")));

                if (addon is CustomRoles.Admired)
                {
                    Admirer.AdmiredList[killer.PlayerId].Add(target.PlayerId);
                    Admirer.SendRPC(killer.PlayerId, target.PlayerId); //Sync playerId list
                }

                Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
                Utils.NotifyRoles(SpecifySeer: target, SpecifyTarget: killer, ForceLoop: true);

                killer.ResetKillCooldown();
                killer.SetKillCooldown(forceAnime: !DisableShieldAnimations.GetBool());

                target.ResetKillCooldown();
                target.SetKillCooldown(forceAnime: true);
                break;
            case 0: // SideKick when failed Recruit
                if (TargetCanBeSidekick)
                {
                    // Remove other team converted roles first
                    foreach (var x in target.GetCustomSubRoles())
                    {
                        if (x.IsBetrayalAddonV2() && x != addon)
                        {
                            Main.PlayerStates[target.PlayerId].RemoveSubRole(x);
                            Main.PlayerStates[target.PlayerId].SubRoles.Remove(CustomRoles.Rascal);
                        }
                    }
                    target.GetRoleClass()?.OnRemove(target.PlayerId);
                    target.RpcChangeRoleBasis(role);
                    target.RpcSetCustomRole(role);
                    target.GetRoleClass()?.OnAdd(target.PlayerId);
                    if (role is CustomRoles.Sidekick && killer.GetBetrayalAddon() != CustomRoles.NotAssigned)
                        target.RpcSetCustomRole(addon);
                }
                else if (!target.CanBeRecruitedBy(killer))
                {
                    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), GetString("Jackal_RecruitFailed")));
                    return true;
                }
                else
                {
                    target.RpcSetCustomRole(addon);
                }
                killer.RpcRemoveAbilityUse();

                killer.Notify(Utils.ColorString(Utils.GetRoleColor(addon), GetString("GangsterSuccessfullyRecruited")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(addon), GetString("BeRecruitedByJackal")));

                if (addon is CustomRoles.Admired)
                {
                    Admirer.AdmiredList[killer.PlayerId].Add(target.PlayerId);
                    Admirer.SendRPC(killer.PlayerId, target.PlayerId); //Sync playerId list
                }

                Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
                Utils.NotifyRoles(SpecifySeer: target, SpecifyTarget: killer, ForceLoop: true);

                killer.ResetKillCooldown();
                killer.SetKillCooldown(forceAnime: !DisableShieldAnimations.GetBool());

                target.ResetKillCooldown();
                target.SetKillCooldown(forceAnime: true);
                break;
        }

        Logger.Info($"{killer.GetNameWithRole().RemoveHtmlTags()} - Recruit limit:{killer.GetAbilityUseLimit()}", "Jackal");

        return false;

        /*
        if (target.Is(CustomRoles.Jackal)) return false;
        if (!CanRecruitSidekick.GetBool() || AbilityLimit < 1) return true;
        
        if (SidekickAssignMode.GetValue() != 2)
        {
            if (CanBeSidekick(target))
            {
                killer.RpcRemoveAbilityUse();
                
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
                Logger.Info($"{killer.GetNameWithRole().RemoveHtmlTags()} - Recruit limit:{AbilityLimit}", "Jackal");
                return false;
            }
        }
        if (SidekickAssignMode.GetValue() != 1)
        {
            if (!target.GetCustomRole().IsNeutral() && !target.Is(CustomRoles.Sidekick) && !target.Is(CustomRoles.Recruit) && !target.Is(CustomRoles.Loyal) && !target.Is(CustomRoles.Admired))
            {
                killer.RpcRemoveAbilityUse();
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
                Logger.Info($"{killer.GetNameWithRole().RemoveHtmlTags()} - Recruit limit:{AbilityLimit}", "Jackal");
                return false;
            }
        }
        
        Logger.Info($"{killer.GetNameWithRole().RemoveHtmlTags()} - Recruit limit:{AbilityLimit}", "Jackal");
        return true;
        */
    }

    // very very Long Dog shit lmao
    public static bool CanBeSidekick(PlayerControl pc)
    {
        return pc != null && !pc.Is(CustomRoles.Sidekick) && !pc.Is(CustomRoles.Recruit)
            && !pc.Is(CustomRoles.Loyal) && !pc.Is(CustomRoles.Admired) && !pc.Is(CustomRoles.Rascal) && !pc.Is(CustomRoles.Madmate)
            && !pc.Is(CustomRoles.Charmed) && !pc.Is(CustomRoles.Infected) && !pc.Is(CustomRoles.Paranoia)
            && !pc.Is(CustomRoles.Contagious) && !pc.Is(CustomRoles.Enchanted) && pc.GetCustomRole().IsAbleToBeSidekicked() && !(CovenManager.HasNecronomicon(pc.PlayerId) && pc.Is(CustomRoles.CovenLeader));
    }

    private static readonly object OnMurderPlayerAsTargetLock = new();

    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuidice)
    {
        lock (OnMurderPlayerAsTargetLock)
        {
            if (_Player == null || !target.Is(CustomRoles.Jackal) || target.IsAlive() || target.PlayerId != _Player.PlayerId) return;

            if (hasConverted) return;

            if (SidekickTurnIntoJackal.GetBool())
            {
                Logger.Info("Starting Jackal Death Assign.", "Jackal");
                var readySideKicks = Main.AllAlivePlayerControls.Where(x => x.IsAlive() && x.Is(CustomRoles.Sidekick) && x.PlayerId != target.PlayerId).ToList();

                if (readySideKicks.Count < 1)
                {
                    readySideKicks = Main.AllAlivePlayerControls.Where(x => x.IsAlive() && x.Is(CustomRoles.Recruit) && x.PlayerId != target.PlayerId).ToList();
                }

                if (readySideKicks.Count < 1)
                {
                    Logger.Info("Jackal dead, but no alive sidekick can be assigned!", "Jackal");
                    hasConverted = true;
                    return;
                }

                var newJackal = readySideKicks.RandomElement();
                if (newJackal.IsAlive())
                {
                    Logger.Info($"Assigned new Jackal {newJackal.GetNameWithRole()}", "Jackal");

                    newJackal.GetRoleClass()?.OnRemove(newJackal.PlayerId);
                    newJackal.RpcChangeRoleBasis(CustomRoles.Jackal);
                    newJackal.RpcSetCustomRole(CustomRoles.Jackal);
                    newJackal.GetRoleClass()?.OnAdd(target.PlayerId);

                    Main.PlayerStates[newJackal.PlayerId].RemoveSubRole(CustomRoles.Recruit);
                    newJackal.PlayerId.SetAbilityUseLimit(RestoreLimitOnNewJackal.GetBool() && CanRecruitSidekick.GetBool() ? SidekickRecruitLimitOpt.GetInt() : 0);

                    if (inMeeting)
                    {
                        Utils.SendMessage(string.Format(GetString("Jackal_OnBecomeNewJackalMeeting"), target.GetRealName(true)), newJackal.PlayerId);
                        foreach (var player in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Recruit) || x.Is(CustomRoles.Sidekick)))
                        {
                            if (player.PlayerId == newJackal.PlayerId) continue;
                            Utils.SendMessage(string.Format(GetString("Jackal_OnNewJackalSelectedMeeting"), target.GetRealName(true), newJackal.GetRealName(true)), player.PlayerId);
                        }
                    }

                    newJackal.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), GetString("Jackal_BecomeNewJackal")));
                    newJackal.ResetKillCooldown();
                    target.SetKillCooldown(forceAnime: true);

                    foreach (var player in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Recruit) || x.Is(CustomRoles.Sidekick)))
                    {
                        player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), string.Format(GetString("Jackal_OnNewJackalSelected"), newJackal.GetRealName())));
                    }
                    Utils.NotifyRoles(SpecifyTarget: newJackal);

                    hasConverted = true;
                }
                else
                {
                    Logger.Info($"Selected alive Sidekick [{newJackal.PlayerId}]{newJackal.GetNameWithRole()} is dead? wtf", "Jackal");
                    hasConverted = true;
                }
            }
            else
            {
                Logger.Info("Opps, Jackal boss is dead!", "Jackal");
                foreach (var player in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Recruit) || x.Is(CustomRoles.Sidekick)))
                {
                    player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), GetString("Jackal_BossIsDead")));
                }
                hasConverted = true;
            }
        }
    }

    public override void AfterMeetingTasks()
    {
        if (_Player && !_Player.IsAlive() && !hasConverted)
        {
            OnMurderPlayerAsTarget(_Player, _Player, true, false);
        }
    }

    public override string GetProgressText(byte playerId, bool comms)
        => CanRecruitSidekick.GetBool() ? Utils.GetAbilityUseLimitDisplay(playerId, true) : string.Empty;

    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        if (!JackalCanKillSidekick.GetBool())
        {
            // Jackal can kill Sidekick/Recruit
            if (killer.Is(CustomRoles.Jackal) && (target.Is(CustomRoles.Sidekick) || target.Is(CustomRoles.Recruit)))
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

internal class Sidekick : RoleBase
{
    public override CustomRoles Role => CustomRoles.Sidekick;

    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;

    public override void Init()
    {

    }
    public override void Add(byte playerId)
    {
        Main.PlayerStates[playerId].taskState.hasTasks = false;
        playerId.SetAbilityUseLimit(0);

        if (Jackal.RestoreLimitOnNewJackal.GetBool())
        {
            playerId.SetAbilityUseLimit(Jackal.SidekickRecruitLimitOpt.GetInt());
        }
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Jackal.KillCooldownSK.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte ico) => opt.SetVision(Jackal.HasImpostorVision.GetBool());
    public override bool CanUseKillButton(PlayerControl player) => Jackal.SidekickCanKillWhenJackalAlive.GetBool() || !CustomRoles.Jackal.RoleExist();
    public override bool CanUseImpostorVentButton(PlayerControl player) => Jackal.CanVentSK.GetBool();
    public override bool CanUseSabotage(PlayerControl player) => Jackal.CanUseSabotageSK.GetBool();
    public override string GetProgressText(byte playerId, bool comms) => string.Empty;

    //public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target) => SidekickKnowRole(target);
    //public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target) => SidekickKnowRole(target) ? Main.roleColors[CustomRoles.Jackal] : string.Empty;

    //private static bool SidekickKnowRole(PlayerControl target)
    //{
    //    return target.Is(CustomRoles.Jackal) || target.Is(CustomRoles.Recruit) || target.Is(CustomRoles.Sidekick);
    //}

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("KillButtonText"));
        hud.SabotageButton.OverrideText(GetString("SabotageButtonText"));
    }
}
