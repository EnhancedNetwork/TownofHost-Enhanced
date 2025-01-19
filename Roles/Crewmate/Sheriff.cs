using AmongUs.GameOptions;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Sheriff : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Sheriff;
    private const int Id = 11200;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Sheriff);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem MisfireKillsTarget;
    private static OptionItem ShotLimitOpt;
    private static OptionItem ShowShotLimit;
    private static OptionItem CanKillAllAlive;
    private static OptionItem CanKillCoven;
    private static OptionItem MisfireOnAdmired;
    private static OptionItem CanKillNeutrals;
    private static OptionItem CanKillNeutralsMode;
    private static OptionItem CanKillMadmate;
    private static OptionItem CanKillCharmed;
    private static OptionItem CanKillLovers;
    private static OptionItem CanKillSidekicks;
    private static OptionItem CanKillEgoists;
    private static OptionItem CanKillInfected;
    private static OptionItem CanKillContagious;
    private static OptionItem CanKillEnchanted;
    private static OptionItem SidekickSheriffCanGoBerserk;
    private static OptionItem SetNonCrewCanKill;
    private static OptionItem NonCrewCanKillCrew;
    private static OptionItem NonCrewCanKillImp;
    private static OptionItem NonCrewCanKillNeutral;
    private static OptionItem NonCrewCanKillCoven;

    private float CurrentKillCooldown;

    private static readonly Dictionary<CustomRoles, OptionItem> KillTargetOptions = [];

    [Obfuscation(Exclude = true)]
    private enum KillOptionList
    {
        SheriffCanKillAll,
        SheriffCanKillSeparately
    }

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Sheriff);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 60f, 2.5f), 15f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sheriff])
            .SetValueFormat(OptionFormat.Seconds);
        MisfireKillsTarget = BooleanOptionItem.Create(Id + 11, "SheriffMisfireKillsTarget", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sheriff]);
        ShotLimitOpt = IntegerOptionItem.Create(Id + 12, "SheriffShotLimit", new(1, 15, 1), 6, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sheriff])
            .SetValueFormat(OptionFormat.Times);
        ShowShotLimit = BooleanOptionItem.Create(Id + 13, "SheriffShowShotLimit", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sheriff]);
        CanKillAllAlive = BooleanOptionItem.Create(Id + 15, "SheriffCanKillAllAlive", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sheriff]);
        CanKillMadmate = BooleanOptionItem.Create(Id + 17, "SheriffCanKillMadmate", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sheriff]);
        CanKillCharmed = BooleanOptionItem.Create(Id + 22, "SheriffCanKillCharmed", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sheriff]);
        CanKillLovers = BooleanOptionItem.Create(Id + 24, "SheriffCanKillLovers", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sheriff]);
        CanKillSidekicks = BooleanOptionItem.Create(Id + 23, "SheriffCanKillSidekick", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sheriff]);
        CanKillEgoists = BooleanOptionItem.Create(Id + 25, "SheriffCanKillEgoist", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sheriff]);
        CanKillInfected = BooleanOptionItem.Create(Id + 26, "SheriffCanKillInfected", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sheriff]);
        CanKillContagious = BooleanOptionItem.Create(Id + 27, "SheriffCanKillContagious", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sheriff]);
        CanKillEnchanted = BooleanOptionItem.Create(Id + 30, "SheriffCanKillEnchanted", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sheriff]);
        CanKillCoven = BooleanOptionItem.Create(Id + 29, "SheriffCanKillCoven", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sheriff]);
        MisfireOnAdmired = BooleanOptionItem.Create(Id + 32, "SheriffMisfireOnAdmired", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sheriff]);
        CanKillNeutrals = BooleanOptionItem.Create(Id + 16, "SheriffCanKillNeutrals", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sheriff]);
        CanKillNeutralsMode = StringOptionItem.Create(Id + 14, "SheriffCanKillNeutralsMode", EnumHelper.GetAllNames<KillOptionList>(), 0, TabGroup.CrewmateRoles, false).SetParent(CanKillNeutrals);
        SetUpNeutralOptions(Id + 33);
        SidekickSheriffCanGoBerserk = BooleanOptionItem.Create(Id + 28, "SidekickSheriffCanGoBerserk", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sheriff]);
        SetNonCrewCanKill = BooleanOptionItem.Create(Id + 18, "SheriffSetMadCanKill", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sheriff]);
        NonCrewCanKillImp = BooleanOptionItem.Create(Id + 19, "SheriffMadCanKillImp", true, TabGroup.CrewmateRoles, false).SetParent(SetNonCrewCanKill);
        NonCrewCanKillCrew = BooleanOptionItem.Create(Id + 21, "SheriffMadCanKillCrew", true, TabGroup.CrewmateRoles, false).SetParent(SetNonCrewCanKill);
        NonCrewCanKillNeutral = BooleanOptionItem.Create(Id + 20, "SheriffMadCanKillNeutral", true, TabGroup.CrewmateRoles, false).SetParent(SetNonCrewCanKill);
        NonCrewCanKillCoven = BooleanOptionItem.Create(Id + 31, "SheriffMadCanKillCoven", true, TabGroup.CrewmateRoles, false).SetParent(SetNonCrewCanKill);
    }
    public override void Add(byte playerId)
    {
        CurrentKillCooldown = KillCooldown.GetFloat();
        AbilityLimit = ShotLimitOpt.GetInt();

        Logger.Info($"{GetPlayerById(playerId)?.GetNameWithRole()} : limit: {AbilityLimit}", "Sheriff");
    }
    private static void SetUpNeutralOptions(int Id)
    {
        foreach (var neutral in CustomRolesHelper.AllRoles.Where(x => x.IsNeutral() && !x.IsTNA() && x is not CustomRoles.Glitch and not CustomRoles.Killer).ToArray())
        {
            SetUpKillTargetOption(neutral, Id, true, CanKillNeutralsMode);
            Id++;
        }
    }
    private static void SetUpKillTargetOption(CustomRoles role, int Id, bool defaultValue = true, OptionItem parent = null)
    {
        parent ??= CustomRoleSpawnChances[CustomRoles.Sheriff];
        var roleName = GetRoleName(role);
        Dictionary<string, string> replacementDic = new() { { "%role%", ColorString(GetRoleColor(role), roleName) } };
        KillTargetOptions[role] = BooleanOptionItem.Create(Id, "SheriffCanKill%role%", defaultValue, TabGroup.CrewmateRoles, false).SetParent(parent);
        KillTargetOptions[role].ReplacementDictionary = replacementDic;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = IsUseKillButton(GetPlayerById(id)) ? CurrentKillCooldown : 300f;

    public override bool CanUseKillButton(PlayerControl pc) => IsUseKillButton(pc);
    public bool IsUseKillButton(PlayerControl pc)
        => pc.IsAlive()
        && (CanKillAllAlive.GetBool() || GameStates.AlreadyDied)
        && AbilityLimit > 0;

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        AbilityLimit--;
        Logger.Info($"{killer.GetNameWithRole()} : Number of kills left: {AbilityLimit}", "Sheriff");
        SendSkillRPC();
        if ((CanBeKilledBySheriff(target) && !(SetNonCrewCanKill.GetBool() && killer.IsNonCrewSheriff() || SidekickSheriffCanGoBerserk.GetBool() && killer.Is(CustomRoles.Recruit)))
            || (SidekickSheriffCanGoBerserk.GetBool() && killer.Is(CustomRoles.Recruit))
            || (SetNonCrewCanKill.GetBool() && killer.IsNonCrewSheriff()
                 && ((target.GetCustomRole().IsImpostorTeamV3() && NonCrewCanKillImp.GetBool()) || (target.IsNonRebelCrewmate() && NonCrewCanKillCrew.GetBool()) || (target.IsRebelNeutralV3() && NonCrewCanKillNeutral.GetBool()) || (target.GetCustomRole().IsCoven() && NonCrewCanKillCoven.GetBool())))
            )
        {
            killer.ResetKillCooldown();
            if (AbilityLimit < 1)
            {
                killer.SetKillCooldown();
            }
            return true;
        }
        killer.SetDeathReason(PlayerState.DeathReason.Misfire);
        killer.RpcMurderPlayer(killer);
        return MisfireKillsTarget.GetBool();
    }
    public override string GetProgressText(byte playerId, bool computervirus)
        => ShowShotLimit.GetBool() ? ColorString(IsUseKillButton(GetPlayerById(playerId)) ? GetRoleColor(CustomRoles.Sheriff).ShadeColor(0.25f) : Color.gray, $"({AbilityLimit})") : "";
    public static bool CanBeKilledBySheriff(PlayerControl player)
    {
        var cRole = player.GetCustomRole();
        var subRole = player.GetCustomSubRoles();
        bool CanKill = false;
        foreach (var SubRoleTarget in subRole)
        {
            CanKill = SubRoleTarget switch
            {
                CustomRoles.Madmate => CanKillMadmate.GetBool(),
                CustomRoles.Charmed => CanKillCharmed.GetBool(),
                CustomRoles.Lovers => CanKillLovers.GetBool(),
                CustomRoles.Recruit => CanKillSidekicks.GetBool(),
                CustomRoles.Egoist => CanKillEgoists.GetBool(),
                CustomRoles.Infected => CanKillInfected.GetBool(),
                CustomRoles.Contagious => CanKillContagious.GetBool(),
                CustomRoles.Enchanted => CanKillEnchanted.GetBool(),
                CustomRoles.Rascal => true,
                CustomRoles.Admired => false,
                _ => false,
            };
        }

        bool CanKillAdmired = !(player.Is(CustomRoles.Admired) && MisfireOnAdmired.GetBool());
        return cRole switch
        {
            CustomRoles.Trickster => false,
            var r when cRole.IsTNA() => false,
            _ => player.GetCustomRoleTeam() switch
            {
                Custom_Team.Impostor => CanKillAdmired,
                Custom_Team.Neutral => CanKillNeutrals.GetBool() && (CanKillNeutralsMode.GetValue() == 0 || !KillTargetOptions.TryGetValue(cRole, out var option) || option.GetBool()) && CanKillAdmired,
                Custom_Team.Coven => CanKillCoven.GetBool() && CanKillAdmired,
                _ => CanKill,
            }
        };
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.KillButton.OverrideText(GetString("SheriffKillButtonText"));
    }
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Kill");
}
