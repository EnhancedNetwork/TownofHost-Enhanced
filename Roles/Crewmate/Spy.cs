using Hazel;
using System;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Spy : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 9700;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem SpyRedNameDur;
    private static OptionItem UseLimitOpt;
    private static OptionItem SpyAbilityUseGainWithEachTaskCompleted;
    private static OptionItem SpyInteractionBlocked;

    private static readonly Dictionary<byte, long> SpyRedNameList = [];
    private static bool change = false;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Spy, 1);
        UseLimitOpt = IntegerOptionItem.Create(Id + 10, "AbilityUseLimit", new(1, 20, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spy])
        .SetValueFormat(OptionFormat.Times);
        SpyRedNameDur = FloatOptionItem.Create(Id + 11, "SpyRedNameDur", new(0f, 70f, 1f), 3f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spy])
            .SetValueFormat(OptionFormat.Seconds);
        SpyAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 12, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 0.5f, TabGroup.CrewmateRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Spy])
        .SetValueFormat(OptionFormat.Times);
        SpyInteractionBlocked = BooleanOptionItem.Create(Id + 13, "SpyInteractionBlocked", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spy]);
    }
    public override void Init()
    {
        playerIdList.Clear();
        SpyRedNameList.Clear();
        change = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        AbilityLimit = UseLimitOpt.GetInt();
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
    }
    public static void SendRPC(byte susId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SpyRedNameSync, SendOption.Reliable, -1);
        writer.Write(susId);
        writer.Write(SpyRedNameList[susId].ToString());
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void SendRPC(byte susId, bool changeColor)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SpyRedNameRemove, SendOption.Reliable, -1);
        writer.Write(susId);
        writer.Write(changeColor);
        Logger.Info($"RPC to remove player {susId} from red name list and change `change` to {changeColor}", "Spy");
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader, bool isRemove = false)
    {
        if (isRemove)
        {
            SpyRedNameList.Remove(reader.ReadByte());
            change = reader.ReadBoolean();
            return;
        }
        byte susId = reader.ReadByte();
        string stimeStamp = reader.ReadString();
        if (long.TryParse(stimeStamp, out long timeStamp)) SpyRedNameList[susId] = timeStamp;
    }
    public bool OnKillAttempt(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        if (killer.PlayerId == target.PlayerId) return true;

        if (AbilityLimit >= 1 && killer.IsAlive())
        {
            AbilityLimit -= 1;
            SendSkillRPC();
            SpyRedNameList.TryAdd(killer.PlayerId, GetTimeStamp());
            SendRPC(killer.PlayerId);                
            if (SpyInteractionBlocked.GetBool()) 
                killer.SetKillCooldown(time: 10f);
            NotifyRoles(SpecifySeer: target, ForceLoop: true);
            return false;
        }
        return true;
    }
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
        => OnKillAttempt(killer, target);

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (lowLoad || !SpyRedNameList.Any()) return;
        
        change = false;
        foreach (var x in SpyRedNameList)
        {
            if (x.Value + SpyRedNameDur.GetInt() < nowTime)
            {
                if (SpyRedNameList.ContainsKey(x.Key))
                {
                    SpyRedNameList.Remove(x.Key);
                    change = true;
                    SendRPC(x.Key, change);
                }
            }
        }
        if (change) { NotifyRoles(SpecifySeer: player, ForceLoop: true); }
    }
    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (player.IsAlive())
        {
            AbilityLimit += SpyAbilityUseGainWithEachTaskCompleted.GetFloat();
            SendSkillRPC();
        }
        return true;
    }
    public override string GetProgressText(byte playerId, bool comms)
    {
        var sb = "";

        var taskState = Main.PlayerStates?[playerId].TaskState;
        Color TextColor;
        var TaskCompleteColor = Color.green;
        var NonCompleteColor = Color.yellow;
        var NormalColor = taskState.IsTaskFinished ? TaskCompleteColor : NonCompleteColor;
        TextColor = comms ? Color.gray : NormalColor;
        string Completed = comms ? "?" : $"{taskState.CompletedTasksCount}";

        Color TextColor1;
        if (AbilityLimit < 1) TextColor1 = Color.red;
        else TextColor1 = Color.white;

        sb += ColorString(TextColor, $"({Completed}/{taskState.AllTasksCount})");
        sb += ColorString(TextColor1, $" <color=#777777>-</color> {Math.Round(AbilityLimit, 1)}");

        return sb;
    }
    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target) => (seer.Is(CustomRoles.Spy) && SpyRedNameList.ContainsKey(target.PlayerId)) ? "#BA4A00" : "";
}