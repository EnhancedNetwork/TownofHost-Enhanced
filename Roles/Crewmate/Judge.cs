using Hazel;
using System;
using System.Text;
using System.Text.RegularExpressions;
using TOHE.Modules.ChatManager;
using TOHE.Roles.Core;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Judge : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 10700;



    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    public static OptionItem TrialLimitPerMeeting;
    private static OptionItem TrialLimitPerGame;
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
    private static OptionItem CanTrialNeutralA;

    private static readonly Dictionary<byte, int> TrialLimitMeeting = [];

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Judge);
        TrialLimitPerMeeting = IntegerOptionItem.Create(Id + 10, "JudgeTrialLimitPerMeeting", new(1, 30, 1), 1, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Judge])
            .SetValueFormat(OptionFormat.Times);
        TrialLimitPerGame = IntegerOptionItem.Create(Id + 25, "JudgeTrialLimitPerGame", new(1, 30, 1), 1, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Judge])
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
        CanTrialNeutralA = BooleanOptionItem.Create(Id + 22, "JudgeCanTrialNeutralA", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Judge]);
        TryHideMsg = BooleanOptionItem.Create(Id + 11, "JudgeTryHideMsg", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Judge])
            .SetColor(Color.green);
    }
    public override void Init()
    {

        TrialLimitMeeting.Clear();
    }
    public override void Add(byte playerId)
    {

        TrialLimitMeeting[playerId] = TrialLimitPerMeeting.GetInt();
        AbilityLimit = TrialLimitPerGame.GetInt();
    }
    public override void Remove(byte playerId)
    {

        TrialLimitMeeting.Remove(playerId);
    }
    public override void OnReportDeadBody(PlayerControl party, NetworkedPlayerInfo dinosaur)
    {
        foreach (var pid in TrialLimitMeeting.Keys)
        {
            TrialLimitMeeting[pid] = TrialLimitPerMeeting.GetInt();
        }
    }
    public bool TrialMsg(PlayerControl pc, string msg, bool isUI = false)
    {
        var originMsg = msg;

        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsMeeting || pc == null || GameStates.IsExilling) return false;
        if (!pc.Is(CustomRoles.Judge)) return false;

        int operate = 0;
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (CheckCommond(ref msg, "id|guesslist|gl编号|玩家编号|玩家id|id列表|玩家列表|列表|所有id|全部id||編號|玩家編號")) operate = 1;
        else if (CheckCommond(ref msg, "sp|jj|tl|trial|审判|判|审|審判|審", false)) operate = 2;
        else return false;

        if (!pc.IsAlive())
        {
            pc.ShowInfoMessage(isUI, GetString("JudgeDead"));
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
                //if (Options.NewHideMsg.GetBool()) ChatManager.SendPreviousMessagesToAll();
                //else GuessManager.TryHideMsg();
                GuessManager.TryHideMsg();
                ChatManager.SendPreviousMessagesToAll();
            }
            else if (pc.AmOwner) SendMessage(originMsg, 255, pc.GetRealName());

            if (!MsgToPlayerAndRole(msg, out byte targetId, out string error))
            {
                SendMessage(error, pc.PlayerId);
                return true;
            }
            var target = GetPlayerById(targetId);
            if (target != null)
            {
                Logger.Info($"{pc.GetNameWithRole()} try trial {target.GetNameWithRole()}", "Judge");
                bool judgeSuicide = true;
                if (TrialLimitMeeting[pc.PlayerId] < 1)
                {
                    pc.ShowInfoMessage(isUI, GetString("JudgeTrialMaxMeetingMsg"));
                    return true;
                }
                if (AbilityLimit < 1)
                {
                    pc.ShowInfoMessage(isUI, GetString("JudgeTrialMaxGameMsg"));
                }
                if (Jailer.IsTarget(target.PlayerId))
                {
                    pc.ShowInfoMessage(isUI, GetString("CanNotTrialJailed"), ColorString(GetRoleColor(CustomRoles.Jailer), GetString("JailerTitle")));
                    return true;
                }
                if (pc.PlayerId == target.PlayerId)
                {
                    pc.ShowInfoMessage(isUI, GetString("Judge_LaughToWhoTrialSelf"), ColorString(Color.cyan, GetString("MessageFromKPD")));
                    goto SkipToPerform;
                }
                if (target.Is(CustomRoles.NiceMini) && Mini.Age < 18)
                {
                    pc.ShowInfoMessage(isUI, GetString("GuessMini"));
                    return true;
                }
                if (target.Is(CustomRoles.PunchingBag))
                {
                    pc.ShowInfoMessage(isUI, GetString("EradicatePunchingBag"));
                    return true;
                }

                if (target.Is(CustomRoles.Rebound))
                {
                    Logger.Info($"{pc.GetNameWithRole()} judged {target.GetNameWithRole()}, judge sucide = true because target rebound", "JudgeTrialMsg");
                    judgeSuicide = true;
                }
                else if (target.Is(CustomRoles.Solsticer))
                {
                    pc.ShowInfoMessage(isUI, GetString("GuessSolsticer"));
                    return true;
                }
                else if (target.IsTransformedNeutralApocalypse()) judgeSuicide = true;
                else if (target.Is(CustomRoles.Trickster)) judgeSuicide = true;
                else if (Medic.IsProtected(target.PlayerId) && !Medic.GuesserIgnoreShield.GetBool())
                {
                    pc.ShowInfoMessage(isUI, GetString("GuessShielded"));
                    return true;
                }
                else if (Guardian.CannotBeKilled(target))
                {
                    pc.ShowInfoMessage(isUI, GetString("GuessGuardianTask"));
                    return true;
                }
                else if (pc.IsAnySubRole(x => x.IsConverted())) judgeSuicide = false;
                else if (target.Is(CustomRoles.Rascal)) judgeSuicide = false;
                else if ((target.Is(CustomRoles.Sidekick) || target.Is(CustomRoles.Recruit)) && CanTrialSidekick.GetBool()) judgeSuicide = false;
                else if ((target.GetCustomRole().IsMadmate() || target.Is(CustomRoles.Madmate)) && CanTrialMadmate.GetBool()) judgeSuicide = false;
                else if (target.Is(CustomRoles.Infected) && CanTrialInfected.GetBool()) judgeSuicide = false;
                else if (target.Is(CustomRoles.Contagious) && CanTrialContagious.GetBool()) judgeSuicide = false;
                else if (target.Is(CustomRoles.Charmed) && CanTrialCharmed.GetBool()) judgeSuicide = false;
                else if (target.GetCustomRole().IsCrewKiller() && CanTrialCrewKilling.GetBool()) judgeSuicide = false;
                else if (target.GetCustomRole().IsNK() && CanTrialNeutralK.GetBool()) judgeSuicide = false;
                else if (target.GetCustomRole().IsNB() && CanTrialNeutralB.GetBool()) judgeSuicide = false;
                else if (target.GetCustomRole().IsNE() && CanTrialNeutralE.GetBool()) judgeSuicide = false;
                else if (target.GetCustomRole().IsNC() && CanTrialNeutralC.GetBool()) judgeSuicide = false;
                else if (target.GetCustomRole().IsNA() && CanTrialNeutralA.GetBool()) judgeSuicide = false;
                else if (target.GetCustomRole().IsImpostor()) judgeSuicide = false;
                else
                {
                    Logger.Warn("Impossibe to reach here!", "JudgeTrial");
                    judgeSuicide = true;
                }

            SkipToPerform:
                var dp = judgeSuicide ? pc : target;
                target = dp;

                string Name = dp.GetRealName();

                TrialLimitMeeting[pc.PlayerId]--;
                AbilityLimit--;
                SendSkillRPC();

                if (!GameStates.IsProceeding)
                    _ = new LateTask(() =>
                    {
                        dp.SetDeathReason(PlayerState.DeathReason.Trialed);
                        dp.SetRealKiller(pc);
                        GuessManager.RpcGuesserMurderPlayer(dp);

                        Main.PlayersDiedInMeeting.Add(dp.PlayerId);
                        MurderPlayerPatch.AfterPlayerDeathTasks(pc, dp, true);

                        _ = new LateTask(() => { SendMessage(string.Format(GetString("Judge_TrialKill"), Name), 255, ColorString(GetRoleColor(CustomRoles.Judge), GetString("Judge_TrialKillTitle")), true); }, 0.6f, "Guess Msg");

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
            result += mc[i];
        }

        if (int.TryParse(result, out int num))
        {
            id = Convert.ToByte(num);
        }
        else
        {
            id = byte.MaxValue;
            error = GetString("Judge_TrialHelp");
            return false;
        }

        PlayerControl target = GetPlayerById(id);
        if (target == null || target.Data.IsDead)
        {
            error = GetString("Judge_TrialNull");
            return false;
        }

        error = string.Empty;
        return true;
    }
    public static bool CheckCommond(ref string msg, string command, bool exact = true)
    {
        var comList = command.Split('|');
        foreach (var comm in comList)
        {
            if (exact)
            {
                if (msg == "/" + comm) return true;
            }
            else
            {
                if (msg.StartsWith("/" + comm))
                {
                    msg = msg.Replace("/" + comm, string.Empty);
                    return true;
                }
            }
        }
        return false;
    }

    private static void SendRPC(byte targetId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.Judge, SendOption.Reliable);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC_Custom(MessageReader reader, PlayerControl pc)
    {
        byte targetId = reader.ReadByte();

        if (pc.GetRoleClass() is Judge judge)
        {
            judge.TrialMsg(pc, $"/tl {targetId}", true);
        }
    }

    private static void JudgeOnClick(byte targetId /*, MeetingHud __instance*/)
    {
        Logger.Msg($"Click: ID {targetId}", "Judge UI");
        var target = targetId.GetPlayer();
        if (target == null || !target.IsAlive() || !GameStates.IsVoting) return;
        if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.GetRoleClass() is Judge judge) judge.TrialMsg(PlayerControl.LocalPlayer, $"/tl {targetId}", true);
        else SendRPC(targetId);
    }

    public override string NotifyPlayerName(PlayerControl seer, PlayerControl target, string TargetPlayerName = "", bool IsForMeeting = false)
        => IsForMeeting && seer.IsAlive() && target.IsAlive() ? ColorString(GetRoleColor(CustomRoles.Judge), target.PlayerId.ToString()) + " " + TargetPlayerName : "";
    public override string PVANameText(PlayerVoteArea pva, PlayerControl seer, PlayerControl target)
        => seer.IsAlive() && target.IsAlive() ? ColorString(GetRoleColor(CustomRoles.Judge), target.PlayerId.ToString()) + " " + pva.NameText.text : "";
    public override string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        var taskState8 = Main.PlayerStates?[playerId].TaskState;
        Color TextColor8;
        var TaskCompleteColor16 = Color.green;
        var NonCompleteColor16 = Color.yellow;
        var NormalColor8 = taskState8.IsTaskFinished ? TaskCompleteColor16 : NonCompleteColor16;
        TextColor8 = comms ? Color.gray : NormalColor8;
        string Completed8 = comms ? "?" : $"{taskState8.CompletedTasksCount}";
        ProgressText.Append(ColorString(TextColor8, $"({Completed8}/{taskState8.AllTasksCount})" + " "));
        ProgressText.Append(ColorString((AbilityLimit > 0) ? GetRoleColor(CustomRoles.Judge).ShadeColor(0.25f) : Color.gray, $"({AbilityLimit})" ?? "Invalid"));
        return ProgressText.ToString();
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
            var pc = GetPlayerById(pva.TargetPlayerId);
            if (pc == null || !pc.IsAlive()) continue;

            GameObject template = pva.Buttons.transform.Find("CancelButton").gameObject;
            GameObject targetBox = UnityEngine.Object.Instantiate(template, pva.transform);
            targetBox.name = "ShootButton";
            targetBox.transform.localPosition = new Vector3(-0.35f, 0.03f, -1.31f);
            SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
            renderer.sprite = CustomButton.Get("JudgeIcon");
            PassiveButton button = targetBox.GetComponent<PassiveButton>();
            button.OnClick.RemoveAllListeners();
            button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => JudgeOnClick(pva.TargetPlayerId/*, __instance*/)));
        }
    }
}
