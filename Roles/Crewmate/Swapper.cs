using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TOHE.Modules.ChatManager;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

public static class Swapper
{
    private static readonly int Id = 12400;
    public static bool IsEnable = false;
    public static OptionItem SwapMax;
    public static OptionItem CanSwapSelf;
    public static OptionItem CanStartMeeting;
    public static OptionItem TryHideMsg;
    public static List<byte> playerIdList = new();
    public static List<byte> Vote = new();
    public static List<byte> VoteTwo = new();
    public static Dictionary<byte, int> Swappermax = new();
    public static void SetupCustomOption()
    {   
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Swapper);
        SwapMax = IntegerOptionItem.Create(Id + 3, "SwapperMax", new(1, 999, 1), 3, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Swapper])
            .SetValueFormat(OptionFormat.Times);
        CanSwapSelf = BooleanOptionItem.Create(Id + 2, "CanSwapSelfVotes", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Swapper]);
        CanStartMeeting = BooleanOptionItem.Create(Id + 4, "JesterCanUseButton", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Swapper]);
        TryHideMsg = BooleanOptionItem.Create(Id + 5, "SwapperTryHideMsg", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Swapper]);
    }
    public static void Init()
    {
        playerIdList = new();
        IsEnable = false;
        Vote = new();
        VoteTwo = new();
        Swappermax = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;
        Swappermax.TryAdd(playerId, SwapMax.GetInt());
    }
    public static string GetSwappermax(byte playerId) => Utils.ColorString((Swappermax.TryGetValue(playerId, out var x) && x >= 1) ? Utils.GetRoleColor(CustomRoles.Swapper).ShadeColor(0.25f) : Color.gray, Swappermax.TryGetValue(playerId, out var changermax) ? $"({changermax})" : "Invalid");
    public static bool SwapMsg(PlayerControl pc, string msg, bool isUI = false)
    {
        var originMsg = msg;

        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsMeeting || pc == null || GameStates.IsExilling) return false;
        if (!pc.Is(CustomRoles.Swapper)) return false;

        int operate = 0;
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (CheckCommond(ref msg, "id|guesslist|gl编号|玩家编号|玩家id|id列表|玩家列表|列表|所有id|全部id")) operate = 1;
        else if (CheckCommond(ref msg, "sw|换票|换|swap|st", false)) operate = 2;
        else return false;

        if (!pc.IsAlive())
        {
            if (!isUI) Utils.SendMessage(GetString("SwapDead"), pc.PlayerId);
            pc.ShowPopUp(GetString("SwapDead"));
            return true;
        }

        if (operate == 1)
        {
            Utils.SendMessage(GuessManager.GetFormatString(), pc.PlayerId);
            return true;
        }
        else if (operate == 2)
        {
            if (TryHideMsg.GetBool())
            {
                GuessManager.TryHideMsg();
                ChatManager.SendPreviousMessagesToAll();
            }
            else if (pc.AmOwner && !isUI) Utils.SendMessage(originMsg, 255, pc.GetRealName());

            if (!MsgToPlayerAndRole(msg, out byte targetId, out string error))
            {
                Utils.SendMessage(error, pc.PlayerId);
                return true;
            }
            var target = Utils.GetPlayerById(targetId);
            if (target != null)
            {
                if (Swappermax[pc.PlayerId] <= 0)
                {
                    if (!isUI) Utils.SendMessage(GetString("SwapperTrialMax"), pc.PlayerId);
                    pc.ShowPopUp(GetString("SwapperTrialMax"));
                    return true;
                }

                var dp = target;
                target = dp;

                if (Vote.Count < 1 && !Vote.Contains(dp.PlayerId) && !VoteTwo.Contains(dp.PlayerId) && CanSwapSelf.GetBool()
                || Vote.Count < 1 && !Vote.Contains(dp.PlayerId) && !VoteTwo.Contains(dp.PlayerId) && dp != pc && !CanSwapSelf.GetBool())
                {
                    Vote.Add(dp.PlayerId);
                    if (!isUI) Utils.SendMessage(GetString("Swap1"), pc.PlayerId); 
                    else pc.ShowPopUp(GetString("Swap1"));
                    Logger.Info($"{pc.GetNameWithRole()} 选择 {target.GetNameWithRole()}", "Swapper");
                }
                else if (Vote.Count == 1 && VoteTwo.Count < 1 && !Vote.Contains(dp.PlayerId) && !VoteTwo.Contains(dp.PlayerId) && CanSwapSelf.GetBool()
                || Vote.Count == 1 && VoteTwo.Count < 1 && !Vote.Contains(dp.PlayerId) && !VoteTwo.Contains(dp.PlayerId) && dp != pc && !CanSwapSelf.GetBool())
                {
                    VoteTwo.Add(dp.PlayerId);
                    if (!isUI) Utils.SendMessage(GetString("Swap2"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("Swap2"));
                    Logger.Info($"{pc.GetNameWithRole()} 选择 {target.GetNameWithRole()}", "Swapper");
                }
                else if (Vote.Count >= 1 && Vote.Contains(dp.PlayerId))
                {
                    Vote.Remove(dp.PlayerId);
                    if (!isUI) Utils.SendMessage(GetString("CancelSwap1"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("CancelSwap1"));
                    Logger.Info($"{pc.GetNameWithRole()} 取消选择 {target.GetNameWithRole()}", "Swapper");
                 }
                 else if (VoteTwo.Contains(dp.PlayerId) && VoteTwo.Count >= 1)
                 {
                    VoteTwo.Remove(dp.PlayerId);
                    if (!isUI) Utils.SendMessage(GetString("CancelSwap2"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("CancelSwap2"));
                    Logger.Info($"{pc.GetNameWithRole()} 取消选择 {target.GetNameWithRole()}", "Swapper");
                }
                else if (pc == dp && !CanSwapSelf.GetBool())
                {
                    if (!isUI) Utils.SendMessage(GetString("CantSwapSelf"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("CantSwapSelf"));
                }
                _ = new LateTask(() =>
                {
                        if (Vote.Count > 0 && VoteTwo.Count > 0)
                        {
                        PlayerControl player1 = new();
                        PlayerControl player2 = new();
                        foreach (var swap1 in Vote)
                        {
                            player1.PlayerId = swap1;
                        }
                        foreach (var swap2 in Vote)
                        {
                            player2.PlayerId = swap2;
                        }
                        if (!isUI) Utils.SendMessage(string.Format(GetString("SwapVoteC"), player1.GetRealName(), player2.GetRealName()), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Swapper), GetString("SwapTitle")));
                        else pc.ShowPopUp(string.Format(GetString("SwapVoteC"), player1.GetRealName(), player2.GetRealName()));
                    }
                    Utils.NotifyRoles(isForMeeting: true, NoCache: true);
                }, 0.2f, "Players Was Swaped");
            }
        }
        return true;
    }
    private static bool MsgToPlayerAndRole(string msg, out byte id, out string error)
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
            id = Convert.ToByte(num);
        }
        else
        {
            //并不是玩家编号，判断是否颜色
            //byte color = GetColorFromMsg(msg);
            //好吧我不知道怎么取某位玩家的颜色，等会了的时候再来把这里补上
            id = byte.MaxValue;
            error = GetString("SwapHelp");
            return false;
        }

        //判断选择的玩家是否合理
        PlayerControl target = Utils.GetPlayerById(id);
        if (target == null || target.Data.IsDead)
        {
            error = GetString("SwapNull");
            return false;
        }

        error = string.Empty;
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
                    msg = msg.Replace("/" + comList[i], string.Empty);
                    return true;
                }
            }
        }
        return false;
    }
    public static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetSwapperVotes, SendOption.Reliable, -1);
        writer.Write(playerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        byte PlayerId = reader.ReadByte();
        SwapMsg(pc, $"/sw {PlayerId}");
    }
    private static void SwapperOnClick(byte playerId, MeetingHud __instance)
    {
        Logger.Msg($"Click: ID {playerId}", "Swapper UI");
        var pc = Utils.GetPlayerById(playerId);
        if (pc == null || !pc.IsAlive() || !GameStates.IsVoting) return;
        
        if (AmongUsClient.Instance.AmHost) SwapMsg(PlayerControl.LocalPlayer, $"/sw {playerId}", true);
        else SendRPC(playerId);
        if (PlayerControl.LocalPlayer.Is(CustomRoles.Swapper) && PlayerControl.LocalPlayer.IsAlive())
        {
            CreateSwapperButton(__instance);
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class StartMeetingPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (PlayerControl.LocalPlayer.Is(CustomRoles.Swapper) && PlayerControl.LocalPlayer.IsAlive())
                CreateSwapperButton(__instance);
        }
    }
    public static void CreateSwapperButton(MeetingHud __instance)
    {
        foreach (var pva in __instance.playerStates)
        {
            var pc = Utils.GetPlayerById(pva.TargetPlayerId);
            if (pc == null || !pc.IsAlive()) continue;

            GameObject template = pva.Buttons.transform.Find("CancelButton").gameObject;
            GameObject targetBox = UnityEngine.Object.Instantiate(template, pva.transform);
            targetBox.name = "ShootButton";
            targetBox.transform.localPosition = new Vector3(-0.35f, 0.03f, -1.31f);
            SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
            PassiveButton button = targetBox.GetComponent<PassiveButton>(); 
            if (pc.PlayerId == pva.TargetPlayerId && (Vote.Contains(pc.PlayerId) || VoteTwo.Contains(pc.PlayerId))) 
            {
                renderer.sprite = CustomButton.Get("SwapYes"); 
            } 
            else 
            {
                renderer.sprite = CustomButton.Get("SwapNo"); 
            }
            button.OnClick.RemoveAllListeners();
            button.OnClick.AddListener((Action)(() => SwapperOnClick(pva.TargetPlayerId, __instance)));
        }
    }
}