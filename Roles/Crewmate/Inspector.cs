using Hazel;
using System;
using System.Text;
using TOHE.Modules.ChatManager;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.Core;
using TOHE.Roles.Coven;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;
internal class Inspector : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 8300;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Inspector);

    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem TryHideMsg;
    private static OptionItem InspectCheckLimitMax;
    private static OptionItem InspectCheckLimitPerMeeting;
    private static OptionItem InspectCheckTargetKnow;
    private static OptionItem InspectCheckOtherTargetKnow;
    private static OptionItem InspectCheckBaitCountTypeOpt;
    private static OptionItem InspectCheckRevealTargetTeam;
    private static OptionItem InspectAbilityUseGainWithEachTaskCompleted;

    private static readonly Dictionary<byte, float> MaxCheckLimit = [];
    private static readonly Dictionary<byte, int> RoundCheckLimit = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Inspector);
        TryHideMsg = BooleanOptionItem.Create(Id + 10, "InspectorTryHideMsg", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Inspector])
            .SetColor(Color.green);
        InspectCheckLimitMax = IntegerOptionItem.Create(Id + 11, "MaxInspectCheckLimit", new(0, 20, 1), 5, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Inspector])
            .SetValueFormat(OptionFormat.Times);
        InspectCheckLimitPerMeeting = IntegerOptionItem.Create(Id + 12, "InspectCheckLimitPerMeeting", new(1, 20, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Inspector])
            .SetValueFormat(OptionFormat.Times);
        InspectCheckBaitCountTypeOpt = BooleanOptionItem.Create(Id + 14, "InspectCheckBaitCountMode", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Inspector]);
        InspectCheckTargetKnow = BooleanOptionItem.Create(Id + 15, "InspectCheckTargetKnow", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Inspector]);
        InspectCheckOtherTargetKnow = BooleanOptionItem.Create(Id + 16, "InspectCheckOtherTargetKnow", false, TabGroup.CrewmateRoles, false).SetParent(InspectCheckTargetKnow);
        InspectCheckRevealTargetTeam = BooleanOptionItem.Create(Id + 17, "InspectCheckRevealTarget", false, TabGroup.CrewmateRoles, false).SetParent(InspectCheckOtherTargetKnow);
        InspectAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 18, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Inspector])
            .SetValueFormat(OptionFormat.Times);
        OverrideTasksData.Create(Id + 20, TabGroup.CrewmateRoles, CustomRoles.Inspector);
    }
    public override void Init()
    {
        MaxCheckLimit.Clear();
        RoundCheckLimit.Clear();
    }

    public override void Add(byte playerId)
    {
        MaxCheckLimit.Add(playerId, InspectCheckLimitMax.GetInt());
        RoundCheckLimit.Add(playerId, InspectCheckLimitPerMeeting.GetInt());
    }
    public override void Remove(byte playerId)
    {
        MaxCheckLimit.Remove(playerId);
        RoundCheckLimit.Remove(playerId);
    }
    public static void SendRPC(byte playerId, int operate)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetInspectorLimit, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(operate);
        // reset round limit
        if (operate == 0) writer.Write(RoundCheckLimit[playerId]);
        // reduce the limits
        if (operate == 1)
        {
            writer.Write(RoundCheckLimit[playerId]);
            writer.Write(MaxCheckLimit[playerId]);
        }
        // increase limit
        if (operate == 3) writer.Write(MaxCheckLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte pid = reader.ReadByte();
        int operate = reader.ReadInt32();
        if (operate == 0)
        {
            int roundLimit = reader.ReadInt32();
            RoundCheckLimit[pid] = roundLimit;
        }
        if (operate == 1)
        {
            int roundLimit = reader.ReadInt32();
            float maxLimit = reader.ReadSingle();
            RoundCheckLimit[pid] = roundLimit;
            MaxCheckLimit[pid] = maxLimit;
        }
        if (operate == 2)
        {
            float maxLimit = reader.ReadSingle();
            MaxCheckLimit[pid] = maxLimit;
        }
    }

    public static bool CheckBaitCountType => InspectCheckBaitCountTypeOpt.GetBool();
    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (player.IsAlive())
        {
            MaxCheckLimit[player.PlayerId] += InspectAbilityUseGainWithEachTaskCompleted.GetFloat();
            SendRPC(player.PlayerId, 2);
        }
        return true;
    }
    public override void OnReportDeadBody(PlayerControl reported, NetworkedPlayerInfo target)
    {
        foreach (var pid in RoundCheckLimit.Keys)
        {
            RoundCheckLimit[pid] = InspectCheckLimitPerMeeting.GetInt();
            SendRPC(pid, 0);
        }
    }

    public static bool InspectCheckMsg(PlayerControl pc, string msg, bool isUI = false)
    {
        var originMsg = msg;

        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsMeeting || pc == null || GameStates.IsExilling) return false;
        if (!pc.Is(CustomRoles.Inspector)) return false;

        int operate = 0; // 1:ID 2:猜测
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (CheckCommond(ref msg, "id|guesslist|gl编号|玩家编号|玩家id|id列表|玩家列表|列表|所有id|全部id|編號|玩家編號")) operate = 1;
        else if (CheckCommond(ref msg, "compare|cmp|比较|比較", false)) operate = 2;
        else return false;

        if (!pc.IsAlive())
        {
            SendMessage(GetString("InspectorDead"), pc.PlayerId);
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
                //else TryHideMsgForCompare(); 
                TryHideMsgForCompare();
                ChatManager.SendPreviousMessagesToAll();
            }
            else if (pc.AmOwner) SendMessage(originMsg, 255, pc.GetRealName());

            if (!MsgToPlayerAndRole(msg, out byte targetId1, out byte targetId2, out string error))
            {
                SendMessage(error, pc.PlayerId);
                return true;
            }
            var target1 = GetPlayerById(targetId1);
            // Voodoo Master Check 1
            bool target1IsVM = false;
            if (target1.Is(CustomRoles.VoodooMaster) && VoodooMaster.Dolls[target1.PlayerId].Count > 0)
            {
                target1 = Utils.GetPlayerById(VoodooMaster.Dolls[target1.PlayerId].Where(x => Utils.GetPlayerById(x).IsAlive()).ToList().RandomElement());
                Utils.SendMessage(string.Format(GetString("VoodooMasterTargetInMeeting"), target1.GetRealName()), Utils.GetPlayerListByRole(CustomRoles.VoodooMaster).First().PlayerId);
                target1IsVM = true;
            }
            var target1Name = target1.GetRealName();
            if (target1IsVM) target1Name = Utils.GetPlayerListByRole(CustomRoles.VoodooMaster).First().GetRealName();

            var target2 = GetPlayerById(targetId2);
            // Voodoo Master Check 1
            bool target2IsVM = false;
            if (target2.Is(CustomRoles.VoodooMaster) && VoodooMaster.Dolls[target1.PlayerId].Count > 0)
            {
                target2 = Utils.GetPlayerById(VoodooMaster.Dolls[target2.PlayerId].Where(x => Utils.GetPlayerById(x).IsAlive()).ToList().RandomElement());
                Utils.SendMessage(string.Format(GetString("VoodooMasterTargetInMeeting"), target2.GetRealName()), Utils.GetPlayerListByRole(CustomRoles.VoodooMaster).First().PlayerId);
                target2IsVM = true;
            }
            var target2Name = target2.GetRealName();
            if (target2IsVM) target2Name = Utils.GetPlayerListByRole(CustomRoles.VoodooMaster).First().GetRealName();

            if (target1 != null && target2 != null)
            {
                Logger.Info($"{pc.GetNameWithRole()} checked {target1.GetNameWithRole()} and {target2.GetNameWithRole()}", "Inspector");

                if (MaxCheckLimit[pc.PlayerId] < 1 || RoundCheckLimit[pc.PlayerId] < 1)
                {
                    if (MaxCheckLimit[pc.PlayerId] < 1)
                    {
                        _ = new LateTask(() =>
                        {
                            pc.ShowInfoMessage(isUI, GetString("InspectCheckMax"));
                            Logger.Msg("Check attempted at max checks per game", "Inspector");
                        }, 0.2f, "Inspector Msg 1");
                    }
                    else
                    {
                        _ = new LateTask(() =>
                        {
                            pc.ShowInfoMessage(isUI, GetString("InspectCheckRound"));
                            Logger.Msg("Check attempted at max checks per meeting", "Inspector");
                        }, 0.2f, "Inspector Msg 2");
                    }
                    return true;
                }
                if (pc.PlayerId == target1.PlayerId || pc.PlayerId == target2.PlayerId)
                {
                    _ = new LateTask(() =>
                    {
                        pc.ShowInfoMessage(isUI, GetString("InspectCheckSelf"), ColorString(GetRoleColor(CustomRoles.Inspector), GetString("InspectCheckTitle")));
                        Logger.Msg("Check attempted on self", "Inspector");
                    }, 0.2f, "Inspector Msg 3");
                    return true;
                }
                else if (target1.GetCustomRole().IsRevealingRole(target1) || target1.IsAnySubRole(role => role.IsRevealingRole(target1)) || target2.GetCustomRole().IsRevealingRole(target2) || target2.IsAnySubRole(role => role.IsRevealingRole(target2)))
                {
                    _ = new LateTask(() =>
                    {
                        pc.ShowInfoMessage(isUI, GetString("InspectCheckReveal"), ColorString(GetRoleColor(CustomRoles.Inspector), GetString("InspectCheckTitle")));
                        Logger.Msg("Check attempted on revealed role", "Inspector");
                    }, 0.2f, "Inspector Msg 4");
                    return true;
                }
                else
                {
                    if
                    (
                        (
                        ((target1.IsPlayerCoven() || target1.Is(CustomRoles.Enchanted) || Illusionist.IsNonCovIllusioned(target1.PlayerId)))
                        && (target2.IsPlayerCoven() || target2.Is(CustomRoles.Enchanted) || Illusionist.IsNonCovIllusioned(target2.PlayerId))
                        )
                        ||
                        (
                        (Illusionist.IsCovIllusioned(target1.PlayerId) || (target1.GetCustomRole().IsCrewmateTeamV2() && (target1.GetCustomSubRoles().All(role => role.IsCrewmateTeamV2()) || target1.GetCustomSubRoles().Count == 0)) || target1.Is(CustomRoles.Admired))
                        &&
                        (Illusionist.IsCovIllusioned(target2.PlayerId) || (target2.GetCustomRole().IsCrewmateTeamV2() && (target2.GetCustomSubRoles().All(role => role.IsCrewmateTeamV2()) || target2.GetCustomSubRoles().Count == 0)) || target2.Is(CustomRoles.Admired))
                        )
                        ||
                        (
                        (target1.GetCustomRole().IsImpostorTeamV2() || target1.IsAnySubRole(role => role.IsImpostorTeamV2())) && !target1.Is(CustomRoles.Admired)
                        &&
                        (target2.GetCustomRole().IsImpostorTeamV2() || target2.IsAnySubRole(role => role.IsImpostorTeamV2()) && !target2.Is(CustomRoles.Admired))
                        )
                        ||
                        (
                        (target1.GetCustomRole().IsNeutralTeamV2() || target1.IsAnySubRole(role => role.IsNeutralTeamV2())) && !target1.Is(CustomRoles.Admired)
                        &&
                        (target2.GetCustomRole().IsNeutralTeamV2() || target2.IsAnySubRole(role => role.IsNeutralTeamV2())) && !target2.Is(CustomRoles.Admired)
                        )
                    )
                    {
                        _ = new LateTask(() =>
                        {
                            pc.ShowInfoMessage(isUI, string.Format(GetString("InspectCheckTrue"), target1.GetRealName(), target2.GetRealName()), ColorString(GetRoleColor(CustomRoles.Inspector), GetString("InspectCheckTitle")));
                            Logger.Msg("Check attempt, result TRUE", "Inspector");
                        }, 0.2f, "Inspector Msg 5");
                    }
                    else
                    {
                        _ = new LateTask(() =>
                        {
                            pc.ShowInfoMessage(isUI, string.Format(GetString("InspectCheckFalse"), target1Name, target2Name), ColorString(GetRoleColor(CustomRoles.Inspector), GetString("InspectCheckTitle")));
                            Logger.Msg("Check attempt, result FALSE", "Inspector");
                        }, 0.2f, "Inspector Msg 6");
                    }

                    if (InspectCheckTargetKnow.GetBool())
                    {
                        string textToSend = $"{target1Name}";
                        if (InspectCheckOtherTargetKnow.GetBool())
                            textToSend += $" and {target2Name}";
                        textToSend += GetString("InspectCheckTargetMsg");

                        string textToSend1 = $"{target2Name}";
                        if (InspectCheckOtherTargetKnow.GetBool())
                            textToSend1 += $" and {target1Name}";
                        textToSend1 += GetString("InspectCheckTargetMsg");
                        _ = new LateTask(() =>
                        {
                            SendMessage(textToSend, target1.PlayerId, ColorString(GetRoleColor(CustomRoles.Inspector), GetString("InspectCheckTitle")));
                            SendMessage(textToSend1, target2.PlayerId, ColorString(GetRoleColor(CustomRoles.Inspector), GetString("InspectCheckTitle")));
                            Logger.Msg("Check attempt, target1 notified", "Inspector");
                            Logger.Msg("Check attempt, target2 notified", "Inspector");
                        }, 0.2f, "Inspector Msg 7");

                        if (InspectCheckRevealTargetTeam.GetBool() && pc.AllTasksCompleted())
                        {
                            string roleT1 = "", roleT2 = "";
                            if (target1.Is(CustomRoles.Admired)) roleT1 = "Crewmate";
                            else if (target1.GetCustomRole().IsImpostorTeamV2() || target1.IsAnySubRole(role => role.IsImpostorTeamV2())) roleT1 = "Impostor";
                            else if (target1.GetCustomRole().IsNeutralTeamV2() || target1.IsAnySubRole(role => role.IsNeutralTeamV2())) roleT1 = "Neutral";
                            else if (target1.GetCustomRole().IsCrewmateTeamV2() && (target1.GetCustomSubRoles().Any(role => role.IsCrewmateTeamV2()) || (target1.GetCustomSubRoles().Count == 0))) roleT1 = "Crewmate";

                            if (target2.Is(CustomRoles.Admired)) roleT2 = "Crewmate";
                            else if (target2.GetCustomRole().IsImpostorTeamV2() || target2.IsAnySubRole(role => role.IsImpostorTeamV2())) roleT2 = "Impostor";
                            else if (target2.GetCustomRole().IsNeutralTeamV2() || target2.IsAnySubRole(role => role.IsNeutralTeamV2())) roleT2 = "Neutral";
                            else if ((target2.GetCustomRole().IsCrewmateTeamV2() && (target2.GetCustomSubRoles().Any(role => role.IsCrewmateTeamV2()) || target2.GetCustomSubRoles().Count == 0))) roleT2 = "Crewmate";

                            _ = new LateTask(() =>
                            {
                                SendMessage(string.Format(GetString("InspectorTargetReveal"), target2Name, roleT2), target1.PlayerId, ColorString(GetRoleColor(CustomRoles.Inspector), GetString("InspectCheckTitle")));
                                SendMessage(string.Format(GetString("InspectorTargetReveal"), target1Name, roleT1), target2.PlayerId, ColorString(GetRoleColor(CustomRoles.Inspector), GetString("InspectCheckTitle")));
                                Logger.Msg($"check attempt, target1 notified target2 as {roleT2} and target2 notified target1 as {roleT1}", "Inspector");
                            }, 0.3f, "Inspector Msg 8");
                        }
                    }
                    else
                    {
                        if (target1.Is(CustomRoles.Aware))
                        {
                            Aware.AwareInteracted[target1.PlayerId].Add(GetRoleName(CustomRoles.Inspector));
                        }
                        if (target2.Is(CustomRoles.Aware))
                        {
                            Aware.AwareInteracted[target2.PlayerId].Add(GetRoleName(CustomRoles.Inspector));
                        }
                    }
                    MaxCheckLimit[pc.PlayerId] -= 1;
                    RoundCheckLimit[pc.PlayerId]--;
                    SendRPC(pc.PlayerId, 1);
                }
            }
        }
        return true;
    }

    private static bool MsgToPlayerAndRole(string msg, out byte id1, out byte id2, out string error)
    {
        if (msg.StartsWith("/")) msg = msg.Replace("/", string.Empty);
        msg = msg.TrimStart().TrimEnd();
        Logger.Msg(msg, "Inspector");

        string[] nums = msg.Split(" ");
        if (nums.Length != 2)
        {
            Logger.Msg($"nums length is {nums.Length}", "Inspector");
            id1 = byte.MaxValue;
            id2 = byte.MaxValue;
            error = GetString("InspectCheckHelp");
            return false;
        }
        else if (!int.TryParse(nums[0], out int num1) || !int.TryParse(nums[1], out int num2))
        {
            Logger.Msg($"{nums.Length}, nums0 {nums[0]}, nums1 {nums[1]}", "Inspector");
            id1 = byte.MaxValue;
            id2 = byte.MaxValue;
            error = GetString("InspectCheckHelp");
            return false;
        }
        else
        {
            id1 = Convert.ToByte(num1);
            id2 = Convert.ToByte(num2);
        }

        //判断选择的玩家是否合理
        PlayerControl target1 = GetPlayerById(id1);
        PlayerControl target2 = GetPlayerById(id2);
        if (target1 == null || target1.Data.IsDead || target2 == null || target2.Data.IsDead)
        {
            error = GetString("InspectCheckNull");
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
    private static void TryHideMsgForCompare()
    {
        ChatUpdatePatch.DoBlockChat = true;
        List<CustomRoles> roles = CustomRolesHelper.AllRoles.Where(x => x is not CustomRoles.NotAssigned).ToList();
        var rd = IRandom.Instance;
        string msg;
        string[] command = ["cmp", "compare", "比较"];
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
                msg += rd.Next(0, 15).ToString();
                msg += " ";
                msg += rd.Next(0, 15).ToString();

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
    public override string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        var taskState8 = Main.PlayerStates?[playerId].TaskState;
        Color TextColor8;
        var TaskCompleteColor8 = Color.green;
        var NonCompleteColor8 = Color.yellow;
        var NormalColor8 = taskState8.IsTaskFinished ? TaskCompleteColor8 : NonCompleteColor8;
        TextColor8 = comms ? Color.gray : NormalColor8;
        string Completed8 = comms ? "?" : $"{taskState8.CompletedTasksCount}";
        Color TextColor81;
        if (MaxCheckLimit[playerId] < 1) TextColor81 = Color.red;
        else TextColor81 = Color.white;
        ProgressText.Append(ColorString(TextColor8, $"({Completed8}/{taskState8.AllTasksCount})"));
        ProgressText.Append(ColorString(TextColor81, $" <color=#ffffff>-</color> {Math.Round(MaxCheckLimit[playerId], 1)}"));
        return ProgressText.ToString();
    }

    public override string PVANameText(PlayerVoteArea pva, PlayerControl seer, PlayerControl target)
        => ColorString(GetRoleColor(CustomRoles.Inspector), target.PlayerId.ToString()) + " " + pva.NameText.text;

    public override string NotifyPlayerName(PlayerControl seer, PlayerControl target, string TargetPlayerName = "", bool IsForMeeting = false)
        => IsForMeeting ? ColorString(GetRoleColor(CustomRoles.Inspector), target.PlayerId.ToString()) + " " + TargetPlayerName : string.Empty;
}