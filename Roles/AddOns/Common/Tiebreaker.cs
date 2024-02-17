using System.Collections.Generic;

namespace TOHE.Roles.AddOns.Common;

public static class Tiebreaker
{
    private static readonly int Id = 20200;

    public static OptionItem ImpCanBeTiebreaker;
    public static OptionItem CrewCanBeTiebreaker;
    public static OptionItem NeutralCanBeTiebreaker;

    public static List<byte> VoteFor = [];

    public static void SetupCustomOptions()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Tiebreaker, canSetNum: true);
        ImpCanBeTiebreaker = BooleanOptionItem.Create(Id + 10, "ImpCanBeTiebreaker", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Tiebreaker]);
        CrewCanBeTiebreaker = BooleanOptionItem.Create(Id + 11, "CrewCanBeTiebreaker", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Tiebreaker]);
        NeutralCanBeTiebreaker = BooleanOptionItem.Create(Id + 12, "NeutralCanBeTiebreaker", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Tiebreaker]);
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