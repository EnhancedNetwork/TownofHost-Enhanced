using System.Text;
using TOHE.Modules;
using TOHE.Roles.Core;
using UnityEngine;

namespace TOHE.Roles.Neutral;

internal class Collector : RoleBase
{

    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Collector;
    private const int Id = 14700;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Collector);
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralChaos;
    //==================================================================\\

    private static OptionItem CollectorCollectAmount;

    private static readonly Dictionary<byte, byte> CollectorVoteFor = [];

    private bool calculated = false;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Collector);
        CollectorCollectAmount = IntegerOptionItem.Create(Id + 13, "CollectorCollectAmount", new(1, 100, 1), 20, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Collector])
            .SetValueFormat(OptionFormat.Votes);
    }
    public override void Init()
    {
        calculated = false;
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(0);
    }
    public override string GetProgressText(byte playerId, bool cooms)
    {
        var ProgressText = new StringBuilder();
        Color TextColor = Utils.GetRoleColor(CustomRoles.Collector).ShadeColor(0.25f);

        int VoteAmount = (int)playerId.GetAbilityUseLimit();
        int CollectNum = CollectorCollectAmount.GetInt();

        ProgressText.Append(Utils.ColorString(TextColor, $"({VoteAmount}/{CollectNum})"));
        return ProgressText.ToString();
    }
    public static void Clear()
    {
        CollectorVoteFor.Clear();
    }
    public bool CollectorWin(bool check = true)
    {
        if (_Player != null && _Player.IsAlive() && CollectDone(_Player))
        {
            bool isWinConverted = false;

            if (CustomWinnerHolder.CheckForConvertedWinner(_Player.PlayerId))
            {
                isWinConverted = true;
            }

            if (check) return true;

            if (!isWinConverted)
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Collector);
                CustomWinnerHolder.WinnerIds.Add(_Player.PlayerId);
            }
            return true;
        }
        return false;
    }
    private static bool CollectDone(PlayerControl player)
    {
        if (player.Is(CustomRoles.Collector))
        {
            int VoteAmount = (int)player.GetAbilityUseLimit();
            int CollectNum = CollectorCollectAmount.GetInt();
            if (VoteAmount >= CollectNum) return true;
        }
        return false;
    }
    public static void CollectorVotes(PlayerControl target, PlayerVoteArea ps)
    {
        if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.Collector))
            CollectorVoteFor.TryAdd(target.PlayerId, ps.TargetPlayerId);
    }
    public override void AfterMeetingTasks() => calculated = false;
    public void CollectAmount(Dictionary<byte, int> VotingData, MeetingHud __instance)
    {
        if (calculated) return;
        int VoteAmount;
        foreach (var pva in __instance.playerStates)
        {
            if (pva == null) continue;
            PlayerControl pc = pva.TargetPlayerId.GetPlayer();
            if (pc == null) continue;
            foreach (var data in VotingData)
            {
                if (CollectorVoteFor.ContainsKey(data.Key) && pc.PlayerId == CollectorVoteFor[data.Key] && pc.Is(CustomRoles.Collector))
                {
                    VoteAmount = data.Value;
                    pc.RpcIncreaseAbilityUseLimitBy(VoteAmount);
                    Logger.Info($"{pc.GetNameWithRole().RemoveHtmlTags()}, collected {VoteAmount} votes from {data.Key.GetPlayer().GetNameWithRole().RemoveHtmlTags()}", "Collected votes");
                }
            }
        }
        Logger.Info($"Total amount of votes collected {_Player?.GetAbilityUseLimit()}", "Collector");
        calculated = true;
    }
}
