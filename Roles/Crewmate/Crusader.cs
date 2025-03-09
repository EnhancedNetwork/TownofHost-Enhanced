using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Core;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Crusader : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Crusader;
    private const int Id = 10400;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Crusader);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    private static OptionItem SkillLimitOpt;
    private static OptionItem SkillCooldown;

    public readonly HashSet<byte> ForCrusade = [];
    private float CurrentKillCooldown;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Crusader);
        SkillCooldown = FloatOptionItem.Create(Id + 10, "CrusaderSkillCooldown", new(2.5f, 180f, 2.5f), 20f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Crusader])
            .SetValueFormat(OptionFormat.Seconds);
        SkillLimitOpt = IntegerOptionItem.Create(Id + 11, "CrusaderSkillLimit", new(1, 15, 1), 5, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Crusader])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(SkillLimitOpt.GetInt());
        CurrentKillCooldown = SkillCooldown.GetFloat();
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(id.GetPlayer()) ? CurrentKillCooldown : 300f;

    public override bool CanUseKillButton(PlayerControl pc) => pc.GetAbilityUseLimit() > 0;

    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);

    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (ForCrusade.Contains(target.PlayerId) || killer.GetAbilityUseLimit() <= 0) return false;

        ForCrusade.Remove(target.PlayerId);
        ForCrusade.Add(target.PlayerId);

        killer.RpcRemoveAbilityUse();
        killer.SetKillCooldown();

        if (!Options.DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
        target.RpcGuardAndKill(killer);

        return false;
    }
    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        if (!ForCrusade.Contains(target.PlayerId)) return false;

        var crusader = _Player; //this method is added by localplayer's ROLEBASE instance, so the player will always be the current crusader running the code.
        if (!crusader.IsAlive() || crusader.PlayerId == target.PlayerId) return false;

        var killerRole = killer.GetCustomRole();
        // Not should kill
        if (killerRole is CustomRoles.Taskinator
            or CustomRoles.Bodyguard
            or CustomRoles.Veteran
            or CustomRoles.Deputy)
            return false;

        if (crusader.CheckForInvalidMurdering(killer) && crusader.RpcCheckAndMurder(killer, true))
        {
            killer.RpcGuardAndKill(target);
            crusader.RpcMurderPlayer(killer);
            ForCrusade.Remove(target.PlayerId);
            return true;
        }

        if (killer.Is(CustomRoles.Pestilence))
        {
            crusader.SetDeathReason(PlayerState.DeathReason.PissedOff);
            killer.RpcMurderPlayer(crusader);
            ForCrusade.Remove(target.PlayerId);
            target.RpcGuardAndKill(killer);

            return true;
        }


        return false;
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));
        hud.KillButton.OverrideText(GetString("CrusaderKillButtonText"));
    }
}
