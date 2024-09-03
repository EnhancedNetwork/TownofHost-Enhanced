namespace TOHE.Roles.AddOns.Common;

public class Tiebreaker : IAddon
{
    private const int Id = 20200;
    public AddonTypes Type => AddonTypes.Helpful;

    public static List<byte> VoteFor = [];

    public void SetupCustomOption()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Tiebreaker, canSetNum: true, teamSpawnOptions: true);
    }

    public static void Clear()
    {
        VoteFor = [];
    }
    public static void CheckVote(PlayerControl target, PlayerVoteArea ps)
    {
        if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.Tiebreaker) && !VoteFor.Contains(target.PlayerId))
            VoteFor.Add(target.PlayerId);
    }
}