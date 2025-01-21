using AmongUs.GameOptions;
using TOHE.Roles.Core;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Laborer : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 32200;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Laborer);
    public override CustomRoles Role => CustomRoles.Laborer;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\

    private static OptionItem SkillLimitOpt;
    private static OptionItem SkillCooldown;
    private static OptionItem CanVent;
    private static OptionItem HasImpostorVision;

    private readonly HashSet<byte> ForLabor = [];
    private float CurrentKillCooldown;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Laborer);
        SkillCooldown = FloatOptionItem.Create(Id + 10, "LaborerSkillCooldown", new(2.5f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Laborer])
            .SetValueFormat(OptionFormat.Seconds);
        SkillLimitOpt = IntegerOptionItem.Create(Id + 11, "LaborerSkillLimit", new(1, 15, 1), 5, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Laborer])
            .SetValueFormat(OptionFormat.Times);
        CanVent = BooleanOptionItem.Create(Id + 12, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Laborer]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Laborer]);
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(SkillLimitOpt.GetInt());
        CurrentKillCooldown = SkillCooldown.GetFloat();

        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(id.GetPlayer()) ? CurrentKillCooldown : 300f;
    public override bool CanUseKillButton(PlayerControl pc) => pc.GetAbilityUseLimit() > 0;
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (ForLabor.Contains(target.PlayerId) || killer.GetAbilityUseLimit() <= 0) return false;

        ForLabor.Remove(target.PlayerId);
        ForLabor.Add(target.PlayerId);
        
        killer.RpcRemoveAbilityUse();
        killer.SetKillCooldown();

        if (!Options.DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
        target.RpcGuardAndKill(killer);

        return false;
    }
    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        if (!ForLabor.Contains(target.PlayerId)) return false;

        var Laborer = _Player;
        if (Laborer == null) return false;


        if (Laborer.CheckForInvalidMurdering(killer) && Laborer.RpcCheckAndMurder(killer, true))
        {
            target.RpcGuardAndKill(killer);
            Laborer.RpcMurderPlayer(target);
            ForLabor.Remove(target.PlayerId);
            return true;
        }

        if (killer.Is(CustomRoles.Pestilence))
        {
            Main.PlayerStates[Laborer.PlayerId].deathReason = PlayerState.DeathReason.PissedOff;
            killer.RpcMurderPlayer(Laborer);
            ForLabor.Remove(target.PlayerId);
            target.RpcGuardAndKill(killer);

            return true;
        }


        return false;
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));
        hud.KillButton.OverrideText(GetString("LaborerKillButtonText"));
    }
}
