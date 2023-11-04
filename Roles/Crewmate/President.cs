using Hazel;
using System.Collections.Generic;
using System.Linq;
using TOHE.Modules.ChatManager;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

public static class President
{
    private static readonly int Id = 7980;
    public static bool IsEnable = false;

    public static Dictionary<byte, int> EndLimit;
    public static Dictionary<byte, int> RevealLimit;
    public static Dictionary<byte, bool> CheckPresidentReveal = new();


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
    public static void Init()
    {
        CheckPresidentReveal = new();
        EndLimit = new();
        RevealLimit = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        CheckPresidentReveal.Add(playerId, false);
        EndLimit.Add(playerId, PresidentAbilityUses.GetInt());
        RevealLimit.Add(playerId, 1);
        IsEnable = true;
    }
    public static string GetEndLimit(byte playerId) => Utils.ColorString(EndLimit[playerId] > 0 ? Utils.GetRoleColor(CustomRoles.President) : Color.gray, EndLimit.TryGetValue(playerId, out var endLimit) ? $"({endLimit})" : "Invalid");

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
            var player = Main.AllAlivePlayerControls.ToArray()[rd.Next(0, Main.AllAlivePlayerControls.Count())];
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
        if (!GameStates.IsInGame || pc == null) return false;
        if (!pc.Is(CustomRoles.President)) return false;

        int operate = 0;
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (CheckCommond(ref msg, "finish")) operate = 1;
        else if (CheckCommond(ref msg, "reveal")) operate = 2;
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
        for (int i = 0; i < comList.Count(); i++)
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
}