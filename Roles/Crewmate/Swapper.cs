using Hazel;
using System;
using System.Text.RegularExpressions;
using TOHE.Modules;
using TOHE.Modules.ChatManager;
using TOHE.Roles.Core;
using TOHE.Roles.Coven;
using UnityEngine;
using static TOHE.CheckForEndVotingPatch;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Swapper : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Swapper;
    private const int Id = 12400;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Swapper);
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    private static OptionItem SwapMax;
    private static OptionItem CanSwapSelf;
    private static OptionItem OptCanStartMeeting;
    private static OptionItem TryHideMsg;

    private static readonly HashSet<byte> ResultSent = [];
    private static readonly Dictionary<byte, byte> Vote = [];
    private static readonly Dictionary<byte, byte> VoteTwo = [];

    private static List<byte> PlayerIdList => Main.PlayerStates.Values.Where(x => x.MainRole == CustomRoles.Swapper).Select(p => p.PlayerId).ToList();

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Swapper);
        SwapMax = IntegerOptionItem.Create(Id + 3, "SwapperMax", new(1, 999, 1), 3, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Swapper])
            .SetValueFormat(OptionFormat.Times);
        CanSwapSelf = BooleanOptionItem.Create(Id + 2, "CanSwapSelfVotes", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Swapper]);
        OptCanStartMeeting = BooleanOptionItem.Create(Id + 4, GeneralOption.CanUseMeetingButton, false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Swapper]);
        TryHideMsg = BooleanOptionItem.Create(Id + 5, "SwapperTryHideMsg", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Swapper]);
    }
    public override void Init()
    {
        Vote.Clear();
        VoteTwo.Clear();
        ResultSent.Clear();
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(SwapMax.GetInt());
    }
    public override bool OnCheckStartMeeting(PlayerControl reporter) => OptCanStartMeeting.GetBool();

    public override string NotifyPlayerName(PlayerControl seer, PlayerControl target, string TargetPlayerName = "", bool IsForMeeting = false)
        => IsForMeeting && seer.IsAlive() && target.IsAlive() ? ColorString(GetRoleColor(CustomRoles.Swapper), target.PlayerId.ToString()) + " " + TargetPlayerName : string.Empty;

    public bool SwapMsg(PlayerControl pc, string msg, bool isUI = false)
    {
        var originMsg = msg;

        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsMeeting || _Player == null || GameStates.IsExilling) return false;

        int operate;
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (CheckCommond(ref msg, "id|guesslist|gl编号|玩家编号|玩家id|id列表|玩家列表|列表|所有id|全部id|編號|玩家編號")) operate = 1;
        else if (CheckCommond(ref msg, "sw|换票|换|換票|換|swap|st", false)) operate = 2;
        else return false;

        if (!pc.IsAlive())
        {
            pc.ShowInfoMessage(isUI, GetString("SwapDead"));
            return true;
        }

        if (operate == 1)
        {
            SendMessage(GuessManager.GetFormatString(), pc.PlayerId);
            return true;
        }
        else if (operate == 2)
        {
            if (TryHideMsg.GetBool())
            {
                GuessManager.TryHideMsg();
                ChatManager.SendPreviousMessagesToAll();
            }
            else if (pc.AmOwner && !isUI) SendMessage(originMsg, 255, pc.GetRealName());

            if (!MsgToPlayerAndRole(msg, out byte targetId, out string error) && targetId != 253)
            {
                SendMessage(error, pc.PlayerId);
                return true;
            }

            if (targetId == 253)
            {
                Vote.TryAdd(pc.PlayerId, 253);
                VoteTwo.TryAdd(pc.PlayerId, 253);
                Vote[pc.PlayerId] = 253;
                VoteTwo[pc.PlayerId] = 253;

                pc.ShowInfoMessage(isUI, GetString("CancelSwap"), ColorString(GetRoleColor(CustomRoles.Swapper), GetString("Swapper").ToUpper()));
                return true;
            }

            var target = targetId.GetPlayer();

            if (target != null)
            {
                if (pc.GetAbilityUseLimit() <= 0)
                {
                    pc.ShowInfoMessage(isUI, GetString("SwapperTrialMax"), ColorString(GetRoleColor(CustomRoles.Swapper), GetString("Swapper").ToUpper()));
                    return true;
                }
                //Swapper skill limit is changed in after meeting task

                if (!Vote.ContainsKey(pc.PlayerId) || !VoteTwo.ContainsKey(pc.PlayerId))
                {
                    Vote.TryAdd(pc.PlayerId, 253);
                    VoteTwo.TryAdd(pc.PlayerId, 253);
                    Vote[pc.PlayerId] = 253;
                    VoteTwo[pc.PlayerId] = 253;

                    pc.ShowInfoMessage(isUI, GetString("SwapNull"), ColorString(GetRoleColor(CustomRoles.Swapper), GetString("Swapper").ToUpper()));

                    return true;
                }

                var dp = target;
                if (pc.PlayerId == dp.PlayerId && !CanSwapSelf.GetBool())
                {
                    pc.ShowInfoMessage(isUI, GetString("CantSwapSelf"), ColorString(GetRoleColor(CustomRoles.Swapper), GetString("Swapper").ToUpper()));
                    return true;
                }
                if (dp == null || !dp.IsAlive())
                {
                    pc.ShowInfoMessage(isUI, GetString("SwapNull"), ColorString(GetRoleColor(CustomRoles.Swapper), GetString("Swapper").ToUpper()));
                    return true;
                }

                if (Vote[pc.PlayerId] != 253 && VoteTwo[pc.PlayerId] != 253)
                {
                    var target1 = Vote[pc.PlayerId].GetPlayer();
                    var target2 = VoteTwo[pc.PlayerId].GetPlayer();

                    if (target1 == null || target2 == null || !target1.IsAlive() || !target2.IsAlive())
                    {
                        Vote.TryAdd(pc.PlayerId, 253);
                        VoteTwo.TryAdd(pc.PlayerId, 253);
                        Vote[pc.PlayerId] = 253;
                        VoteTwo[pc.PlayerId] = 253;

                        pc.ShowInfoMessage(isUI, GetString("CancelSwapDueToTarget"), ColorString(GetRoleColor(CustomRoles.Swapper), GetString("Swapper").ToUpper()));
                    }
                    else pc.ShowInfoMessage(isUI, string.Format(GetString("SwapperPreResult"), target1.GetRealName(), target2.GetRealName()), ColorString(GetRoleColor(CustomRoles.Swapper), GetString("Swapper").ToUpper()));

                    return true;
                }
                else if (Vote[pc.PlayerId] == 253 && VoteTwo[pc.PlayerId] == 253)
                {
                    Vote[pc.PlayerId] = dp.PlayerId;
                    pc.ShowInfoMessage(isUI, GetString("Swap1"), ColorString(GetRoleColor(CustomRoles.Swapper), GetString("Swapper").ToUpper()));

                    return true;
                }
                else if (Vote[pc.PlayerId] != 253 && VoteTwo[pc.PlayerId] == 253)
                {
                    if (dp.PlayerId != Vote[pc.PlayerId])
                    {
                        VoteTwo[pc.PlayerId] = dp.PlayerId;
                        pc.ShowInfoMessage(isUI, GetString("Swap2"), ColorString(GetRoleColor(CustomRoles.Swapper), GetString("Swapper").ToUpper()));

                        var target1 = Vote[pc.PlayerId].GetPlayer();
                        var target2 = VoteTwo[pc.PlayerId].GetPlayer();

                        if (target1 == null || target2 == null || !target1.IsAlive() || !target2.IsAlive())
                        {
                            Vote.TryAdd(pc.PlayerId, 253);
                            VoteTwo.TryAdd(pc.PlayerId, 253);
                            Vote[pc.PlayerId] = 253;
                            VoteTwo[pc.PlayerId] = 253;

                            pc.ShowInfoMessage(isUI, GetString("CancelSwapDueToTarget"), ColorString(GetRoleColor(CustomRoles.Swapper), GetString("Swapper").ToUpper()));
                        }
                        else pc.ShowInfoMessage(isUI, string.Format(GetString("SwapperPreResult"), target1.GetRealName(), target2.GetRealName()), ColorString(GetRoleColor(CustomRoles.Swapper), GetString("Swapper").ToUpper()));
                    }
                    else pc.ShowInfoMessage(isUI, GetString("Swap1=Swap2"), ColorString(GetRoleColor(CustomRoles.Swapper), GetString("Swapper").ToUpper()));

                    return true;
                }
                else if (Vote[pc.PlayerId] == 253 && VoteTwo[pc.PlayerId] != 253) //How could this happen
                {
                    Vote.TryAdd(pc.PlayerId, 253);
                    VoteTwo.TryAdd(pc.PlayerId, 253);
                    Vote[pc.PlayerId] = 253;
                    VoteTwo[pc.PlayerId] = 253;

                    pc.ShowInfoMessage(isUI, GetString("CancelSwapDueToTarget"), ColorString(GetRoleColor(CustomRoles.Swapper), GetString("Swapper").ToUpper()));
                    return true;
                }
            }
            else
            {
                pc.ShowInfoMessage(isUI, GetString("SwapNull"), ColorString(GetRoleColor(CustomRoles.Swapper), GetString("Swapper").ToUpper()));
            }
        }
        return true;
    }

    public static void CheckSwapperTarget(byte deadid)
    {
        if (deadid == 253) return;

        foreach (var pid in PlayerIdList)
        {
            if (!Vote.TryGetValue(pid, out var tid1) || !VoteTwo.TryGetValue(pid, out var tid2)) continue;
            if (tid1 == deadid || tid2 == deadid)
            {
                Vote.TryAdd(pid, 253);
                VoteTwo.TryAdd(pid, 253);
                Vote[pid] = 253;
                VoteTwo[pid] = 253;

                SendMessage(GetString("CancelSwapDueToTarget"), pid, ColorString(GetRoleColor(CustomRoles.Swapper), GetString("Swapper").ToUpper()));
            }
        }
    }

    public void SwapVotes(MeetingHud __instance)
    {
        var pid = _state.PlayerId;
        if (ResultSent.Contains(pid)) return;

        //idk why this would be triggered repeatedly.
        var pc = pid.GetPlayer();
        if (pc == null || !pc.IsAlive()) return;

        if (!Vote.TryGetValue(pc.PlayerId, out var tid1) || !VoteTwo.TryGetValue(pc.PlayerId, out var tid2)) return;
        if (tid1 == 253 || tid2 == 253 || tid1 == tid2) return;

        var target1 = tid1.GetPlayer();
        if (target1.Is(CustomRoles.VoodooMaster) && VoodooMaster.Dolls[target1.PlayerId].Count > 0)
        {
            target1 = VoodooMaster.Dolls[target1.PlayerId].Where(x => x.GetPlayer().IsAlive()).ToList().RandomElement().GetPlayer();
            SendMessage(string.Format(GetString("VoodooMasterTargetInMeeting"), target1.GetRealName()), Utils.GetPlayerListByRole(CustomRoles.VoodooMaster).First().PlayerId);
        }
        var target2 = tid2.GetPlayer();
        if (target2.Is(CustomRoles.VoodooMaster) && VoodooMaster.Dolls[target2.PlayerId].Count > 0)
        {
            target2 = VoodooMaster.Dolls[target2.PlayerId].Where(x => x.GetPlayer().IsAlive()).ToList().RandomElement().GetPlayer();
            SendMessage(string.Format(GetString("VoodooMasterTargetInMeeting"), target2.GetRealName()), Utils.GetPlayerListByRole(CustomRoles.VoodooMaster).First().PlayerId);
        }

        if (target1 == null || target2 == null || !target1.IsAlive() || !target2.IsAlive()) return;

        List<byte> templist = [];

        foreach (var pva in __instance.playerStates.ToArray())
        {
            if (pva.VotedFor != target1.PlayerId || pva.AmDead) continue;
            templist.Add(pva.TargetPlayerId);
            pva.VotedFor = target2.PlayerId;
            ReturnChangedPva(pva);
        }

        foreach (var pva in __instance.playerStates.ToArray())
        {
            if (pva.VotedFor != target2.PlayerId || pva.AmDead) continue;
            if (templist.Contains(pva.TargetPlayerId)) continue;
            pva.VotedFor = target1.PlayerId;
            ReturnChangedPva(pva);
        }

        if (!ResultSent.Contains(pid))
        {
            ResultSent.Add(pid);
            SendMessage(string.Format(GetString("SwapVote"), target1.GetRealName(), target2.GetRealName()), 255, ColorString(GetRoleColor(CustomRoles.Swapper), GetString("Swapper").ToUpper()));
            _Player.RpcRemoveAbilityUse();
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
        PlayerControl target = id.GetPlayer();
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
    public static void ReceiveSwapRPC(MessageReader reader, PlayerControl pc)
    {
        byte PlayerId = reader.ReadByte();
        if (pc.GetRoleClass() is Swapper sw) sw.SwapMsg(pc, $"/sw {PlayerId}", true);
    }
    private void SwapperOnClick(byte playerId, MeetingHud __instance)
    {
        Logger.Msg($"Click: ID {playerId}", "Swapper UI");
        var pc = playerId.GetPlayer();
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
            if (PlayerControl.LocalPlayer.GetRoleClass() is Swapper sp && PlayerControl.LocalPlayer.IsAlive())
                sp.CreateSwapperButton(__instance);

            if (AmongUsClient.Instance.AmHost)
            {
                Vote.Clear();
                VoteTwo.Clear();
                foreach (var pc in Main.AllAlivePlayerControls.ToArray())
                {
                    if (!pc.Is(CustomRoles.Swapper) || !pc.IsAlive()) continue;

                    Vote.Add(pc.PlayerId, 253);
                    VoteTwo.Add(pc.PlayerId, 253);

                    MeetingHudStartPatch.msgToSend.Add((GetString("SwapHelp"), pc.PlayerId, ColorString(GetRoleColor(CustomRoles.Swapper), GetString("Swapper").ToUpper())));

                    ResultSent.Clear();
                }
            }
        }
    }
    public void CreateSwapperButton(MeetingHud __instance)
    {
        foreach (var pva in __instance.playerStates)
        {
            if (pva.transform.Find("SwapButton") != null) UnityEngine.Object.Destroy(pva.transform.Find("SwapButton").gameObject);

            var pc = pva.TargetPlayerId.GetPlayer();
            var local = PlayerControl.LocalPlayer;
            if (pc == null || !pc.IsAlive()) continue;

            GameObject template = pva.Buttons.transform.Find("CancelButton").gameObject;
            GameObject targetBox = UnityEngine.Object.Instantiate(template, pva.transform);
            targetBox.name = "SwapButton";
            targetBox.transform.localPosition = new Vector3(-0.35f, 0.03f, -1.31f);
            SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
            PassiveButton button = targetBox.GetComponent<PassiveButton>();
            renderer.sprite = CustomButton.Get("SwapNo");

            button.OnClick.RemoveAllListeners();
            button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                SwapperOnClick(pva.TargetPlayerId, __instance);
            }));
        }
    }
}
