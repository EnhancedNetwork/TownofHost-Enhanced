using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TOHE.Modules.ChatManager;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

public static class Councillor
{
    private static readonly int Id = 900;
    private static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    private static OptionItem MurderLimitPerMeeting;
  //  private static OptionItem MurderLimitPerGame;
    private static OptionItem TryHideMsg;
    private static OptionItem CanMurderMadmate;
    private static OptionItem CanMurderImpostor;
    public static OptionItem KillCooldown;
    private static Dictionary<byte, int> MurderLimit;
   // private static Dictionary<byte, int> MurderLimitGame;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Councillor);
            KillCooldown = FloatOptionItem.Create(Id + 15, "KillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Councillor])
                .SetValueFormat(OptionFormat.Seconds);
        MurderLimitPerMeeting = IntegerOptionItem.Create(Id + 10, "MurderLimitPerMeeting", new(1, 15, 1), 1, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Councillor])
            .SetValueFormat(OptionFormat.Times);
     //   MurderLimitPerGame = IntegerOptionItem.Create(Id + 13, "MurderLimitPerGame", new(1, 99, 1), 2, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Councillor])
       //     .SetValueFormat(OptionFormat.Times);
        CanMurderMadmate = BooleanOptionItem.Create(Id + 12, "CouncillorCanMurderMadmate", true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Councillor]);
        CanMurderImpostor = BooleanOptionItem.Create(Id + 16, "CouncillorCanMurderImpostor", true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Councillor]);
        TryHideMsg = BooleanOptionItem.Create(Id + 11, "CouncillorTryHideMsg", true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Councillor])
            .SetColor(Color.green);
    }
    public static void Init()
    {
        playerIdList = new();
        MurderLimit = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        MurderLimit.Add(playerId, MurderLimitPerMeeting.GetInt());
        IsEnable = true;
    }
    public static void OnReportDeadBody()
    {
        MurderLimit.Clear();
        foreach (var pc in playerIdList) MurderLimit.Add(pc, MurderLimitPerMeeting.GetInt());
    }
    public static bool MurderMsg(PlayerControl pc, string msg, bool isUI = false)
    {
        var originMsg = msg;

        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsInGame || pc == null) return false;
        if (!pc.Is(CustomRoles.Councillor)) return false;

        int operate = 0; // 1:ID 2:猜测
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (CheckCommond(ref msg, "id|guesslist|gl编号|玩家编号|玩家id|id列表|玩家列表|列表|所有id|全部id")) operate = 1;
        else if (CheckCommond(ref msg, "sp|jj|tl|Murder|审判|判|审", false)) operate = 2;
        else return false;

        if (!pc.IsAlive())
        {
            Utils.SendMessage(GetString("CouncillorDead"), pc.PlayerId);
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
                //if (Options.NewHideMsg.GetBool()) ChatManager.SendPreviousMessagesToAll();
                //else GuessManager.TryHideMsg();
                GuessManager.TryHideMsg();
                ChatManager.SendPreviousMessagesToAll();
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
                Logger.Info($"{pc.GetNameWithRole()} 审判了 {target.GetNameWithRole()}", "Councillor");
                bool CouncillorSuicide = true;
                if (MurderLimit[pc.PlayerId] < 1)
                {
                    if (!isUI) Utils.SendMessage(GetString("CouncillorMurderMax"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("CouncillorMurderMax"));
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
                    if (!isUI) Utils.SendMessage(GetString("LaughToWhoMurderSelf"), pc.PlayerId, Utils.ColorString(Color.cyan, GetString("MessageFromKPD")));
                    else pc.ShowPopUp(Utils.ColorString(Color.cyan, GetString("MessageFromKPD")) + "\n" + GetString("LaughToWhoMurderSelf"));
                    CouncillorSuicide = true;
                }
                if ((target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)) && Mini.Age < 18)
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessMini"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessMini"));
                    return true;
                }
                else if (target.Is(CustomRoles.Rebound))
                {
                    Logger.Info($"{pc.GetNameWithRole()} judged {target.GetNameWithRole()}, councillor sucide = true because target rebound", "CouncillorTrialMsg");
                    CouncillorSuicide = true;
                }
                else if (target.Is(CustomRoles.Madmate) && CanMurderMadmate.GetBool()) CouncillorSuicide = false;
                else if (target.Is(CustomRoles.Parasite) && CanMurderMadmate.GetBool()) CouncillorSuicide = false;
                else if (target.Is(CustomRoles.Refugee) && CanMurderMadmate.GetBool()) CouncillorSuicide = false;
                else if (target.Is(CustomRoles.Convict) && CanMurderMadmate.GetBool()) CouncillorSuicide = false;
                else if (target.Is(CustomRoles.Crewpostor) && CanMurderMadmate.GetBool()) CouncillorSuicide = false;
                else if (target.Is(CustomRoles.SuperStar)) CouncillorSuicide = true;
                else if (target.Is(CustomRoles.Pestilence)) CouncillorSuicide = true;
                else if (target.Is(CustomRoles.Marshall) && target.AllTasksCompleted()) CouncillorSuicide = true;
                else if (target.Is(CustomRoles.Snitch) && target.AllTasksCompleted()) CouncillorSuicide = true;
                else if (target.Is(CustomRoles.Guardian) && target.AllTasksCompleted()) CouncillorSuicide = true;
                else if (target.Is(CustomRoles.Merchant) && Merchant.IsBribedKiller(pc, target)) CouncillorSuicide = true;
                else if ((target.GetCustomRole().IsImpostor()) && CanMurderImpostor.GetBool()) CouncillorSuicide = false;
                else if (target.GetCustomRole().IsCrewmate()) CouncillorSuicide = false;
                else if (target.GetCustomRole().IsNeutral()) CouncillorSuicide = false;
                else CouncillorSuicide = true;

                var dp = CouncillorSuicide ? pc : target;
                target = dp;

                string Name = dp.GetRealName();

                MurderLimit[pc.PlayerId]--;

                if (!GameStates.IsProceeding)
                _ = new LateTask(() =>
                {
                    Main.PlayerStates[dp.PlayerId].deathReason = PlayerState.DeathReason.Trialed;
                    dp.SetRealKiller(pc);
                    GuessManager.RpcGuesserMurderPlayer(dp);

                    //死者检查
                    Utils.AfterPlayerDeathTasks(dp, true);

                    Utils.NotifyRoles(isForMeeting: false, NoCache: true);

                    _ = new LateTask(() => { Utils.SendMessage(string.Format(GetString("MurderKill"), Name), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Judge), GetString("MurderKillTitle"))); }, 0.6f, "Guess Msg");

                }, 0.2f, "Murder Kill");
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
            error = GetString("MurderHelp");
            return false;
        }

        //判断选择的玩家是否合理
        PlayerControl target = Utils.GetPlayerById(id);
        if (target == null || target.Data.IsDead)
        {
            error = GetString("MurderNull");
            return false;
        }

        error = string.Empty;
        return true;
    }
    public static void SetKillCooldown(byte id)
        {
            Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
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

    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.MeetingKill, SendOption.Reliable, -1);
        writer.Write(playerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        int PlayerId = reader.ReadByte();
        MurderMsg(pc, $"/tl {PlayerId}", true);
    }

    private static void CouncillorOnClick(byte playerId, MeetingHud __instance)
    {
        Logger.Msg($"Click: ID {playerId}", "Councillor UI");
        var pc = Utils.GetPlayerById(playerId);
        if (pc == null || !pc.IsAlive() || !GameStates.IsVoting) return;
        if (AmongUsClient.Instance.AmHost) MurderMsg(PlayerControl.LocalPlayer, $"/tl {playerId}", true);
        else SendRPC(playerId);
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class StartMeetingPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (PlayerControl.LocalPlayer.Is(CustomRoles.Councillor) && PlayerControl.LocalPlayer.IsAlive())
                CreateCouncillorButton(__instance);
        }
    }
    public static void CreateCouncillorButton(MeetingHud __instance)
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
            renderer.sprite = CustomButton.Get("MeetingKillButton");
            PassiveButton button = targetBox.GetComponent<PassiveButton>();
            button.OnClick.RemoveAllListeners();
            button.OnClick.AddListener((Action)(() => CouncillorOnClick(pva.TargetPlayerId, __instance)));
        }
    }
}