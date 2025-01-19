namespace TOHE.Roles.Impostor;

internal class Vindicator : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Vindicator;
    private const int Id = 3800;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    private static OptionItem VindicatorAdditionalVote;
    private static OptionItem VindicatorHideVote;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Vindicator);
        VindicatorAdditionalVote = IntegerOptionItem.Create(Id + 2, "MayorAdditionalVote", new(1, 20, 1), 3, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Vindicator])
            .SetValueFormat(OptionFormat.Votes);
        VindicatorHideVote = BooleanOptionItem.Create(Id + 3, GeneralOption.HideAdditionalVotes, false, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Vindicator]);
    }
    public override void AddVisualVotes(PlayerVoteArea votedPlayer, ref List<MeetingHud.VoterState> statesList)
    {
        if (VindicatorHideVote.GetBool()) return;

        for (var i2 = 0; i2 < VindicatorAdditionalVote.GetInt(); i2++)
        {
            statesList.Add(new MeetingHud.VoterState()
            {
                VoterId = votedPlayer.TargetPlayerId,
                VotedForId = votedPlayer.VotedFor
            });
        }
    }
    public override int AddRealVotesNum(PlayerVoteArea PVA) => VindicatorAdditionalVote.GetInt();
}
