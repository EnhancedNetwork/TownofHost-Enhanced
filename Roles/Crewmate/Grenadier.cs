using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Grenadier : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Grenadier;
    private const int Id = 8200;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Grenadier);
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateHindering;
    public override bool BlockMoveInVent(PlayerControl pc) => true;
    //==================================================================\\

    private static readonly Dictionary<byte, long> GrenadierBlinding = [];
    private static readonly Dictionary<byte, long> MadGrenadierBlinding = [];

    private static OptionItem GrenadierSkillCooldown;
    private static OptionItem GrenadierSkillDuration;
    private static OptionItem GrenadierCauseVision;
    private static OptionItem GrenadierCanAffectNeutral;
    private static OptionItem GrenadierCanAffectCoven;
    private static OptionItem GrenadierSkillMaxOfUseage;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Grenadier);
        GrenadierSkillCooldown = FloatOptionItem.Create(Id + 10, "GrenadierSkillCooldown", new(1f, 180f, 1f), 25f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier])
            .SetValueFormat(OptionFormat.Seconds);
        GrenadierSkillDuration = FloatOptionItem.Create(Id + 11, "GrenadierSkillDuration", new(1f, 60f, 1f), 10f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier])
            .SetValueFormat(OptionFormat.Seconds);
        GrenadierCauseVision = FloatOptionItem.Create(Id + 12, "GrenadierCauseVision", new(0f, 5f, 0.05f), 0.3f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier])
            .SetValueFormat(OptionFormat.Multiplier);
        GrenadierCanAffectNeutral = BooleanOptionItem.Create(Id + 13, "GrenadierCanAffectNeutral", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier]);
        GrenadierCanAffectCoven = BooleanOptionItem.Create(Id + 16, "GrenadierCanAffectCoven", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier]);
        GrenadierSkillMaxOfUseage = FloatOptionItem.Create(Id + 14, "GrenadierSkillMaxOfUseage", new(0, 20, 1), 2, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier])
            .SetValueFormat(OptionFormat.Times);
        GrenadierAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 15, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier])
            .SetValueFormat(OptionFormat.Times);
        OverrideTasksData.Create(Id + 17, TabGroup.CrewmateRoles, CustomRoles.Grenadier);

    }

    public override void Init()
    {
        GrenadierBlinding.Clear();
        MadGrenadierBlinding.Clear();
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(GrenadierSkillMaxOfUseage.GetFloat());
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = GrenadierSkillCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
    public static void ApplyGameOptionsForOthers(IGameOptions opt, PlayerControl player)
    {
        // Grenadier or Mad Grenadier enter the vent
        if ((GrenadierBlinding.Any() &&
            (player.GetCustomRole().IsImpostor() ||
            (player.GetCustomRole().IsNeutral() && GrenadierCanAffectNeutral.GetBool()) ||
            (player.GetCustomRole().IsCoven() && GrenadierCanAffectCoven.GetBool()))
            )
            || (MadGrenadierBlinding.Any() && !player.GetCustomRole().IsImpostorTeam() && !player.Is(CustomRoles.Madmate)))
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, GrenadierCauseVision.GetFloat());
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, GrenadierCauseVision.GetFloat());
        }
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        GrenadierBlinding.Clear();
        MadGrenadierBlinding.Clear();
    }

    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (pc.GetAbilityUseLimit() >= 1)
        {
            if (pc.Is(CustomRoles.Madmate))
            {
                MadGrenadierBlinding.Remove(pc.PlayerId);
                MadGrenadierBlinding.Add(pc.PlayerId, GetTimeStamp());
                Main.AllPlayerControls.Where(x => x.IsModded())
                    .Where(x => !x.GetCustomRole().IsImpostorTeam() && !x.Is(CustomRoles.Madmate))
                    .Do(x => x.RPCPlayCustomSound("FlashBang"));
            }
            // Why in the world is there a separate list for Mad, whatever i guess -- Marg
            else if (pc.Is(CustomRoles.Enchanted))
            {
                MadGrenadierBlinding.Remove(pc.PlayerId);
                MadGrenadierBlinding.Add(pc.PlayerId, GetTimeStamp());
                Main.AllPlayerControls.Where(x => x.IsModded())
                    .Where(x => !x.GetCustomRole().IsCoven() && !x.Is(CustomRoles.Enchanted))
                    .Do(x => x.RPCPlayCustomSound("FlashBang"));
            }
            else
            {
                GrenadierBlinding.Remove(pc.PlayerId);
                GrenadierBlinding.Add(pc.PlayerId, GetTimeStamp());
                Main.AllPlayerControls.Where(x => x.IsModded())
                    .Where(x => x.GetCustomRole().IsImpostor() || (x.GetCustomRole().IsNeutral() && GrenadierCanAffectNeutral.GetBool()) || (x.GetCustomRole().IsCoven() && GrenadierCanAffectCoven.GetBool()))
                    .Do(x => x.RPCPlayCustomSound("FlashBang"));
            }
            if (!DisableShieldAnimations.GetBool()) pc.RpcGuardAndKill(pc);
            pc.RPCPlayCustomSound("FlashBang");
            pc.Notify(GetString("AbilityInUse"), GrenadierSkillDuration.GetFloat());
            pc.RpcRemoveAbilityUse();
            MarkEveryoneDirtySettings();
        }
        else
        {
            pc.Notify(GetString("OutOfAbilityUsesDoMoreTasks"));
        }
    }

    public static bool stopGrenadierSkill = false;
    public static bool stopMadGrenadierSkill = false;
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (lowLoad) return;
        if (!GrenadierBlinding.ContainsKey(player.PlayerId) && !MadGrenadierBlinding.ContainsKey(player.PlayerId)) return;

        var nowStamp = GetTimeStamp();
        if (GrenadierBlinding.TryGetValue(player.PlayerId, out var grenadierTime) && grenadierTime + GrenadierSkillDuration.GetInt() < nowStamp)
        {
            GrenadierBlinding.Remove(player.PlayerId);
            stopGrenadierSkill = true;
        }
        if (MadGrenadierBlinding.TryGetValue(player.PlayerId, out var madGrenadierTime) && madGrenadierTime + GrenadierSkillDuration.GetInt() < nowStamp)
        {
            MadGrenadierBlinding.Remove(player.PlayerId);
            stopMadGrenadierSkill = true;
        }
        if (stopGrenadierSkill || stopMadGrenadierSkill)
        {
            if (!DisableShieldAnimations.GetBool())
            {
                player.RpcGuardAndKill();
            }
            else
            {
                player.RpcResetAbilityCooldown();
            }
            player.Notify(string.Format(GetString("AbilityExpired"), player.GetAbilityUseLimit()));
            MarkEveryoneDirtySettings();
            stopGrenadierSkill = false;
            stopMadGrenadierSkill = false;
        }
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.AbilityButton.buttonLabelText.text = GetString("GrenadierVentButtonText");
    }
}
