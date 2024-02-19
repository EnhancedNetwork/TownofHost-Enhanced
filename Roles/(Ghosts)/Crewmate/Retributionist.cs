using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

public static class Retributionist
{

    private static readonly int Id = 11000;
    public static OptionItem KillCooldown;
    public static OptionItem RetributionistCanKillNum;
    public static OptionItem MinimumPlayersAliveToRetri;
    public static OptionItem OnlyKillAfterXKillerNoEject;
    public static OverrideTasksData RetributionistTasks;
    
    public static Dictionary<byte, int> KillCount;
    public static int KillersNoEjects;
    public static int KeepCount;
    public static int GetCount;
    public static void SetupCustomOptions()
    {
        SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Retributionist);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 60f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Retributionist])
            .SetValueFormat(OptionFormat.Seconds);
        RetributionistCanKillNum = IntegerOptionItem.Create(Id + 11, "RetributionistCanKillNum", new(1, 15, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Retributionist])
            .SetValueFormat(OptionFormat.Players);
        MinimumPlayersAliveToRetri = IntegerOptionItem.Create(Id + 12, "MinimumPlayersAliveToRetri", new(0, 15, 1), 4, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Retributionist])
            .SetValueFormat(OptionFormat.Players);
        OnlyKillAfterXKillerNoEject = IntegerOptionItem.Create(Id + 13, "MinimumNoKillerEjectsToRetri", new(0, 10, 1), 0, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Retributionist])
            .SetValueFormat(OptionFormat.Players);
    }

    public static void Init()
    {
        KillCount = [];
        KillersNoEjects = 0;
        KeepCount = 0;
        GetCount = 0;
    }
    public static void Add(byte PlayerId)
    {
        KillCount.Add(PlayerId, RetributionistCanKillNum.GetInt());
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Retributionist);
        writer.Write(playerId);
        writer.Write(KillCount[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        KillCount[PlayerId] = Limit;
    }
    public static bool IsEnable => Main.AllPlayerControls.Any(x => x.Is(CustomRoles.Retributionist));

    public static void OnReportDeadBody()
    {
        KeepCount = 0;
        foreach (var KVC in Main.AllAlivePlayerControls.Where(x => x.GetCustomRole().IsImpostor() || x.GetCustomRole().IsNK()))
        {
            KeepCount++;
        }
    }

    public static void AfterMeetingTasks()
    {
        GetCount = 0;
        foreach (var KVC in Main.AllAlivePlayerControls.Where(x => x.GetCustomRole().IsImpostor() || x.GetCustomRole().IsNK()))
        {
            GetCount++;
        }
        
        if (GetCount >= KeepCount)
        {
            KillersNoEjects++;
        }
        else
        {
            KillersNoEjects = 0;
        }
    }

    public static void SetKillCooldown() => AURoleOptions.GuardianAngelCooldown = KillCooldown.GetFloat();
    public static bool OnCheckProtect(PlayerControl killer, PlayerControl target)
    {
        if (CheckRetriConflicts(killer, target))
        {
            killer.RpcMurderPlayerV3(target);
            killer.RpcResetAbilityCooldown();
            killer.SetKillCooldown();
            KillCount[killer.PlayerId]--;
            SendRPC(killer.PlayerId);
        }
        else if (KillCount[killer.PlayerId] > RetributionistCanKillNum.GetInt()) killer.Notify(GetString("RetributionistKillMax"));
        else if (KillersNoEjects < OnlyKillAfterXKillerNoEject.GetInt()) killer.Notify(GetString("RetributionistKillNoEject"));
        else if (Main.AllAlivePlayerControls.Count() < MinimumPlayersAliveToRetri.GetInt()) killer.Notify(GetString("RetributionistKillTooManyDead"));
        return false;
    }

    private static bool CheckRetriConflicts(PlayerControl killer, PlayerControl target)
    {
        return target != null && KillersNoEjects >= OnlyKillAfterXKillerNoEject.GetInt()
            && Main.AllAlivePlayerControls.Count() >= MinimumPlayersAliveToRetri.GetInt()
            && KillCount[killer.PlayerId] >= RetributionistCanKillNum.GetInt()
            && !target.Is(CustomRoles.Pestilence); // Double body could happen
    }
    public static bool CanKill(byte id) => KillCount.TryGetValue(id, out var x) && x > 0;
    public static string GetRetributeLimit(byte playerId) => Utils.ColorString(CanKill(playerId) ? Utils.GetRoleColor(CustomRoles.Retributionist).ShadeColor(0.25f) : Color.gray, KillCount.TryGetValue(playerId, out var killLimit) ? $"({killLimit})" : "Invalid");

}


