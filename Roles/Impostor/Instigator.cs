using TOHE.Modules;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

internal class Instigator : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Instigator;
    private const int Id = 1700;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem AbilityLimitt;
    private static OptionItem KillsPerAbilityUse;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Instigator);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(20f, 180f, 1f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Instigator])
            .SetValueFormat(OptionFormat.Seconds);
        AbilityLimitt = IntegerOptionItem.Create(Id + 11, "InstigatorAbilityLimit", new(1, 15, 1), 3, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Instigator])
            .SetValueFormat(OptionFormat.Times);
        KillsPerAbilityUse = IntegerOptionItem.Create(Id + 12, "InstigatorKillsPerAbilityUse", new(1, 15, 1), 1, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Instigator])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(AbilityLimitt.GetInt());
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public override void OnPlayerExiled(PlayerControl instigator, NetworkedPlayerInfo exiled)
    {
        if (exiled == null || !exiled.GetCustomRole().IsCrewmate()) return;

        if (instigator.GetAbilityUseLimit() <= 0) return;

        var killer = _Player;
        if (!killer.IsAlive()) return;

        List<PlayerControl> killPotentials = [];
        var votedForExiled = MeetingHud.Instance.playerStates.Where(a => a.VotedFor == exiled.PlayerId && a.TargetPlayerId != exiled.PlayerId).ToArray();
        foreach (var playerVote in votedForExiled)
        {
            var crewPlayer = Main.AllPlayerControls.FirstOrDefault(a => a.PlayerId == playerVote.TargetPlayerId);
            if (crewPlayer == null || !crewPlayer.GetCustomRole().IsCrewmate() || crewPlayer.IsAnySubRole(x => !x.IsCrewmateTeamV2()) || !crewPlayer.IsAlive()) continue;
            killPotentials.Add(crewPlayer);
        }

        if (killPotentials.Count == 0) return;

        List<byte> killPlayers = [];

        for (int i = 0; i < KillsPerAbilityUse.GetInt(); i++)
        {
            if (killPotentials.Count == 0) break;

            PlayerControl target = killPotentials.RandomElement();
            target.SetRealKiller(killer);
            killPlayers.Add(target.PlayerId);
            killPotentials.Remove(target);
        }

        CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Retribution, [.. killPlayers]);

        instigator.RpcRemoveAbilityUse();
    }
}
