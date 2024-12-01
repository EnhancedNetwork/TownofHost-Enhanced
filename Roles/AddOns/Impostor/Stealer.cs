using static TOHE.Options;

namespace TOHE.Roles.AddOns.Impostor;

public class Stealer : IAddon
{
    private const int Id = 23200;
    public AddonTypes Type => AddonTypes.Impostor;
    private static Dictionary<byte, int> killedplayers;

    private static OptionItem TicketsPerKill;
    private static OptionItem HideAdditionalVotes;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Stealer, canSetNum: true, tab: TabGroup.Addons);
        TicketsPerKill = FloatOptionItem.Create(Id + 3, "TicketsPerKill", new(0.1f, 10f, 0.1f), 0.5f, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Stealer]);
        HideAdditionalVotes = BooleanOptionItem.Create(Id + 4, "HideAdditionalVotes", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Stealer]);
    }
    public void Init()
    {
        killedplayers.Clear();
    }
    public void Add(byte playerId, bool gameIsLoading = true)
    {
        killedplayers[playerId] = 0;
    }
    public void Remove(byte playerId)
    {
        killedplayers.Remove(playerId);
    }
    public static int AddRealVotesNum(PlayerVoteArea ps)
    {
        return (int)(killedplayers[ps.TargetPlayerId] * TicketsPerKill.GetFloat());
    }
    public static void AddVisualVotes(PlayerVoteArea votedPlayer, ref List<MeetingHud.VoterState> statesList)
    {
        if (HideAdditionalVotes.GetBool()) return;

        var additionalVotes = (int)(killedplayers[votedPlayer.TargetPlayerId] * TicketsPerKill.GetFloat());

        for (var i = 0; i < additionalVotes; i++)
        {
            statesList.Add(new MeetingHud.VoterState()
            {
                VoterId = votedPlayer.TargetPlayerId,
                VotedForId = votedPlayer.VotedFor
            });
        }
    }
    public static void OnMurderPlayer(PlayerControl killer)
    {
        killedplayers[killer.PlayerId]++;
        killer.Notify(string.Format(Translator.GetString("StealerGetTicket"),
            (killedplayers[killer.PlayerId] * TicketsPerKill.GetFloat() + 1)
            .ToString("0.0#####")));
    }
}