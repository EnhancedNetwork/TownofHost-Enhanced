using static TOHE.Options;

namespace TOHE.Roles.Impostor;

internal class Instigator : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 1700;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem AbilityLimit;
    private static OptionItem KillsPerAbilityUse;

    private static readonly IRandom rd = IRandom.Instance;

    private static Dictionary<int, int> AbilityUseCount = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Instigator);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(20f, 180f, 1f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Instigator])
            .SetValueFormat(OptionFormat.Seconds);
        AbilityLimit = IntegerOptionItem.Create(Id + 11, "InstigatorAbilityLimit", new(1, 15, 1), 3, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Instigator])
            .SetValueFormat(OptionFormat.Times);
        KillsPerAbilityUse = IntegerOptionItem.Create(Id + 12, "InstigatorKillsPerAbilityUse", new(1, 15, 1), 1, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Instigator])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        playerIdList.Clear();
        AbilityUseCount = [];
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        AbilityUseCount.Add(playerId, 0);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public override void OnPlayerExiled(PlayerControl instigator, GameData.PlayerInfo exiled)
    {
        if (exiled == null || !exiled.GetCustomRole().IsCrewmate()) return;

        foreach (var player in playerIdList)
        {
            if (AbilityUseCount[player] >= AbilityLimit.GetInt()) continue;

            var killer = Main.AllPlayerControls.FirstOrDefault(a => a.PlayerId == player);
            if (!killer.IsAlive()) continue;

            List<PlayerControl> killPotentials = [];
            var votedForExiled = MeetingHud.Instance.playerStates.Where(a => a.VotedFor == exiled.PlayerId && a.TargetPlayerId != exiled.PlayerId).ToArray();
            foreach (var playerVote in votedForExiled)
            {
                var crewPlayer = Main.AllPlayerControls.FirstOrDefault(a => a.PlayerId == playerVote.TargetPlayerId);
                if (crewPlayer == null || !crewPlayer.GetCustomRole().IsCrewmate()) continue;
                killPotentials.Add(crewPlayer);
            }

            if (killPotentials.Count == 0) break;

            List<byte> killPlayers = [];

            for (int i = 0; i < KillsPerAbilityUse.GetInt(); i++)
            {
                if (killPotentials.Count == 0) break;

                PlayerControl target = killPotentials[rd.Next(0, killPotentials.Count)];
                target.SetRealKiller(killer);
                killPlayers.Add(target.PlayerId);
                killPotentials.Remove(target);
            }

            CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Retribution, [.. killPlayers]);

            AbilityUseCount[player] += 1;
        }
    }
}
