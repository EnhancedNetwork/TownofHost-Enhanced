using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Paranoia : IAddon
{
    public CustomRoles Role => CustomRoles.Paranoia;
    private const int Id = 22400;

    public static OptionItem CanBeImp;
    public static OptionItem CanBeCrew;
    public static OptionItem CanBeCov;
    public static OptionItem DualVotes;
    private static OptionItem HideAdditionalVotes;
    public AddonTypes Type => AddonTypes.Mixed;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Paranoia, canSetNum: true);
        CanBeImp = BooleanOptionItem.Create(Id + 10, "ImpCanBeParanoia", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Paranoia]);
        CanBeCrew = BooleanOptionItem.Create(Id + 11, "CrewCanBeParanoia", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Paranoia]);
        CanBeCov = BooleanOptionItem.Create(Id + 14, "CovenCanBeParanoia", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Paranoia]);
        DualVotes = BooleanOptionItem.Create(Id + 12, "DualVotes", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Paranoia]);
        HideAdditionalVotes = BooleanOptionItem.Create(Id + 13, "HideAdditionalVotes", false, TabGroup.Addons, false).SetParent(DualVotes);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }

    public static bool IsExistInGame(PlayerControl player) => player.Is(CustomRoles.Paranoia);

    public static void AddVisualVotes(PlayerVoteArea votedPlayer, ref List<MeetingHud.VoterState> statesList)
    {
        if (HideAdditionalVotes.GetBool()) return;

        statesList.Add(new MeetingHud.VoterState()
        {
            VoterId = votedPlayer.TargetPlayerId,
            VotedForId = votedPlayer.VotedFor
        });
    }
}

