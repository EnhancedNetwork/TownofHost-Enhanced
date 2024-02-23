using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

public static class Hawk
{

    private static readonly int Id = 28000;
    public static OptionItem KillCooldown;
    public static OptionItem HawkCanKillNum;
    public static OptionItem MinimumPlayersAliveToKill;
    public static OptionItem OnlyKillAfterXKillerNoEject;
    
    public static Dictionary<byte, int> KillCount;
    public static int KillersNoEjects;
    public static int KeepCount;
    public static int GetCount;
    public static void SetupCustomOptions()
    {
        SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Hawk);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 120f, 2.5f), 40f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hawk])
            .SetValueFormat(OptionFormat.Seconds);
        HawkCanKillNum = IntegerOptionItem.Create(Id + 11, "HawkCanKillNum", new(1, 15, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Retributionist])
            .SetValueFormat(OptionFormat.Players);
        MinimumPlayersAliveToKill = IntegerOptionItem.Create(Id + 12, "MinimumPlayersAliveToKill", new(0, 15, 1), 4, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hawk])
            .SetValueFormat(OptionFormat.Players);
        OnlyKillAfterXKillerNoEject = IntegerOptionItem.Create(Id + 13, "MinimumNoKillerEjectsToKill", new(0, 10, 1), 0, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hawk])
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
        KillCount.Add(PlayerId, HawkCanKillNum.GetInt());
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Hawk);
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
    public static bool IsEnable => Main.AllPlayerControls.Any(x => x.Is(CustomRoles.Hawk));
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
    }

    public static void SetKillCooldown()
    {
        AURoleOptions.GuardianAngelCooldown = KillCooldown.GetFloat();
        AURoleOptions.ProtectionDurationSeconds = 0f;
    }
    public static bool OnCheckProtect(PlayerControl killer, PlayerControl target)
    {
        if (CheckRetriConflicts(killer, target))
        {
            killer.RpcMurderPlayerV3(target);
            killer.RpcResetAbilityCooldown();
            KillCount[killer.PlayerId]--;
            SendRPC(killer.PlayerId);
        }
        else if (KillCount[killer.PlayerId] <= 0) killer.Notify(GetString("HawkKillMax"));
        else if (KillersNoEjects < OnlyKillAfterXKillerNoEject.GetInt()) killer.Notify(GetString("HawkKillNoEject").Replace("{0}", OnlyKillAfterXKillerNoEject.GetInt().ToString()));
        else if (Main.AllAlivePlayerControls.Length < MinimumPlayersAliveToKill.GetInt()) killer.Notify(GetString("HawkKillTooManyDead"));
        return false;
    }

    private static bool CheckRetriConflicts(PlayerControl killer, PlayerControl target)
    {
        return target != null && KillersNoEjects >= OnlyKillAfterXKillerNoEject.GetInt()
            && Main.AllAlivePlayerControls.Length >= MinimumPlayersAliveToKill.GetInt()
            && KillCount[killer.PlayerId] > 0
            && !target.Is(CustomRoles.Pestilence)
            && (target.Is(CustomRoles.NiceMini) ? Mini.Age > 18 : true);
    }
    public static bool CanKill(byte id) => KillCount.TryGetValue(id, out var x) && x > 0;
    public static string GetRetributeLimit(byte playerId) => Utils.ColorString(CanKill(playerId) ? Utils.GetRoleColor(CustomRoles.Hawk).ShadeColor(0.25f) : Color.gray, KillCount.TryGetValue(playerId, out var killLimit) ? $"({killLimit})" : "Invalid");

}


