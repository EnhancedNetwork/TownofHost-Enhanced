namespace TOHE.Roles.AddOns.Common;

public class Tiebreaker : IAddon
{
    public CustomRoles Role => CustomRoles.Tiebreaker;
    private const int Id = 20200;
    public AddonTypes Type => AddonTypes.Helpful;

    public static readonly HashSet<byte> VoteFor = [];

    public void SetupCustomOption()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Tiebreaker, canSetNum: true, teamSpawnOptions: true);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
    public static void Clear()
    {
        VoteFor.Clear();
    }
    public static void CheckVote(PlayerControl target, PlayerVoteArea ps)
    {
        if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.Tiebreaker) && !VoteFor.Contains(target.PlayerId))
            VoteFor.Add(target.PlayerId);
    }
}
