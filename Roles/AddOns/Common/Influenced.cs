using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

namespace TOHE.Roles.AddOns.Common
{
    public static class Influenced
    {
        private static readonly int Id = 21200;
        public static OptionItem CanBeOnCrew;
        public static OptionItem CanBeOnImp;
        public static OptionItem CanBeOnNeutral;

        public static void SetupCustomOption()
        {
            Options.SetupAdtRoleOptions(Id, CustomRoles.Influenced, canSetNum: true);
            CanBeOnImp = BooleanOptionItem.Create(Id + 10, "ImpCanBeInfluenced", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Influenced]);
            CanBeOnCrew = BooleanOptionItem.Create(Id + 11, "CrewCanBeInfluenced", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Influenced]);
            CanBeOnNeutral = BooleanOptionItem.Create(Id + 12, "NeutralCanBeInfluenced", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Influenced]);
        }
        public static void ChangeVotingData(Dictionary<byte, int> VotingData)
        { 
            //The incoming votedata does not count influenced votes
            List<byte> playerIdList = [];

            Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Influenced))
                .Do(x => playerIdList.Add(x.PlayerId));
            
            if (playerIdList.Count == 0) return;
            if (playerIdList.Count >= Main.AllAlivePlayerControls.Length) return;

            int max = 0;
            bool tie = false;
            byte exileId = byte.MaxValue;
            //var voteLog = Logger.Handler("Influenced check Vote");
            foreach (var data in VotingData)
            {
                //voteLog.Info($"{data.Key}({Utils.GetVoteName(data.Key)}):{data.Value}票");
                if (data.Value > max)
                {
                    //voteLog.Info(data.Key + "拥有更高票数(" + data.Value + ")");
                    exileId = data.Key;
                    max = data.Value;
                    tie = false;
                }
                else if (data.Value == max)
                {
                    //voteLog.Info(data.Key + "与" + exileId + "的票数相同(" + data.Value + ")");
                    exileId = byte.MaxValue;
                    tie = true;
                }
                //voteLog.Info($"驱逐ID: {exileId}, 最大: {max}票");
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
}
