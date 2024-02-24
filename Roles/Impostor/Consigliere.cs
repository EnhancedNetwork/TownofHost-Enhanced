using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

internal class Consigliere : RoleBase
{
    private const int Id = 3100;
    public static bool On;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    private static OptionItem KillCooldown;
    private static OptionItem DivinationMaxCount;

    private static Dictionary<byte, int> DivinationCount = [];
    private static Dictionary<byte, List<byte>> DivinationTarget = [];

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Consigliere);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Consigliere])
            .SetValueFormat(OptionFormat.Seconds);
        DivinationMaxCount = IntegerOptionItem.Create(Id + 11, "ConsigliereDivinationMaxCount", new(1, 15, 1), 5, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Consigliere])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        DivinationCount = [];
        DivinationTarget = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        DivinationCount.TryAdd(playerId, DivinationMaxCount.GetInt());
        DivinationTarget.TryAdd(playerId, []);
        On = true;

        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();
    }

    private static void SendRPC(byte playerId, byte targetId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetConsigliere, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(DivinationCount[playerId]);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte playerId = reader.ReadByte();
        {
            if (DivinationCount.ContainsKey(playerId))
                DivinationCount[playerId] = reader.ReadInt32();
            else
                DivinationCount.Add(playerId, DivinationMaxCount.GetInt());
        }{
            if (DivinationCount.ContainsKey(playerId))
                DivinationTarget[playerId].Add(reader.ReadByte());
            else
                DivinationTarget.Add(playerId, []);
        }
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (DivinationCount[killer.PlayerId] > 0)
        {
            return killer.CheckDoubleTrigger(target, () => { SetDivination(killer, target); });
        }
        else return true;  
    }

    private static bool IsDivination(byte seer, byte target)
    {
        if (DivinationTarget[seer].Contains(target))
        {
            return true;
        }
        return false;
    }
    private static void SetDivination(PlayerControl killer, PlayerControl target)
    {
        if (!IsDivination(killer.PlayerId, target.PlayerId))
        {
            DivinationCount[killer.PlayerId]--;
            DivinationTarget[killer.PlayerId].Add(target.PlayerId);
            Logger.Info($"{killer.GetNameWithRole()}：占った 占い先→{target.GetNameWithRole()} || 残り{DivinationCount[killer.PlayerId]}回", "Consigliere");
            Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);

            SendRPC(killer.PlayerId, target.PlayerId);
            //キルクールの適正化
            killer.SetKillCooldown();
            //killer.RpcGuardAndKill(target);
        }
    }
    public static bool IsShowTargetRole(PlayerControl seer, PlayerControl target)
    {
        var IsWatch = false;
        DivinationTarget.Do(x =>
        {
            if (x.Value != null && seer.PlayerId == x.Key && x.Value.Contains(target.PlayerId) && Utils.GetPlayerById(x.Key).IsAlive())
                IsWatch = true;
        });
        return IsWatch;
    }
    public static string GetDivinationCount(byte playerId) => Utils.ColorString(DivinationCount[playerId] > 0 ? Utils.GetRoleColor(CustomRoles.Consigliere).ShadeColor(0.25f) : Color.gray, DivinationCount.TryGetValue(playerId, out var shotLimit) ? $"({shotLimit})" : "Invalid");
}