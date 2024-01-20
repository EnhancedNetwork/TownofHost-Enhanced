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
    public static List<byte> playerIdList = [];
    public static Dictionary<byte, byte> Vote = [];
    public static Dictionary<byte, byte> VoteTwo = [];
    public static Dictionary<byte, int> Swappermax = [];
    public static List<byte> ResultSent = [];
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
        playerIdList = [];
        IsEnable = false;
        Vote = [];
        VoteTwo = [];
        Swappermax = [];
        ResultSent = [];
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;
        Swappermax.TryAdd(playerId, SwapMax.GetInt());
    }
    public static void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        Swappermax.Remove(playerId);
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
        if (CheckCommond(ref msg, "id|guesslist|gl编号|玩家编号|玩家id|id列表|玩家列表|列表|所有id|全部id|編號|玩家編號")) operate = 1;
        else if (CheckCommond(ref msg, "sw|换票|换|換票|換|swap|st", false)) operate = 2;
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

            if (!MsgToPlayerAndRole(msg, out byte targetId, out string error) && targetId != 253)
            {
                Utils.SendMessage(error, pc.PlayerId);
                return true;
            }

            if (targetId == 253)
            {
                Vote.TryAdd(pc.PlayerId, 253);
                VoteTwo.TryAdd(pc.PlayerId, 253);
                Vote[pc.PlayerId] = 253;
                VoteTwo[pc.PlayerId] = 253;

                if (!isUI) Utils.SendMessage(GetString("CancelSwap"), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Swapper), GetString("SwapTitle")));
                return true;
            }

            var target = Utils.GetPlayerById(targetId);

            if (target != null)
            {
                if (Swappermax[pc.PlayerId] < 1)
                {
                    if (!isUI) Utils.SendMessage(GetString("SwapperTrialMax"), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Swapper), GetString("SwapTitle")));
                    pc.ShowPopUp(GetString("SwapperTrialMax"));
                    return true;
                }
                //Swapper skill limit is changed in after meeting task

                if (!Vote.ContainsKey(pc.PlayerId) || !VoteTwo.ContainsKey(pc.PlayerId) || !playerIdList.Contains(pc.PlayerId))
                {
                    if (!playerIdList.Contains(pc.PlayerId))
                        playerIdList.Add(pc.PlayerId);
                    Vote.TryAdd(pc.PlayerId, 253);
                    VoteTwo.TryAdd(pc.PlayerId, 253);
                    Vote[pc.PlayerId] = 253;
                    VoteTwo[pc.PlayerId] = 253;

                    if (!isUI) Utils.SendMessage(GetString("SwapNull"), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Swapper), GetString("SwapTitle")));
                    else pc.ShowPopUp(GetString("SwapNull"));

                    return true;
                }

                var dp = target;
                if (pc.PlayerId == dp.PlayerId && !CanSwapSelf.GetBool())
                {
                    if (!isUI) Utils.SendMessage(GetString("CantSwapSelf"), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Swapper), GetString("SwapTitle")));
                    else pc.ShowPopUp(GetString("CantSwapSelf"));
                    return true;
                }
                if (dp == null || !dp.IsAlive())
                {
                    if (!isUI) Utils.SendMessage(GetString("SwapNull"), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Swapper), GetString("SwapTitle")));
                    else pc.ShowPopUp(GetString("SwapNull"));
                    return true;
                }

                if (Vote[pc.PlayerId] != 253 && VoteTwo[pc.PlayerId] != 253)
                {
                    var target1 = Utils.GetPlayerById(Vote[pc.PlayerId]);
                    var target2 = Utils.GetPlayerById(VoteTwo[pc.PlayerId]);

                    if (target1 == null || target2 == null || !target1.IsAlive() || !target2.IsAlive())
                    {
                        Vote.TryAdd(pc.PlayerId, 253);
                        VoteTwo.TryAdd(pc.PlayerId, 253);
                        Vote[pc.PlayerId] = 253;
                        VoteTwo[pc.PlayerId] = 253;

                        if (!isUI) Utils.SendMessage(GetString("CancelSwapDueToTarget"), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Swapper), GetString("SwapTitle")));
                        else pc.ShowPopUp(GetString("CancelSwapDueToTarget"));
                    }
                    else Utils.SendMessage(string.Format(GetString("SwapperPreResult"), target1.GetRealName(), target2.GetRealName()), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Swapper), GetString("SwapTitle")));

                    return true;
                }
                else if (Vote[pc.PlayerId] == 253 && VoteTwo[pc.PlayerId] == 253)
                {
                    Vote[pc.PlayerId] = dp.PlayerId;
                    Utils.SendMessage(GetString("Swap1"), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Swapper), GetString("SwapTitle")));

                    return true;
                }
                else if (Vote[pc.PlayerId] != 253 && VoteTwo[pc.PlayerId] == 253)
                {
                    if (dp.PlayerId != Vote[pc.PlayerId])
                    {
                        VoteTwo[pc.PlayerId] = dp.PlayerId;
                        Utils.SendMessage(GetString("Swap2"), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Swapper), GetString("SwapTitle")));

                        var target1 = Utils.GetPlayerById(Vote[pc.PlayerId]);
                        var target2 = Utils.GetPlayerById(VoteTwo[pc.PlayerId]);

                        if (target1 == null || target2 == null || !target1.IsAlive() || !target2.IsAlive())
                        {
                            Vote.TryAdd(pc.PlayerId, 253);
                            VoteTwo.TryAdd(pc.PlayerId, 253);
                            Vote[pc.PlayerId] = 253;
                            VoteTwo[pc.PlayerId] = 253;

                            if (!isUI) Utils.SendMessage(GetString("CancelSwapDueToTarget"), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Swapper), GetString("SwapTitle")));
                            else pc.ShowPopUp(GetString("CancelSwapDueToTarget"));
                        }
                        else Utils.SendMessage(string.Format(GetString("SwapperPreResult"), target1.GetRealName(), target2.GetRealName()), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Swapper), GetString("SwapTitle")));
                    }
                    else Utils.SendMessage(GetString("Swap1=Swap2"), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Swapper), GetString("SwapTitle")));

                    return true;
                }
                else if (Vote[pc.PlayerId] == 253 && VoteTwo[pc.PlayerId] != 253) //How could this happen
                {
                    Vote.TryAdd(pc.PlayerId, 253);
                    VoteTwo.TryAdd(pc.PlayerId, 253);
                    Vote[pc.PlayerId] = 253;
                    VoteTwo[pc.PlayerId] = 253;

                    if (!isUI) Utils.SendMessage(GetString("CancelSwapDueToTarget"), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Swapper), GetString("SwapTitle")));
                    else pc.ShowPopUp(GetString("CancelSwapDueToTarget"));
                    return true;
                }
            }
            else
            {
                if (!isUI) Utils.SendMessage(GetString("SwapNull"), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Swapper), GetString("SwapTitle")));
                else pc.ShowPopUp(GetString("SwapNull"));
            }
        }
        return true;
    }
    public static void CheckSwapperTarget(byte deadid)
    {
        if (deadid == 253) return;
        foreach (var pid in playerIdList)
        {
            if (!Swapper.Vote.TryGetValue(pid, out var tid1) || !Swapper.VoteTwo.TryGetValue(pid, out var tid2)) continue;
            if (tid1 == deadid || tid2 == deadid)
            {
                Vote.TryAdd(pid, 253);
                VoteTwo.TryAdd(pid, 253);
                Vote[pid] = 253;
                VoteTwo[pid] = 253;

                Utils.SendMessage(GetString("CancelSwapDueToTarget"), pid, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Swapper), GetString("SwapTitle")));
            }
        }
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

        if (int.TryParse(result, out int num) && num <= byte.MaxValue)
        {
            id = Convert.ToByte(num);
        }
        else
        {
            //并不是玩家编号，判断是否颜色
            //byte color = GetColorFromMsg(msg);
            //好吧我不知道怎么取某位玩家的颜色，等会了的时候再来把这里补上
            id = byte.MinValue;
            error = GetString("SwapHelp");
            return false;
        }

        //判断选择的玩家是否合理
        PlayerControl target = Utils.GetPlayerById(id);
        if (target == null || !target.IsAlive())
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
                    msg = msg.Replace("/" + comList[i], string.Empty);
                    return true;
                }
            }
        }
        return false;
    }
    private static void SendSwapRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetSwapperVotes, SendOption.Reliable, -1);
        writer.Write(playerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void SendSkillRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetSwapperSkill, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(Swappermax[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveSwapRPC(MessageReader reader, PlayerControl pc)
    {
        byte PlayerId = reader.ReadByte();
        SwapMsg(pc, $"/sw {PlayerId}");
    }
    public static void ReceiveSkillRPC(MessageReader reader)
    {
        byte playerId = reader.ReadByte();
        int skillLimit = reader.ReadInt32();

        if (!Swappermax.ContainsKey(playerId))
            Swappermax.Add(playerId, skillLimit);
        else
            Swappermax[playerId] = skillLimit;
    }
    private static void SwapperOnClick(byte playerId, MeetingHud __instance)
    {
        Logger.Msg($"Click: ID {playerId}", "Swapper UI");
        var pc = Utils.GetPlayerById(playerId);
        if (pc == null || !pc.IsAlive() || !GameStates.IsVoting) return;
        
        if (AmongUsClient.Instance.AmHost) SwapMsg(PlayerControl.LocalPlayer, $"/sw {playerId}", true);
        else SendSwapRPC(playerId);

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

            if (AmongUsClient.Instance.AmHost)
            {
                Vote.Clear();
                VoteTwo.Clear();
                foreach (var pc in Main.AllAlivePlayerControls.ToArray())
                {
                    if (!pc.Is(CustomRoles.Swapper) || !pc.IsAlive()) continue;

                    if (!playerIdList.Contains(pc.PlayerId))
                        Add(pc.PlayerId);

                    Vote.Add(pc.PlayerId, 253);
                    VoteTwo.Add(pc.PlayerId, 253);

                    MeetingHudStartPatch.msgToSend.Add((GetString("SwapHelp"), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Swapper), GetString("SwapTitle"))));
                    SendSkillRPC(pc.PlayerId);
                    ResultSent = [];
                }
            }
        }
    }
    public static void CreateSwapperButton(MeetingHud __instance)
    {
        foreach (var pva in __instance.playerStates)
        {
            var pc = Utils.GetPlayerById(pva.TargetPlayerId);
            var local = PlayerControl.LocalPlayer;
            if (pc == null || !pc.IsAlive()) continue;

            GameObject template = pva.Buttons.transform.Find("CancelButton").gameObject;
            GameObject targetBox = UnityEngine.Object.Instantiate(template, pva.transform);
            targetBox.name = "ShootButton";
            targetBox.transform.localPosition = new Vector3(-0.35f, 0.03f, -1.31f);
            SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
            PassiveButton button = targetBox.GetComponent<PassiveButton>();
            //if (pc.PlayerId == pva.TargetPlayerId && (Vote[local.PlayerId] == pc.PlayerId || VoteTwo[local.PlayerId] == pc.PlayerId)) 
            //{
            //    renderer.sprite = CustomButton.Get("SwapYes"); 
            //}
            //else 
            //{
            //    renderer.sprite = CustomButton.Get("SwapNo"); 
            //}
            //Button state here doesnt work bcz vote and vote2 arent synced to clients
            renderer.sprite = CustomButton.Get("SwapNo");

            button.OnClick.RemoveAllListeners();
            button.OnClick.AddListener((Action)(() => SwapperOnClick(pva.TargetPlayerId, __instance)));
        }
    }
}