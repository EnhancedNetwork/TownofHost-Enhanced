using System;
using TOHE.Modules.ChatManager;
using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Translator;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.Crewmate;

internal class Dictator : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 11600;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\
    public static OptionItem ChangeCommandToExpel;
    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Dictator);
        ChangeCommandToExpel = BooleanOptionItem.Create(Id + 10, "DictatorChangeCommandToExpel", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Dictator]);
    }

    public override void Init()
    {
        playerIdList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }

    public static bool CheckVotingForTarget(PlayerControl pc, PlayerVoteArea pva)
        => pc.Is(CustomRoles.Dictator) && pva.DidVote && pc.PlayerId != pva.VotedFor && pva.VotedFor < 253 && !pc.Data.IsDead;
    public bool ExilePlayer(PlayerControl pc,string msg)
    {
        if (!ChangeCommandToExpel.GetBool()) return false;
        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsMeeting || pc == null || GameStates.IsExilling) return false;
        if (!pc.IsAlive()) return false;
        if (!pc.Is(CustomRoles.Dictator)) return false;
        int operate = 0; // 1:ID 2:猜测
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (ChatManager.CheckCommond(ref msg, "id|guesslist|gl编号|玩家编号|玩家id|id列表|玩家列表|列表|所有id|全部id||編號|玩家編號")) operate = 1;
        else if (ChatManager.CheckCommond(ref msg, "exp|expel|独裁|獨裁", false)) operate = 2;
        else return false;
        List<MeetingHud.VoterState> statesList = [];
        MeetingHud.VoterState[] states;
        if (operate == 1)
        {
            Utils.SendMessage(GuessManager.GetFormatString(), pc.PlayerId);
           // GuessManager.TryHideMsg();
           // ChatManager.SendPreviousMessagesToAll();
            return true;
        }
        if (operate == 2)
        {
            if (msg.StartsWith("/")) msg = msg.Replace("/", string.Empty);
            var targetid = 0;
            if (int.TryParse(msg, out int num))
            {
                targetid = Convert.ToByte(num);
            }
            var target = Utils.GetPlayerById(targetid);
            if (target == pc) 
            {
                pc.ShowInfoMessage(false, GetString("DictatorExpelSelf"));
                return true;
            }
            if (!target.IsAlive())
            {
                return true;
            }
            if (target.Is(CustomRoles.Solsticer))
            {
                pc.ShowInfoMessage(false,GetString("ExpelSolsticer"));
                MeetingHud.Instance.RpcClearVoteDelay(pc.GetClientId());
                return true;
            }
            statesList.Add(new()
            {
                VoterId = pc.PlayerId,
                VotedForId = target.PlayerId
            });
            states = [.. statesList];
            var exiled = target.Data;
            var isBlackOut = AntiBlackout.BlackOutIsActive;
            CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Suicide, pc.PlayerId);
            ExileControllerWrapUpPatch.AntiBlackout_LastExiled = exiled;
            Main.LastVotedPlayerInfo = exiled;
            AntiBlackout.ExilePlayerId = exiled.PlayerId;
            if (AntiBlackout.BlackOutIsActive)
            {
                if (isBlackOut)
                    MeetingHud.Instance.AntiBlackRpcVotingComplete(states, exiled, false);
                else
                    MeetingHud.Instance.RpcVotingComplete(statesList.ToArray(), exiled, false);
                if (exiled != null)
                {
                    AntiBlackout.ShowExiledInfo = isBlackOut;
                    CheckForEndVotingPatch.ConfirmEjections(exiled, isBlackOut);
                    MeetingHud.Instance.RpcVotingComplete(statesList.ToArray(), null, true);
                    MeetingHud.Instance.RpcClose();
                }
            }
            else
            {
                MeetingHud.Instance.RpcVotingComplete(states, exiled, false);

                if (exiled != null)
                {
                    CheckForEndVotingPatch.ConfirmEjections(exiled);
                }
            }

            Logger.Info($"{target.GetNameWithRole()} expelled by Dictator", "Dictator");

            CheckForEndVotingPatch.CheckForDeathOnExile(PlayerState.DeathReason.Vote, target.PlayerId);

            Logger.Info("Dictatorial vote, forced closure of the meeting", "Special Phase");

            target.SetRealKiller(pc);

        }
        return true;
    }
    public override string PVANameText(PlayerVoteArea pva, PlayerControl seer, PlayerControl target)
    => seer.IsAlive() && target.IsAlive() ? ColorString(GetRoleColor(CustomRoles.Dictator), target.PlayerId.ToString()) + " " + pva.NameText.text : "";
    public override string NotifyPlayerName(PlayerControl seer, PlayerControl target, string TargetPlayerName = "", bool IsForMeeting = false) 
        => IsForMeeting && ChangeCommandToExpel.GetBool() ? ColorString(GetRoleColor(CustomRoles.Dictator), target.PlayerId.ToString()) + " " + TargetPlayerName : "";
}
