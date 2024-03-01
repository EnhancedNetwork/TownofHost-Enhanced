using Hazel;
using System.Collections.Generic;
using System.Linq;
using TOHE.Modules.ChatManager;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class President : RoleBase
{
    private static readonly int Id = 12300;
    public static bool On = false;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;

    public static Dictionary<byte, int> EndLimit = [];
    public static Dictionary<byte, int> RevealLimit = [];
    public static Dictionary<byte, bool> CheckPresidentReveal = [];


    public static OptionItem PresidentAbilityUses;
    public static OptionItem PresidentCanBeGuessedAfterRevealing;
    public static OptionItem HidePresidentEndCommand;
    public static OptionItem NeutralsSeePresident;
    public static OptionItem MadmatesSeePresident;
    public static OptionItem ImpsSeePresident;


    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.President);
        PresidentAbilityUses = IntegerOptionItem.Create(Id + 10, "PresidentAbilityUses", new(1, 20, 1), 1, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.President])
            .SetValueFormat(OptionFormat.Times);
        PresidentCanBeGuessedAfterRevealing = BooleanOptionItem.Create(Id + 11, "PresidentCanBeGuessedAfterRevealing", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.President]);
        NeutralsSeePresident = BooleanOptionItem.Create(Id + 12, "NeutralsSeePresident", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.President]);
        MadmatesSeePresident = BooleanOptionItem.Create(Id + 13, "MadmatesSeePresident", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.President]);
        ImpsSeePresident = BooleanOptionItem.Create(Id + 14, "ImpsSeePresident", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.President]);
        HidePresidentEndCommand = BooleanOptionItem.Create(Id + 15, "HidePresidentEndCommand", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.President]);
    }
    public override void Init()
    {
        CheckPresidentReveal = [];
        EndLimit = [];
        RevealLimit = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        CheckPresidentReveal.Add(playerId, false);
        EndLimit.Add(playerId, PresidentAbilityUses.GetInt());
        RevealLimit.Add(playerId, 1);
        On = true;
    }
    public override void Remove(byte playerId)
    {
        CheckPresidentReveal.Remove(playerId);
        EndLimit.Remove(playerId);
        RevealLimit.Remove(playerId);
    }
    public override string GetProgressText(byte PlayerId, bool comms) => Utils.ColorString(EndLimit[PlayerId] > 0 ? Utils.GetRoleColor(CustomRoles.President) : Color.gray, EndLimit.TryGetValue(PlayerId, out var endLimit) ? $"({endLimit})" : "Invalid");

    public static void TryHideMsgForPresident()
    {
        ChatUpdatePatch.DoBlockChat = true;
        //List<CustomRoles> roles = Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().Where(x => x is not CustomRoles.NotAssigned and not CustomRoles.KB_Normal).ToList();
        var rd = IRandom.Instance;
        string msg;
        for (int i = 0; i < 20; i++)
        {
            msg = "/";
            if (rd.Next(1, 100) < 20)
                msg += "finish";
            else
                msg += "reveal";
            var player = Main.AllAlivePlayerControls.ToArray()[rd.Next(0, Main.AllAlivePlayerControls.Length)];
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

    public static bool EndMsg(PlayerControl pc, string msg)
    {
        var originMsg = msg;

        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsMeeting || pc == null || GameStates.IsExilling) return false;
        if (!pc.Is(CustomRoles.President)) return false;

        int operate;
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (CheckCommond(ref msg, "finish|结束|结束会议|結束|結束會議")) operate = 1;
        else if (CheckCommond(ref msg, "reveal|展示")) operate = 2;
        else return false;

        if (!pc.IsAlive())
        {
            Utils.SendMessage(GetString("PresidentDead"), pc.PlayerId);
            return false;
        }

        else if (operate == 1)
        {

            if (HidePresidentEndCommand.GetBool())
            {
                //if (Options.NewHideMsg.GetBool()) ChatManager.SendPreviousMessagesToAll();
                //else TryHideMsgForPresident();
                TryHideMsgForPresident();
                ChatManager.SendPreviousMessagesToAll();
            }
            else if (pc.AmOwner) Utils.SendMessage(originMsg, 255, pc.GetRealName());

            if (EndLimit[pc.PlayerId] < 1)
            {
                Utils.SendMessage(GetString("PresidentEndMax"), pc.PlayerId);
                return true;
            }

            EndLimit[pc.PlayerId]--;

            foreach (var pva in MeetingHud.Instance.playerStates)
            {
                if (pva == null) continue;

                if (pva.VotedFor < 253)
                    MeetingHud.Instance.RpcClearVote(pva.TargetPlayerId);
            }
            List<MeetingHud.VoterState> statesList = [];
            MeetingHud.Instance.RpcVotingComplete(statesList.ToArray(), null, true);
            MeetingHud.Instance.RpcClose();
        }
        else if (operate == 2)
        {

            if (HidePresidentEndCommand.GetBool())
            {
                //if (Options.NewHideMsg.GetBool()) ChatManager.SendPreviousMessagesToAll();
                //else TryHideMsgForPresident();
                TryHideMsgForPresident();
                ChatManager.SendPreviousMessagesToAll();
            }
            else if (pc.AmOwner) Utils.SendMessage(originMsg, 255, pc.GetRealName());

            if (RevealLimit[pc.PlayerId] < 1)
            {
                Utils.SendMessage(GetString("PresidentRevealMax"), pc.PlayerId);
                return true;
            }

            RevealLimit[pc.PlayerId]--;
            CheckPresidentReveal[pc.PlayerId] = true;
            foreach (var tar in Main.AllAlivePlayerControls)
            {
                if (!MadmatesSeePresident.GetBool() && tar.Is(CustomRoles.Madmate) && tar != pc) continue;
                if (!NeutralsSeePresident.GetBool() && tar.GetCustomRole().IsNeutral()) continue;
                if (!ImpsSeePresident.GetBool() && (tar.GetCustomRole().IsImpostor() || tar.Is(CustomRoles.Crewpostor))) continue;
                Utils.SendMessage(string.Format(GetString("PresidentRevealed"), pc.GetRealName()), tar.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.President), GetString("PresidentRevealTitle")));
            }
            SendRPC(pc.PlayerId, isEnd: false);
        }
        return true;
    }
    public static bool CheckCommond(ref string msg, string command, bool exact = true)
    {
        var comList = command.Split('|');
        for (int i = 0; i < comList.Length; i++)
        {
            if (exact)
            {
                if (msg == "/" + comList[i]) return true;
            }
            else
            {
                if (msg.StartsWith("/" + comList[i]))
                {
                    //msg = msg.Replace("/" + comList[i], string.Empty);
                    return true;
                }
            }
        }
        return false;
    }
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (CheckPresidentReveal[target.PlayerId])
            killer.SetKillCooldown(0.9f);
        return true;
    }

    private static void SendRPC(byte playerId, bool isEnd = true)
    {
        MessageWriter writer;
        if (!isEnd)
        {
            writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PresidentReveal, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(CheckPresidentReveal[playerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            return;
        }
        writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PresidentEnd, SendOption.Reliable, -1);
        writer.Write(playerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader, PlayerControl pc, bool isEnd = true)
    {
        byte PlayerId = reader.ReadByte();
        if (!isEnd) 
        {
            bool revealed = reader.ReadBoolean();
            if (CheckPresidentReveal.ContainsKey(PlayerId)) CheckPresidentReveal[PlayerId] = revealed;
            else CheckPresidentReveal.Add(PlayerId, false);
            return;
        }
        EndMsg(pc, $"/finish");
    }
    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl guesser)
    {
        if ((target.Is(CustomRoles.President)) && CheckPresidentReveal[target.PlayerId] && !PresidentCanBeGuessedAfterRevealing.GetBool())
        {
            Utils.SendMessage(GetString("GuessPresident"), guesser.PlayerId);
            return true;
        }
        return false;
    }
    public override bool KnowRoletarget(PlayerControl seer, PlayerControl target)
    => ((target.Is(CustomRoles.President) && seer.GetCustomRole().IsCrewmate() && !seer.Is(CustomRoles.Madmate) && CheckPresidentReveal[target.PlayerId] == true) ||
                (target.Is(CustomRoles.President) && seer.Is(CustomRoles.Madmate) && MadmatesSeePresident.GetBool() && CheckPresidentReveal[target.PlayerId] == true) ||
                (target.Is(CustomRoles.President) && seer.GetCustomRole().IsNeutral() && NeutralsSeePresident.GetBool() && CheckPresidentReveal[target.PlayerId] == true) ||
                (target.Is(CustomRoles.President) && (seer.GetCustomRole().IsImpostorTeam()) && ImpsSeePresident.GetBool() && CheckPresidentReveal[target.PlayerId] == true));
    public override bool KnowTargetRoleColor(PlayerControl seer, PlayerControl target) => KnowRoletarget(seer, target);
}