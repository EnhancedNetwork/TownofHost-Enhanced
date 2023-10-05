using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TOHE.Modules.ChatManager;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

public static class Judge
{
    private static readonly int Id = 9300;
    private static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    public static OptionItem TrialLimitPerMeeting;
    private static OptionItem TryHideMsg;
    private static OptionItem CanTrialMadmate;
    private static OptionItem CanTrialCharmed;
    private static OptionItem CanTrialSidekick;
    private static OptionItem CanTrialInfected;
    private static OptionItem CanTrialContagious;
    private static OptionItem CanTrialCrewKilling;
    private static OptionItem CanTrialNeutralB;
    private static OptionItem CanTrialNeutralK;
    private static OptionItem CanTrialNeutralE;
    private static OptionItem CanTrialNeutralC;
    public static Dictionary<byte, int> TrialLimit;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Judge);
        TrialLimitPerMeeting = IntegerOptionItem.Create(Id + 10, "TrialLimitPerMeeting", new(1, 30, 1), 1, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Judge])
            .SetValueFormat(OptionFormat.Times);
        CanTrialMadmate = BooleanOptionItem.Create(Id + 12, "JudgeCanTrialMadmate", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Judge]);
        CanTrialCharmed = BooleanOptionItem.Create(Id + 16, "JudgeCanTrialCharmed", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Judge]);
        CanTrialSidekick = BooleanOptionItem.Create(Id + 19, "JudgeCanTrialSidekick", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Judge]);
        CanTrialInfected = BooleanOptionItem.Create(Id + 20, "JudgeCanTrialInfected", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Judge]);
        CanTrialContagious = BooleanOptionItem.Create(Id + 21, "JudgeCanTrialContagious", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Judge]);
        CanTrialCrewKilling = BooleanOptionItem.Create(Id + 13, "JudgeCanTrialnCrewKilling", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Judge]);
        CanTrialNeutralB = BooleanOptionItem.Create(Id + 14, "JudgeCanTrialNeutralB", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Judge]);
        CanTrialNeutralE = BooleanOptionItem.Create(Id + 17, "JudgeCanTrialNeutralE", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Judge]);
        CanTrialNeutralC = BooleanOptionItem.Create(Id + 18, "JudgeCanTrialNeutralC", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Judge]);
        CanTrialNeutralK = BooleanOptionItem.Create(Id + 15, "JudgeCanTrialNeutralK", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Judge]);
        TryHideMsg = BooleanOptionItem.Create(Id + 11, "JudgeTryHideMsg", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Judge])
            .SetColor(Color.green);
    }
    public static void Init()
    {
        playerIdList = new();
        TrialLimit = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        TrialLimit.Add(playerId, TrialLimitPerMeeting.GetInt());
        IsEnable = true;
    }
    public static void OnReportDeadBody()
    {
        TrialLimit.Clear();
        foreach (var pc in playerIdList) TrialLimit.Add(pc, TrialLimitPerMeeting.GetInt());
    }
    public static bool TrialMsg(PlayerControl pc, string msg, bool isUI = false)
    {
        var originMsg = msg;

        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsInGame || pc == null) return false;
        if (!pc.Is(CustomRoles.Judge)) return false;

        int operate = 0; // 1:ID 2:猜测
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (CheckCommond(ref msg, "id|guesslist|gl编号|玩家编号|玩家id|id列表|玩家列表|列表|所有id|全部id")) operate = 1;
        else if (CheckCommond(ref msg, "sp|jj|tl|trial|审判|判|审", false)) operate = 2;
        else return false;

        if (!pc.IsAlive())
        {
            Utils.SendMessage(GetString("JudgeDead"), pc.PlayerId);
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
                if (Options.NewHideMsg.GetBool()) ChatManager.SendPreviousMessagesToAll();
                else GuessManager.TryHideMsg();
            }
            else if (pc.AmOwner) Utils.SendMessage(originMsg, 255, pc.GetRealName());

            if (!MsgToPlayerAndRole(msg, out byte targetId, out string error))
            {
                Utils.SendMessage(error, pc.PlayerId);
                return true;
            }
            var target = Utils.GetPlayerById(targetId);
            if (target != null)
            {
                Logger.Info($"{pc.GetNameWithRole()} 审判了 {target.GetNameWithRole()}", "Judge");
                bool judgeSuicide = true;
                if (TrialLimit[pc.PlayerId] < 1)
                {
                    if (!isUI) Utils.SendMessage(GetString("JudgeTrialMax"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("JudgeTrialMax"));
                    return true;
                }
                if (Jailer.JailerTarget.ContainsValue(target.PlayerId))
                {
                    if (!isUI) Utils.SendMessage(GetString("CanNotTrialJailed"), pc.PlayerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jailer), GetString("JailerTitle")));
                    else pc.ShowPopUp(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jailer), GetString("JailerTitle")) + "\n" + GetString("CanNotTrialJailed"));
                    return true;
                }
                if (pc.PlayerId == target.PlayerId)
                {
                    if (!isUI) Utils.SendMessage(GetString("LaughToWhoTrialSelf"), pc.PlayerId, Utils.ColorString(Color.cyan, GetString("MessageFromKPD")));
                    else pc.ShowPopUp(Utils.ColorString(Color.cyan, GetString("MessageFromKPD")) + "\n" + GetString("LaughToWhoTrialSelf"));
                    judgeSuicide = true;
                }
                if ((target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)) && Mini.Age < 18)
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessMini"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessMini"));
                    return true;
                }
                else if (pc.Is(CustomRoles.Madmate)) judgeSuicide = false;
                else if (pc.Is(CustomRoles.Charmed)) judgeSuicide = false;
                else if (pc.Is(CustomRoles.Recruit)) judgeSuicide = false;
                else if (pc.Is(CustomRoles.Infected)) judgeSuicide = false;
                else if (pc.Is(CustomRoles.Contagious)) judgeSuicide = false;
                else if (target.Is(CustomRoles.Rascal)) judgeSuicide = false;
                else if (target.Is(CustomRoles.Pestilence)) judgeSuicide = true;
                else if (target.Is(CustomRoles.Trickster)) judgeSuicide = true;
                else if (target.Is(CustomRoles.Madmate) && CanTrialMadmate.GetBool()) judgeSuicide = false;
                else if (target.Is(CustomRoles.Charmed) && CanTrialCharmed.GetBool()) judgeSuicide = false;
                else if (target.GetCustomRole().IsCK() && CanTrialCrewKilling.GetBool()) judgeSuicide = false;
                else if (target.GetCustomRole().IsNK() && CanTrialNeutralK.GetBool()) judgeSuicide = false;
                else if (target.GetCustomRole().IsNB() && CanTrialNeutralB.GetBool()) judgeSuicide = false;
                else if (target.GetCustomRole().IsNE() && CanTrialNeutralE.GetBool()) judgeSuicide = false;
                else if (target.GetCustomRole().IsNC() && CanTrialNeutralC.GetBool()) judgeSuicide = false;
                else if (target.GetCustomRole().IsImpostor() && !target.Is(CustomRoles.Trickster)) judgeSuicide = false;
                else if (target.GetCustomRole().IsMadmate() && CanTrialMadmate.GetBool()) judgeSuicide = false;
                else judgeSuicide = true;

                var dp = judgeSuicide ? pc : target;
                target = dp;

                string Name = dp.GetRealName();

                TrialLimit[pc.PlayerId]--;

                if (!GameStates.IsProceeding)
                _ = new LateTask(() =>
                {
                    Main.PlayerStates[dp.PlayerId].deathReason = PlayerState.DeathReason.Trialed;
                    dp.SetRealKiller(pc);
                    GuessManager.RpcGuesserMurderPlayer(dp);

                    //死者检查
                    Utils.AfterPlayerDeathTasks(dp, true);

                    Utils.NotifyRoles(isForMeeting: false, NoCache: true);

                    _ = new LateTask(() => { Utils.SendMessage(string.Format(GetString("TrialKill"), Name), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Judge), GetString("TrialKillTitle"))); }, 0.6f, "Guess Msg");

                }, 0.2f, "Trial Kill");
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
            error = GetString("TrialHelp");
            return false;
        }

        //判断选择的玩家是否合理
        PlayerControl target = Utils.GetPlayerById(id);
        if (target == null || target.Data.IsDead)
        {
            error = GetString("TrialNull");
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

    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.Judge, SendOption.Reliable, -1);
        writer.Write(playerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        int PlayerId = reader.ReadByte();
        TrialMsg(pc, $"/tl {PlayerId}", true);
    }

    private static void JudgeOnClick(byte playerId, MeetingHud __instance)
    {
        Logger.Msg($"Click: ID {playerId}", "Judge UI");
        var pc = Utils.GetPlayerById(playerId);
        if (pc == null || !pc.IsAlive() || !GameStates.IsVoting) return;
        if (AmongUsClient.Instance.AmHost) TrialMsg(PlayerControl.LocalPlayer, $"/tl {playerId}", true);
        else SendRPC(playerId);
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class StartMeetingPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (PlayerControl.LocalPlayer.Is(CustomRoles.Judge) && PlayerControl.LocalPlayer.IsAlive())
                CreateJudgeButton(__instance);
        }
    }
    public static void CreateJudgeButton(MeetingHud __instance)
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
            renderer.sprite = CustomButton.Get("JudgeIcon");
            PassiveButton button = targetBox.GetComponent<PassiveButton>();
            button.OnClick.RemoveAllListeners();
            button.OnClick.AddListener((Action)(() => JudgeOnClick(pva.TargetPlayerId, __instance)));
        }
    }
}