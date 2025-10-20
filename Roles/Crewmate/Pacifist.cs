using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Core;
using TOHE.Roles.Impostor;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Pacifist : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Pacifist;
    private const int Id = 9200;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Pacifist);
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateHindering;
    public override bool BlockMoveInVent(PlayerControl pc) => true;
    //==================================================================\\

    private static OptionItem PacifistCooldown;
    private static OptionItem PacifistMaxOfUseage;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Pacifist);
        PacifistCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.AbilityCooldown, new(1f, 180f, 1f), 30f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pacifist])
            .SetValueFormat(OptionFormat.Seconds);
        PacifistMaxOfUseage = IntegerOptionItem.Create(Id + 11, GeneralOption.SkillLimitTimes, new(0, 20, 1), 3, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pacifist])
            .SetValueFormat(OptionFormat.Times);
        PacifistAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(12, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pacifist])
            .SetValueFormat(OptionFormat.Times);
        OverrideTasksData.Create(Id + 13, TabGroup.CrewmateRoles, CustomRoles.Pacifist);
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(PacifistMaxOfUseage.GetInt());
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = PacifistCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        var abilityUse = pc.GetAbilityUseLimit();
        if (abilityUse < 1)
        {
            pc.Notify(GetString("OutOfAbilityUsesDoMoreTasks"));
        }
        else
        {
            pc.RpcRemoveAbilityUse();
            abilityUse--;

            if (!DisableShieldAnimations.GetBool()) pc.RpcGuardAndKill(pc);

            Main.AllAlivePlayerControls.Where(x =>
            pc.Is(CustomRoles.Madmate)
                ? (x.CanUseKillButton() && x.GetCustomRole().IsCrewmate())
                : x.CanUseKillButton()
            ).Do(x =>
            {
                x.RPCPlayCustomSound("Dove");
                x.ResetKillCooldown();
                x.SetKillCooldown();

                if (x.Is(CustomRoles.Mercenary))
                { Mercenary.ClearSuicideTimer(); }

                x.Notify(ColorString(GetRoleColor(CustomRoles.Pacifist), GetString("PacifistSkillNotify")));
            });
            pc.RPCPlayCustomSound("Dove");
            pc.Notify(string.Format(GetString("PacifistOnGuard"), abilityUse));
        }
    }
    public override bool CheckBootFromVent(PlayerPhysics physics, int ventId)
        => physics.myPlayer.GetAbilityUseLimit() < 1;

    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));
        hud.AbilityButton.buttonLabelText.text = GetString("PacifistVentButtonText");
    }
}
