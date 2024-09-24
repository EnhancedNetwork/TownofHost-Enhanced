﻿namespace TOHE.Roles.AddOns.Common;

public class Influenced : IAddon
{
    private const int Id = 21200;
    public AddonTypes Type => AddonTypes.Harmful;

    public void SetupCustomOption()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Influenced, canSetNum: true, teamSpawnOptions: true);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
    public static void ChangeVotingData(Dictionary<byte, int> VotingData)
    { 
        //The incoming votedata does not count influenced votes
        HashSet<byte> playerIdList = [];

        Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Influenced))
            .Do(x => playerIdList.Add(x.PlayerId));
        
        if (playerIdList.Count == 0) return;
        if (playerIdList.Count >= Main.AllAlivePlayerControls.Length) return;

        int max = 0;
        bool tie = false;
        byte exileId = byte.MaxValue;
        foreach (var data in VotingData)
        {
            if (data.Value > max)
            {
                exileId = data.Key;
                max = data.Value;
                tie = false;
            }
            else if (data.Value == max)
            {
                exileId = byte.MaxValue;
                tie = true;
            }
        }
        if (tie) return;

        foreach (var playerId in playerIdList)
        {
            PlayerVoteArea pva = CheckForEndVotingPatch.GetPlayerVoteArea(playerId);
            if (pva != null && pva.VotedFor != exileId)
            {
                pva.VotedFor = exileId;
                CheckForEndVotingPatch.ReturnChangedPva(pva);
                Logger.Info($"changed influenced {playerId} {pva.TargetPlayerId} vote target to {exileId}", "InfluencedChangeVote");
            }
        }
    }
}
