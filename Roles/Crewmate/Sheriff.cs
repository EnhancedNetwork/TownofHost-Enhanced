using AmongUs.GameOptions;
using TOHE.Modules;
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
    public static OptionItem ShotLimitOpt;
    public static OptionItem ShowShotLimit;
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
        NonCrewCanKillNeutral = BooleanOptionItem.Create(Id + 20, "SheriffCanKillNeutrals", true, TabGroup.CrewmateRoles, false).SetParent(SetNonCrewCanKill);
        NonCrewCanKillCoven = BooleanOptionItem.Create(Id + 31, "SheriffCanKillCoven", true, TabGroup.CrewmateRoles, false).SetParent(SetNonCrewCanKill);
    }
    public override void Add(byte playerId)
    {
        CurrentKillCooldown = KillCooldown.GetFloat();
        playerId.SetAbilityUseLimit(ShotLimitOpt.GetInt());
    }
    private static void SetUpNeutralOptions(int Id)
    {
        foreach (var neutral in CustomRolesHelper.AllRoles.Where(x => (x.IsNeutral() || x is CustomRoles.Rebel) && !x.IsTNA() && x is not CustomRoles.Glitch and not CustomRoles.Killer).ToArray())
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
    public static bool IsUseKillButton(PlayerControl pc)
        => (CanKillAllAlive.GetBool() || GameStates.AlreadyDied) && pc.GetAbilityUseLimit() > 0;

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        killer.RpcRemoveAbilityUse();

        if ((CanBeKilledBySheriff(target) && !(SetNonCrewCanKill.GetBool() && killer.IsNonCrewSheriff() || SidekickSheriffCanGoBerserk.GetBool() && killer.Is(CustomRoles.Recruit)))
            || (SidekickSheriffCanGoBerserk.GetBool() && killer.Is(CustomRoles.Recruit))
            || (SetNonCrewCanKill.GetBool() && killer.IsNonCrewSheriff()
                 && ((target.GetCustomRole().IsImpostorTeamV3() && NonCrewCanKillImp.GetBool()) || (target.GetCustomRole().IsCrewmate() && NonCrewCanKillCrew.GetBool()) || (target.GetCustomRole().IsNeutral() && NonCrewCanKillNeutral.GetBool()) || (target.GetCustomRole().IsCoven() && NonCrewCanKillCoven.GetBool())))
            )
        {
            killer.ResetKillCooldown();
            if (killer.GetAbilityUseLimit() < 1)
            {
                killer.SetKillCooldown();
            }
            return true;
        }
        killer.SetDeathReason(PlayerState.DeathReason.Misfire);
        killer.RpcMurderPlayer(killer);
        return MisfireKillsTarget.GetBool();
    }
    public static bool CanBeKilledBySheriff(PlayerControl player)
    {
        var cRole = player.GetCustomRole();
        bool CanKill = false;
        if (player.Is(CustomRoles.Madmate) && CanKillMadmate.GetBool())
            CanKill = true;
        if (player.Is(CustomRoles.Charmed) && CanKillCharmed.GetBool())
            CanKill = true;
        if (player.Is(CustomRoles.Lovers) && CanKillLovers.GetBool())
            CanKill = true;
        if (player.Is(CustomRoles.Recruit) && CanKillSidekicks.GetBool())
            CanKill = true;
        if (player.Is(CustomRoles.Egoist) && CanKillEgoists.GetBool())
            CanKill = true;
        if (player.Is(CustomRoles.Infected) && CanKillInfected.GetBool())
            CanKill = true;
        if (player.Is(CustomRoles.Contagious) && CanKillContagious.GetBool())
            CanKill = true;
        if (player.Is(CustomRoles.Enchanted) && CanKillEnchanted.GetBool())
            CanKill = true;
        if (player.Is(CustomRoles.Rascal))
            CanKill = true;

        if (CanKill) return true;
        else if (player.Is(CustomRoles.Admired) && MisfireOnAdmired.GetBool()) return false;
        else if (player.Is(CustomRoles.Narc)) return false; //copycat sheriff can still kill narc so this exists
        else if (player.Is(CustomRoles.Rebel)) return CanKillNeutrals.GetBool() && (CanKillNeutralsMode.GetValue() == 0 || !KillTargetOptions.TryGetValue(cRole, out var option) || option.GetBool());
        return cRole switch
        {
            CustomRoles.Trickster => false,
            var _ when cRole.IsTNA() => false,
            _ => cRole.GetCustomRoleTeam() switch
            {
                Custom_Team.Impostor => true,
                Custom_Team.Neutral => CanKillNeutrals.GetBool() && (CanKillNeutralsMode.GetValue() == 0 || !KillTargetOptions.TryGetValue(cRole, out var option) || option.GetBool()),
                Custom_Team.Coven => CanKillCoven.GetBool(),
                _ => false,
            }
        };
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.KillButton.OverrideText(GetString("SheriffKillButtonText"));
    }
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Kill");
}
