using TOHE.Roles.AddOns.Crewmate;
using AmongUs.GameOptions;
using TOHE.Roles.Core;
using TOHE.Roles.Double;
using UnityEngine;
using Hazel;
using InnerNet;
using System;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;
internal class DarkFairy : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 29100;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.DarkFairy);
    public override CustomRoles Role => CustomRoles.DarkFairy;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralChaos;
    //==================================================================\\
    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute) => !ForRecompute;

    private static OptionItem TaskMarkPerRoundOpt;
    private static OptionItem CanDarkenNeutral;
    public static OptionItem DarkenedCountMode;


    private enum DarkenedCountModeSelectList
    {
        DarkFairy_DarkenedCountMode_None,
        DarkFairy_DarkenedCountMode_DarkFairy,
        DarkFairy_DarkenedCountMode_Original
    }

    private static readonly Dictionary<byte, List<int>> taskIndex = [];
    private static readonly Dictionary<byte, int> TaskMarkPerRound = [];

    private static int maxTasksMarkedPerRound = new();

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.DarkFairy);
        TaskMarkPerRoundOpt = IntegerOptionItem.Create(Id + 10, "TasksMarkPerRound", new(1, 14, 1), 3, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.DarkFairy])
            .SetValueFormat(OptionFormat.Votes);
        Options.OverrideTasksData.Create(Id + 11, TabGroup.NeutralRoles, CustomRoles.DarkFairy);
        DarkenedCountMode = StringOptionItem.Create(Id + 17, "DarkFairy_DarkenedCountMode", EnumHelper.GetAllNames<DarkenedCountModeSelectList>(), 1, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.DarkFairy]);
        CanDarkenNeutral = BooleanOptionItem.Create(Id + 18, "DarkFairyCanDarkenNeutral", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.DarkFairy]);
    }

    public override void Init()
    {
        taskIndex.Clear();
        TaskMarkPerRound.Clear();
        maxTasksMarkedPerRound = TaskMarkPerRoundOpt.GetInt();
    }
    public override void Add(byte playerId)
    {
        TaskMarkPerRound[playerId] = 0;
    }

    private void SendRPC(byte darkfairyID, int taskIndex = -1, bool isKill = false, bool clearAll = false)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player); //DarkFairyMarkedTask
        writer.Write(darkfairyID);
        writer.Write(taskIndex);
        writer.Write(isKill);
        writer.Write(clearAll);
        if (!isKill)
        {
            writer.Write(TaskMarkPerRound[darkfairyID]);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte darkfairyID = reader.ReadByte();
        int taskInd = reader.ReadInt32();
        bool isKill = reader.ReadBoolean();
        bool clearAll = reader.ReadBoolean();
        if (!isKill)
        {
            int uses = reader.ReadInt32();
            TaskMarkPerRound[darkfairyID] = uses;
            if (!clearAll)
            {
                if (!taskIndex.ContainsKey(darkfairyID)) taskIndex[darkfairyID] = [];
                taskIndex[darkfairyID].Add(taskInd);
            }
        }
        else
        {
            if (taskIndex.ContainsKey(darkfairyID)) taskIndex[darkfairyID].Remove(taskInd);
        }
        if (clearAll && taskIndex.ContainsKey(darkfairyID)) taskIndex[darkfairyID].Clear();
    }

    public override string GetProgressText(byte playerId, bool cooms)
    {
        if (!TaskMarkPerRound.ContainsKey(playerId)) TaskMarkPerRound[playerId] = 0;
        int markedTasks = TaskMarkPerRound[playerId];
        int x = Math.Max(maxTasksMarkedPerRound - markedTasks, 0);
        return Utils.ColorString(Utils.GetRoleColor(CustomRoles.DarkFairy).ShadeColor(0.25f), $"({x})");
    }

    public override void AfterMeetingTasks()
    {
        foreach (var playerId in TaskMarkPerRound.Keys)
        {
            TaskMarkPerRound[playerId] = 0;
            if (taskIndex.ContainsKey(playerId)) taskIndex[playerId].Clear();
            SendRPC(playerId, clearAll: true);
        }
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = 1f;
        AURoleOptions.EngineerInVentMaxTime = 0f;
    }
    public override void OnOthersTaskComplete(PlayerControl player, PlayerTask task)
    {
        if (player == null || _Player == null) return;
        if (!player.IsAlive()) return;
        byte playerId = player.PlayerId;

        if (player.Is(CustomRoles.DarkFairy))
        {
            if (player.IsAlive())
                if (!TaskMarkPerRound.ContainsKey(playerId)) TaskMarkPerRound[playerId] = 0;
            if (TaskMarkPerRound[playerId] >= maxTasksMarkedPerRound)
            {
                TaskMarkPerRound[playerId] = maxTasksMarkedPerRound;
                Logger.Info($"Max task per round ({TaskMarkPerRound[playerId]}) reached for {player.GetNameWithRole()}", "DarkFairy");
                return;
            }
            TaskMarkPerRound[playerId]++;
            if (!taskIndex.ContainsKey(playerId)) taskIndex[playerId] = [];
            taskIndex[playerId].Add(task.Index);
            SendRPC(darkfairyID: playerId, taskIndex: task.Index);
            player.Notify(GetString("DarkFairyTaskMarked"));
        }
        else if (CanBeDarkened(player) && Mini.Age == 18 || CanBeDarkened(player) && Mini.Age < 18 && !(player.Is(CustomRoles.NiceMini) || player.Is(CustomRoles.EvilMini)))
        {
            foreach (var darkfairyId in taskIndex.Keys)
            {
                if (taskIndex[darkfairyId].Contains(task.Index))
                {

                    player.RpcSetCustomRole(CustomRoles.Darkened);

                    taskIndex[darkfairyId].Remove(task.Index);
                    SendRPC(darkfairyID: darkfairyId, taskIndex: task.Index, isKill: true);
                    Logger.Info($"{player.GetAllRoleName()} was charmed by the dark fairy", "Dark Fairy");
                }
            }
        }
    }
    public static bool KnowRole(PlayerControl player, PlayerControl target) // Addons know each-other
    {
        if (player.Is(CustomRoles.Darkened) && target.Is(CustomRoles.DarkFairy)) return true;
        if (player.Is(CustomRoles.DarkFairy) && target.Is(CustomRoles.Darkened)) return true;
        if (player.Is(CustomRoles.Darkened) && target.Is(CustomRoles.Darkened)) return true;
        return false;
    }
    public static bool CanBeDarkened(PlayerControl pc)
    {
        return pc != null && (pc.GetCustomRole().IsCrewmate() || pc.GetCustomRole().IsImpostor() ||
            (CanDarkenNeutral.GetBool() && pc.GetCustomRole().IsNeutral())) && !pc.Is(CustomRoles.Darkened)
            && !pc.Is(CustomRoles.Admired) && !pc.Is(CustomRoles.Loyal) && !pc.Is(CustomRoles.Infectious)
            && !pc.Is(CustomRoles.Virus) && !pc.Is(CustomRoles.Cultist)
            && !(pc.GetCustomSubRoles().Contains(CustomRoles.Hurried) && !Hurried.CanBeConverted.GetBool());
    }
}
