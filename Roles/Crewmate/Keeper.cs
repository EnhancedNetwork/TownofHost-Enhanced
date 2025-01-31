using Hazel;
using System;
using System.Text;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Keeper : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Keeper;
    private const int Id = 26500;

    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem KeeperUsesOpt;

    private static readonly HashSet<byte> keeperTarget = [];
    private static readonly Dictionary<byte, int> keeperUses = [];
    private static readonly Dictionary<byte, bool> DidVote = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Keeper);
        KeeperUsesOpt = IntegerOptionItem.Create(Id + 10, "MaxProtections", new(1, 14, 1), 3, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Keeper])
            .SetValueFormat(OptionFormat.Times);

    }
    public override void Init()
    {
        keeperTarget.Clear();
        keeperUses.Clear();
        DidVote.Clear();
    }

    public override void Add(byte playerId)
    {
        DidVote.Add(playerId, false);
        keeperUses[playerId] = 0;
    }
    public override void Remove(byte playerId)
    {
        DidVote.Remove(playerId);
        keeperUses.Remove(playerId);
    }

    public override string GetProgressText(byte playerId, bool comms)
    {
        if (playerId == byte.MaxValue) return string.Empty;
        if (!keeperUses.ContainsKey(playerId)) return string.Empty;
        var ProgressText = new StringBuilder();
        var taskState8 = Main.PlayerStates?[playerId].TaskState;
        Color TextColor8;
        var TaskCompleteColor8 = Color.green;
        var NonCompleteColor8 = Color.yellow;
        var NormalColor8 = taskState8.IsTaskFinished ? TaskCompleteColor8 : NonCompleteColor8;
        TextColor8 = comms ? Color.gray : NormalColor8;
        string Completed8 = comms ? "?" : $"{taskState8.CompletedTasksCount}";
        Color TextColor81;
        var maxUses = KeeperUsesOpt.GetInt();
        var usesLeft = Math.Max(maxUses - keeperUses[playerId], 0);
        if (usesLeft < 1) TextColor81 = Color.red;
        else TextColor81 = Utils.GetRoleColor(CustomRoles.Keeper);
        ProgressText.Append(Utils.ColorString(TextColor8, $"({Completed8}/{taskState8.AllTasksCount})"));
        ProgressText.Append(Utils.ColorString(TextColor81, $" <color=#ffffff>-</color> {usesLeft}"));
        return ProgressText.ToString();
    }

    private static void SendRPC(int type, byte keeperId = 0xff, byte targetId = 0xff)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.KeeperRPC, SendOption.Reliable, -1);
        writer.Write(type);
        if (type == 0)
        {
            writer.Write(keeperId);
            writer.Write(keeperUses[keeperId]);
            writer.Write(targetId);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        int type = reader.ReadInt32();
        if (type == 0)
        {
            byte keeperId = reader.ReadByte();
            DidVote[keeperId] = true;

            int uses = reader.ReadInt32();
            keeperUses[keeperId] = uses;

            byte targetId = reader.ReadByte();
            if (!keeperTarget.Contains(targetId)) keeperTarget.Add(targetId);
        }

        else if (type == 1)
        {
            foreach (var pid in DidVote.Keys)
            {
                DidVote[pid] = false;
            }
            keeperTarget.Clear();
        }
    }


    public override bool CheckVote(PlayerControl voter, PlayerControl target)
    {
        if (!CustomRoles.Keeper.HasEnabled()) return true;
        if (voter == null || target == null) return true;
        if (!voter.Is(CustomRoles.Keeper)) return true;
        if (DidVote[voter.PlayerId]) return true;
        DidVote[voter.PlayerId] = true;
        if (keeperTarget.Contains(target.PlayerId)) return true;
        if (keeperUses[voter.PlayerId] >= KeeperUsesOpt.GetInt()) return true;

        keeperUses[voter.PlayerId]++;
        keeperTarget.Add(target.PlayerId);
        Logger.Info($"{voter.GetNameWithRole()} chosen as keeper target by {target.GetNameWithRole()}", "Keeper");
        SendRPC(type: 0, keeperId: voter.PlayerId, targetId: target.PlayerId); // add keeperUses, KeeperTarget and DidVote
        Utils.SendMessage(string.Format(GetString("KeeperProtect"), target.GetRealName()), voter.PlayerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.Keeper), GetString("KeeperTitle")));
        return false;
    }

    public override void AfterMeetingTasks()
    {
        foreach (var pid in DidVote.Keys)
        {
            DidVote[pid] = false;
        }
        keeperTarget.Clear();
        SendRPC(type: 1);
    }

    public static bool IsTargetExiled(byte exileId) => keeperTarget.Contains(exileId);
}
