using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate
{
public static class Spy
{
    private static readonly int Id = 640400;
    private static List<byte> playerIdList = new();
    public static Dictionary<byte, float> UseLimit = new();
    public static Dictionary<byte, long> SpyRedNameList = new();
    public static bool IsEnable = false;
    public static OptionItem SpyRedNameDur;
    public static OptionItem UseLimitOpt;
    public static OptionItem SpyAbilityUseGainWithEachTaskCompleted;

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Spy, 1);
        UseLimitOpt = IntegerOptionItem.Create(Id + 10, "AbilityUseLimit", new(1, 20, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spy])
        .SetValueFormat(OptionFormat.Times);
        SpyRedNameDur = FloatOptionItem.Create(Id + 11, "SpyRedNameDur", new(0f, 70f, 1f), 3f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spy])
            .SetValueFormat(OptionFormat.Seconds);
        SpyAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 12, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 0.5f, TabGroup.CrewmateRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Spy])
        .SetValueFormat(OptionFormat.Times);
    }
    public static void Init()
    {
        playerIdList = new();
        UseLimit = new();
        SpyRedNameList = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        UseLimit.Add(playerId, UseLimitOpt.GetInt());
        IsEnable = true;
    }
    public static void OnKillAttempt(PlayerControl killer, PlayerControl target)
    {
        if (killer == null) return;
        if (target == null) return;
        if (!target.Is(CustomRoles.Spy)) return;
        if (killer.PlayerId == target.PlayerId) return;

        if (UseLimit[target.PlayerId] >= 1)
        {
            UseLimit[target.PlayerId] -= 1;
            SpyRedNameList.TryAdd(killer.PlayerId, GetTimeStamp());
        }
    }
    public static void OnFixedUpdate(PlayerControl pc)
    {
        if (pc == null) return;
        if (!pc.Is(CustomRoles.Spy)) return;
        if (!SpyRedNameList.Any()) return;

        bool change = false;

        foreach (var x in SpyRedNameList)
        {
            if (x.Value + SpyRedNameDur.GetInt() < GetTimeStamp() || !GameStates.IsInTask)
            {
                SpyRedNameList.Remove(x.Key);
                    change = true;
            }
        }

        if (change && GameStates.IsInTask) { NotifyRoles(SpecifySeer: pc); }
    }
    public static string GetProgressText(byte playerId, bool comms)
    {
        var sb = new StringBuilder();

        var taskState = Main.PlayerStates?[playerId].GetTaskState();
        Color TextColor;
        var TaskCompleteColor = Color.green;
        var NonCompleteColor = Color.yellow;
        var NormalColor = taskState.IsTaskFinished ? TaskCompleteColor : NonCompleteColor;
        TextColor = comms ? Color.gray : NormalColor;
        string Completed = comms ? "?" : $"{taskState.CompletedTasksCount}";

        Color TextColor1;
        if (UseLimit[playerId] < 1) TextColor1 = Color.red;
        else TextColor1 = Color.white;

        sb.Append(ColorString(TextColor, $"<color=#777777>-</color> {Completed}/{taskState.AllTasksCount}"));
        sb.Append(ColorString(TextColor1, $" <color=#777777>-</color> {Math.Round(UseLimit[playerId], 1)}"));

        return sb.ToString();
        }
    }
}