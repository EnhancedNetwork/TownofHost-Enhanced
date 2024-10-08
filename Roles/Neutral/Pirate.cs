using Hazel;
using InnerNet;
using System.Text.RegularExpressions;
using TOHE.Modules.ChatManager;
using TOHE.Roles.Core;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Translator;
using static TOHE.Utils;


namespace TOHE.Roles.Neutral;
internal class Pirate : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 15000;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Pirate);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralChaos;
    //==================================================================\\

    private static OptionItem SuccessfulDuelsToWin;
    private static OptionItem TryHideMsg;
    private static OptionItem DuelCooldown;

    private static readonly Dictionary<byte, bool> DuelDone = [];

    private static byte PirateTarget;
    private static int pirateChose, targetChose;
    public static int NumWin = 0;

    public override void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Pirate);
        DuelCooldown = FloatOptionItem.Create(Id + 12, "DuelCooldown", new(0f, 180f, 2.5f), 22.5f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Pirate])
                .SetValueFormat(OptionFormat.Seconds);
        TryHideMsg = BooleanOptionItem.Create(Id + 10, "PirateTryHideMsg", true, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Pirate])
            .SetColor(Color.green);
        SuccessfulDuelsToWin = IntegerOptionItem.Create(Id + 11, "SuccessfulDuelsToWin", new(1, 20, 1), 2, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Pirate])
            .SetValueFormat(OptionFormat.Times);
    }

    public override void Init()
    {
        PirateTarget = byte.MaxValue;
        DuelDone.Clear();
        pirateChose = -1;
        targetChose = -1;
        NumWin = 0;
    }
    public override void Add(byte playerId)
    {
        DuelDone[playerId] = false;
    }
    public override void OnMeetingHudStart(PlayerControl pc)
    {
        if (!HasEnabled || PirateTarget == byte.MaxValue) return;

        var tpc = GetPlayerById(PirateTarget);
        if (!tpc.IsAlive()) return;

        MeetingHudStartPatch.AddMsg(GetString("PirateMeetingMsg"), pc.PlayerId, ColorString(GetRoleColor(CustomRoles.Pirate), GetString("PirateTitle")));
        MeetingHudStartPatch.AddMsg(GetString("PirateTargetMeetingMsg"), tpc.PlayerId, ColorString(GetRoleColor(CustomRoles.Pirate), GetString("PirateTitle")));
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = DuelCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override string GetProgressText(byte playerId, bool comms)
            => ColorString(GetRoleColor(CustomRoles.Pirate).ShadeColor(0.25f), $"({NumWin}/{SuccessfulDuelsToWin.GetInt()})");
    
    public void SendRPC(int operate, byte target = byte.MaxValue, int points = -1)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
        writer.Write(operate);
        writer.Write(target);
        if (operate == 1)
        {
            writer.Write(points);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        int operate = reader.ReadInt32();
        byte target = reader.ReadByte();
        PirateTarget = target;
        if (operate == 1)
        {
            int points = reader.ReadInt32();
            NumWin = points;
        }
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.Is(CustomRoles.NiceMini) && Mini.Age < 18)
        {
            killer.Notify(ColorString(GetRoleColor(CustomRoles.Gangster), GetString("CantDuel")));
            return true;
        }

        if (target.Is(CustomRoles.Pestilence)) return true;
        if (PirateTarget != byte.MaxValue)
        {
            killer.Notify(GetString("PirateTargetAlreadyChosen"));
            return false;
        }
        Logger.Msg($"{killer.GetNameWithRole()} chose a target {target.GetNameWithRole()}", "Pirate");
        PirateTarget = target.PlayerId;
        SendRPC(operate: 0, target: target.PlayerId, points: -1);
        DuelDone[PirateTarget] = false;
        if (!Options.DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(killer);
        else killer.SetKillCooldown();
        return false;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("PirateDuelButtonText"));
    }

    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Challenge");

    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isMeeting = false)
        => isMeeting && target.PlayerId == PirateTarget ? ColorString(GetRoleColor(CustomRoles.Pirate), " ⦿") : string.Empty;

    public override void OnCheckForEndVoting(PlayerState.DeathReason deathReason, params byte[] exileIds)
    {
        if (_Player == null || PirateTarget == byte.MaxValue) return;
        
        var pirateId = _state.PlayerId;
        if (!DuelDone[pirateId]) return;

        var pirateTarget = PirateTarget.GetPlayer();
        if (DuelDone[PirateTarget])
        {
            if (targetChose == pirateChose)
            {
                NumWin++;
                if (pirateTarget.IsAlive())
                {
                    CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Pirate, PirateTarget);
                    pirateTarget.SetRealKiller(_Player);
                }
            }
        }
        else if (pirateTarget.IsAlive())
        {
            CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Pirate, PirateTarget);
            pirateTarget.SetRealKiller(_Player);
        }
    }
    public override void AfterMeetingTasks()
    {
        if (_Player == null) return;
        var pirateId = _state.PlayerId;

        if (NumWin >= SuccessfulDuelsToWin.GetInt())
        {
            NumWin = SuccessfulDuelsToWin.GetInt();
            if (!CustomWinnerHolder.CheckForConvertedWinner(pirateId))
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Pirate);
                CustomWinnerHolder.WinnerIds.Add(pirateId);
            }
        }

        DuelDone.Clear();
        PirateTarget = byte.MaxValue;

        SendRPC(operate: 1, target: byte.MaxValue, points: NumWin);
        foreach (byte playerId in Main.PlayerStates.Values.Where(x => x.MainRole == CustomRoles.Pirate).Select(x => x.PlayerId)) { DuelDone[playerId] = false; }
    }
    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        PirateTarget = byte.MaxValue;
        SendRPC(operate: 1, target: byte.MaxValue, points: NumWin);
    }
    public static bool DuelCheckMsg(PlayerControl pc, string msg, bool isUI = false)
    {
        var originMsg = msg;
        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsMeeting || pc == null || GameStates.IsExilling) return false;
        if (!pc.Is(CustomRoles.Pirate) && PirateTarget != pc.PlayerId) return false;


        msg = msg.ToLower().TrimStart().TrimEnd();
        bool operate = false;
        if (CheckCommond(ref msg, "duel|对决|决斗")) operate = true;
        else return false;

        if (!pc.IsAlive())
        {
            pc.ShowInfoMessage(isUI, GetString("PirateDead"));
            return true;
        }

        if (operate)
        {

            if (TryHideMsg.GetBool())
            {
                //if (Options.NewHideMsg.GetBool()) ChatManager.SendPreviousMessagesToAll();
                //else TryHideMsgForDuel();
                TryHideMsgForDuel();
                ChatManager.SendPreviousMessagesToAll();
            }
            else if (pc.AmOwner) SendMessage(originMsg, 255, pc.GetRealName());

            if (!MsgToPlayerAndRole(msg, out int rpsOption, out string error))
            {
                SendMessage(error, pc.PlayerId);
                return true;
            }

            Logger.Info($"{pc.GetNameWithRole()} selected {rpsOption}", "Pirate");

            if (DuelDone[pc.PlayerId])
            {
                _ = new LateTask(() =>
                {
                    pc.ShowInfoMessage(isUI, GetString("DuelAlreadyDone"));
                    Logger.Msg("Duel attempted more than once", "Pirate");
                }, 0.2f, "Pirate Duel Already Done");
                return true;
            }

            else
            {
                if (pc.Is(CustomRoles.Pirate))
                {
                    pirateChose = rpsOption;

                }
                else
                {
                    targetChose = rpsOption;
                }
                _ = new LateTask(() =>
                {
                    pc.ShowInfoMessage(isUI, string.Format(GetString("DuelDone"), rpsOption));
                }, 0.2f, "Pirate Duel Done");

                DuelDone[pc.PlayerId] = true;
                return true;

            }
        }
        return true;
    }


    private static bool MsgToPlayerAndRole(string msg, out int rpsOpt, out string error)
    {
        if (msg.StartsWith("/")) msg = msg.Replace("/", string.Empty);

        Regex r = new("\\d+");
        MatchCollection mc = r.Matches(msg);
        string result = string.Empty;
        for (int i = 0; i < mc.Count; i++)
        {
            result += mc[i];//匹配结果是完整的数字，此处可以不做拼接的
        }

        if (int.TryParse(result, out int num))
        {
            if (num < 0 || num > 2)
            {
                rpsOpt = -1;
                error = GetString("DuelHelp");
                return false;
            }
            else { rpsOpt = num; }
        }
        else
        {
            rpsOpt = -1;
            error = GetString("DuelHelp");
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool CheckCommond(ref string msg, string command)
    {
        var comList = command.Split('|');
        for (int i = 0; i < comList.Length; i++)
        {
            //if (exact)
            //{
            //    if (msg == "/" + comList[i]) return true;
            //}
            //else
            //{
            if (msg.StartsWith("/" + comList[i]))
            {
                msg = msg.Replace("/" + comList[i], string.Empty);
                return true;
            }
            //}
        }
        return false;
    }

    public static void TryHideMsgForDuel()
    {
        ChatUpdatePatch.DoBlockChat = true;
        List<CustomRoles> roles = CustomRolesHelper.AllRoles.Where(x => x is not CustomRoles.NotAssigned).ToList();
        var rd = IRandom.Instance;
        string msg;
        string[] command = ["duel", "rps", "决斗", "对决"];
        for (int i = 0; i < 20; i++)
        {
            msg = "/";
            if (rd.Next(1, 100) < 20)
            {
                msg += "id";
            }
            else
            {
                msg += command[rd.Next(0, command.Length - 1)];
                msg += " ";
                msg += rd.Next(0, 3).ToString();
            }
            var player = Main.AllAlivePlayerControls.RandomElement();
            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg);
            var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
            writer.StartMessage(-1);
            writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
                .Write(msg)
                .EndRpc();
            writer.EndMessage();
            writer.SendMessage();
        }
        ChatUpdatePatch.DoBlockChat = false;
    }


}
