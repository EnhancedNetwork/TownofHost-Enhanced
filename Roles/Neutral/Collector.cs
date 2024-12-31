using Hazel;
using InnerNet;
using TOHE.Roles.Core;

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
    private int CollectVote;
    //private int NewVote;

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
    private void SendRPC(/*byte playerId*/)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
        //writer.Write(playerId);
        writer.Write(CollectVote);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        //byte PlayerId = reader.ReadByte();
        int Num = reader.ReadInt32();
        CollectVote = Num;
    }
    public override string GetProgressText(byte playerId, bool cooms)
    {
        int VoteAmount = CollectVote;
        int CollectNum = CollectorCollectAmount.GetInt();
        return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Collector).ShadeColor(0.25f), $"({VoteAmount}/{CollectNum})");
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
    private bool CollectDone(PlayerControl player)
    {
        if (player.Is(CustomRoles.Collector))
        {
            //var pcid = player.PlayerId;
            int VoteAmount = CollectVote;
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
            PlayerControl pc = Utils.GetPlayerById(pva.TargetPlayerId);
            if (pc == null) continue;
            foreach (var data in VotingData)
            {
                if (CollectorVoteFor.ContainsKey(data.Key) && pc.PlayerId == CollectorVoteFor[data.Key] && pc.Is(CustomRoles.Collector))
                {
                    VoteAmount = data.Value;
                    CollectVote += VoteAmount;
                    SendRPC(/*pc.PlayerId*/);
                    Logger.Info($"{pc.GetNameWithRole().RemoveHtmlTags()}, collected {VoteAmount} votes from {Utils.GetPlayerById(data.Key).GetNameWithRole().RemoveHtmlTags()}", "Collected votes");
                }
            }
            Logger.Info($"Total amount of votes collected {CollectVote}", "Collector total amount");
        }
        calculated = true;
    }
}
