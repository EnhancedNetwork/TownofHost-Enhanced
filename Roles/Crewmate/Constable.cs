using Hazel;
using TOHE.Modules;
using TOHE.Modules.ChatManager;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Constable : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Constable;
    private const int Id = 35400;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    private static OptionItem ConstableAbilityUses;
    private static OptionItem TribunalExtraVotes;
    private static bool IsTribunal = false;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Constable);
        ConstableAbilityUses = IntegerOptionItem.Create(Id + 10, GeneralOption.SkillLimitTimes, new(1, 5, 1), 2, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Constable])
            .SetValueFormat(OptionFormat.Times);
        TribunalExtraVotes = IntegerOptionItem.Create(Id + 11, "TribunalVotes354", new(0, 5, 1), 1, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Constable])
            .SetValueFormat(OptionFormat.Times);
        ConstableAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 12, "AbilityUseGainWithEachTaskCompleted", new(0f, 2f, 0.5f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Constable])
            .SetValueFormat(OptionFormat.Times);
        OverrideTasksData.Create(Id + 13, TabGroup.CrewmateRoles, CustomRoles.Constable);
    }

    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(ConstableAbilityUses.GetInt());
    }
    public override bool CheckVote(PlayerControl voter, PlayerControl target)
    {
        if (IsTribunal) return true;
        if (voter == target)
        {
            if (voter.GetAbilityUseLimit() < 1)
            {
                Utils.SendMessage(GetString("ConstableOutOfAbility"), voter.PlayerId);
                return true;
            }
            voter.RpcRemoveAbilityUse();

            IsTribunal = true;
            return false;
        }
        return true;
    }

    public static int ExtraVotes;
    public static int RealExtraVotes(PlayerVoteArea pc)
    {
        return ExtraVotes;
    }

    public override void AddVisualVotes(PlayerVoteArea votedPlayer, ref List<MeetingHud.VoterState> statesList)
    {
        for (var i2 = 0; i2 < ExtraVotes; i2++)
        {
            statesList.Add(new MeetingHud.VoterState()
            {
                VoterId = votedPlayer.TargetPlayerId,
                VotedForId = votedPlayer.VotedFor
            });
        }
    }

    public override void AfterMeetingTasks()
    {
        if (IsTribunal)
        {
            ExtraVotes = TribunalExtraVotes.GetInt();
            _Player.NoCheckStartMeeting(null, force: true);
            Utils.SendMessage(GetString("TribunalStart"), title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.Constable), GetString("TribunalTitle")));
            IsTribunal = false;
        }
        else ExtraVotes = 0;
    }
}
