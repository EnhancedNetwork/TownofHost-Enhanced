using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Core;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Deceiver : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Deceiver;
    private const int Id = 10500;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Deceiver);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    private static OptionItem DeceiverSkillCooldown;
    private static OptionItem DeceiverSkillLimitTimes;
    private static OptionItem DeceiverAbilityLost;

    private readonly HashSet<byte> notActiveList = [];
    private readonly HashSet<byte> clientList = [];

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Deceiver);
        DeceiverSkillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.AbilityCooldown, new(2.5f, 180f, 2.5f), 20f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Deceiver])
            .SetValueFormat(OptionFormat.Seconds);
        DeceiverSkillLimitTimes = IntegerOptionItem.Create(Id + 11, GeneralOption.SkillLimitTimes, new(1, 15, 1), 2, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Deceiver])
            .SetValueFormat(OptionFormat.Times);
        DeceiverAbilityLost = BooleanOptionItem.Create(Id + 12, "DeceiverAbilityLost", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Deceiver]);
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(DeceiverSkillLimitTimes.GetInt());

        CustomRoleManager.CheckDeadBodyOthers.Add(CheckDeadBody);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    public override bool CanUseKillButton(PlayerControl pc) => pc.GetAbilityUseLimit() > 0;
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(Utils.GetPlayerById(id)) ? DeceiverSkillCooldown.GetFloat() : 300f;
    private bool IsClient(byte playerId) => clientList.Contains(playerId);
    private bool CanBeClient(PlayerControl pc) => pc != null && pc.IsAlive() && !GameStates.IsMeeting && !IsClient(pc.PlayerId);

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        if (target.Is(CustomRoles.SerialKiller)) return false;

        if (!(CanBeClient(target) && killer.GetAbilityUseLimit() > 0)) return false;

        killer.RpcRemoveAbilityUse();

        if (target.Is(CustomRoles.KillingMachine))
        {
            Logger.Info("target is Killing Machine, ability used count reduced, but target will not die", "Deceiver");
            return false;
        }
        if (target.IsTransformedNeutralApocalypse())
        {
            Logger.Info("target is Transformed Neutral Apocalypse, ability used count reduced, but target will not die", "Deceiver");
            return false;
        }

        clientList.Add(target.PlayerId);

        notActiveList.Add(killer.PlayerId);
        killer.RpcGuardAndKill(killer);
        killer.SetKillCooldown();

        killer.RPCPlayCustomSound("Bet");

        Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target);

        Logger.Info($"Counterfeiters {killer.GetRealName()} sell counterfeits to {target.GetRealName()}", "Deceiver");
        return false;
    }
    public override bool CheckMurderOnOthersTarget(PlayerControl pc, PlayerControl _)
    {
        if (!IsClient(pc.PlayerId) || notActiveList.Contains(pc.PlayerId)) return false;

        var killer = _Player;
        var target = pc;
        if (killer == null) return false;

        if (target.GetCustomRole() is not CustomRoles.SerialKiller or CustomRoles.Pursuer or CustomRoles.Deputy or CustomRoles.Deceiver or CustomRoles.Poisoner)
        {
            target.SetDeathReason(PlayerState.DeathReason.Misfire);
            target.RpcMurderPlayer(target);
            target.SetRealKiller(killer);
        }

        Logger.Info($"The customer {target.GetRealName()} of {pc.GetRealName()}, a counterfeiter, commits suicide by using counterfeits", "Deceiver");
        return true;
    }
    private void CheckDeadBody(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        if (!IsClient(target.PlayerId)) return;

        clientList.Remove(target.PlayerId);
    }
    public override void OnReportDeadBody(PlayerControl rafaeu, NetworkedPlayerInfo dinosaurs)
    {
        notActiveList.Clear();
        foreach (var pc in clientList)
        {
            var target = Utils.GetPlayerById(pc);
            if (target == null || !target.IsAlive()) continue;
            var role = target.GetCustomRole();
            if (
                (role.IsCrewmate() && !role.IsCrewKiller()) ||
                (role.IsNeutral() && !role.IsNK())
                )
            {
                var killer = _Player;
                if (killer == null) continue;
                CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Misfire, target.PlayerId);
                target.SetRealKiller(Utils.GetPlayerById(pc));
                target.SetRealKiller(killer);
                if (DeceiverAbilityLost.GetBool())
                {
                    killer.SetAbilityUseLimit(0);
                }
                Logger.Info($"Deceiver: {killer.GetRealName()} deceived {target.GetRealName()} player without kill button", "Deceiver");
            }
        }
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));
        hud.KillButton.OverrideText(GetString("DeceiverButtonText"));
    }
}
