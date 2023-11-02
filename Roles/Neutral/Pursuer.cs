using Hazel;
using System.Collections.Generic;
using TOHE.Modules;
using UnityEngine;

namespace TOHE.Roles.Neutral;

public static class Pursuer
{
    private static readonly int Id = 10200;
    private static List<byte> playerIdList = new();
    public static bool IsEnable = false;
    private static Dictionary<byte, List<byte>> clientList = new();
    private static List<byte> notActiveList = new();
    public static Dictionary<byte, int> SeelLimit = new();
    public static OptionItem PursuerSkillCooldown;
    public static OptionItem PursuerSkillLimitTimes;
    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Pursuer);
        PursuerSkillCooldown = FloatOptionItem.Create(Id + 10, "PursuerSkillCooldown", new(2.5f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Pursuer])
            .SetValueFormat(OptionFormat.Seconds);
        PursuerSkillLimitTimes = IntegerOptionItem.Create(Id + 11, "PursuerSkillLimitTimes", new(1, 20, 1), 2, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Pursuer])
            .SetValueFormat(OptionFormat.Times);
    }
    public static void Init()
    {
        playerIdList = new();
        clientList = new();
        notActiveList = new();
        SeelLimit = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        SeelLimit.Add(playerId, PursuerSkillLimitTimes.GetInt());
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetPursuerSellLimit, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(SeelLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (SeelLimit.ContainsKey(PlayerId))
            SeelLimit[PlayerId] = Limit;
        else
            SeelLimit.Add(PlayerId, PursuerSkillLimitTimes.GetInt());
    }
    public static bool CanUseKillButton(byte playerId)
        => !Main.PlayerStates[playerId].IsDead
        && SeelLimit.TryGetValue(playerId, out var x) && x >= 1;
    public static string GetSeelLimit(byte playerId) => Utils.ColorString(CanUseKillButton(playerId) ? Utils.GetRoleColor(CustomRoles.Pursuer) : Color.gray, SeelLimit.TryGetValue(playerId, out var x) ? $"({x})" : "Invalid");
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(id) ? PursuerSkillCooldown.GetFloat() : 300f;
    public static bool IsClient(byte playerId)
    {
        foreach (var pc in clientList)
            if (pc.Value.Contains(playerId)) return true;
        return false;
    }
    public static bool IsClient(byte pc, byte tar) => clientList.TryGetValue(pc, out var x) && x.Contains(tar);
    public static bool CanBeClient(PlayerControl pc) => pc != null && pc.IsAlive() && !GameStates.IsMeeting && !IsClient(pc.PlayerId);
    public static bool CanSeel(byte playerId) => playerIdList.Contains(playerId) && SeelLimit.TryGetValue(playerId, out int x) && x > 0;
    public static void SeelToClient(PlayerControl pc, PlayerControl target)
    {
        if (pc == null || target == null || !pc.Is(CustomRoles.Pursuer)) return;
        SeelLimit[pc.PlayerId]--;
        SendRPC(pc.PlayerId);
        if (!clientList.ContainsKey(pc.PlayerId)) clientList.Add(pc.PlayerId, new());
        clientList[pc.PlayerId].Add(target.PlayerId);
        if (!Options.DisableShieldAnimations.GetBool()) pc.RpcGuardAndKill(pc);
        notActiveList.Add(pc.PlayerId);
        pc.SetKillCooldown();
        pc.RPCPlayCustomSound("Bet");
        Utils.NotifyRoles(SpecifySeer: pc);
        Logger.Info($"赝品商 {pc.GetRealName()} 将赝品卖给了 {target.GetRealName()}", "Pursuer");
    }
    public static bool OnClientMurder(PlayerControl pc)
    {
        if (!IsClient(pc.PlayerId) || notActiveList.Contains(pc.PlayerId)) return false;
        byte cfId = byte.MaxValue;
        foreach (var cf in clientList)
            if (cf.Value.Contains(pc.PlayerId)) cfId = cf.Key;
        if (cfId == byte.MaxValue) return false;
        var killer = Utils.GetPlayerById(cfId);
        var target = pc;
        if (killer == null) return false;
        target.SetRealKiller(killer);
        target.Data.IsDead = true;
        Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Misfire;
        target.RpcMurderPlayerV3(target);
        Main.PlayerStates[target.PlayerId].SetDead();
        Logger.Info($"赝品商 {pc.GetRealName()} 的客户 {target.GetRealName()} 因使用赝品走火自杀", "Pursuer");
        return true;
    }
    public static void OnReportDeadBody()
    {
        notActiveList.Clear();
        foreach (var cl in clientList)
            foreach (var pc in cl.Value)
            {
                var target = Utils.GetPlayerById(pc);
                if (target == null || !target.IsAlive()) continue;
                var role = target.GetCustomRole();
                if (
                    (role.IsCrewmate() && !role.IsCK()) ||
                    (role.IsNeutral() && !role.IsNK())
                    )
                {
                    var killer = Utils.GetPlayerById(cl.Key);
                    if (killer == null) continue;
                    CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Misfire, target.PlayerId);
                    target.SetRealKiller(Utils.GetPlayerById(pc));
                    target.SetRealKiller(killer);
                    Logger.Info($"赝品商 {killer.GetRealName()} 的客户 {target.GetRealName()} 因不带刀自杀", "Pursuer");
                }
            }
    }
}