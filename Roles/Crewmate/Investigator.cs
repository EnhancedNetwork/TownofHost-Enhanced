using Hazel;
using System.Collections.Generic;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

public static class Investigator
{
    private static readonly int Id = 24900;
    private static List<byte> playerIdList = [];
    public static Dictionary<byte, HashSet<byte>> InvestigatedList = [];
    public static bool IsEnable = false;


    public static OptionItem InvestigateCooldown;
    public static OptionItem InvestigateMax;
    public static OptionItem InvestigateRoundMax;


    private static Dictionary<byte, int> MaxInvestigateLimit = [];
    private static Dictionary<byte, int> RoundInvestigateLimit = [];

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Investigator, 1, zeroOne: false);
        InvestigateCooldown = FloatOptionItem.Create(Id + 10, "InvestigateCooldown", new(0f, 180f, 2.5f), 27.5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Investigator])
            .SetValueFormat(OptionFormat.Seconds);
        InvestigateMax = IntegerOptionItem.Create(Id + 11, "InvestigateMax", new(1, 15, 1), 5, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Investigator])
            .SetValueFormat(OptionFormat.Times);
        InvestigateRoundMax = IntegerOptionItem.Create(Id + 12, "InvestigateRoundMax", new(1, 15, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Investigator])
            .SetValueFormat(OptionFormat.Times);
        //    CrewKillingShowAs = StringOptionItem.Create(Id + 12, "CrewKillingShowAs", ColorTypeText, 1, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Investigator]);
        //    PassiveNeutralsShowAs = StringOptionItem.Create(Id + 13, "PassiveNeutralsShowAs", ColorTypeText, 2, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Investigator]);
        //    NeutralKillingShowAs = StringOptionItem.Create(Id + 14, "NeutralKillingShowAs", ColorTypeText, 0, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Investigator]);
    }
    public static void Init()
    {
        playerIdList = [];
        InvestigatedList = [];
        MaxInvestigateLimit = [];
        RoundInvestigateLimit = [];
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        MaxInvestigateLimit[playerId] = InvestigateMax.GetInt();
        RoundInvestigateLimit[playerId] = InvestigateRoundMax.GetInt();
        InvestigatedList[playerId] = [];
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        MaxInvestigateLimit.Remove(playerId);
        RoundInvestigateLimit.Remove(playerId);
        InvestigatedList.Remove(playerId);
    }

    private static void SendRPC(int operate, byte playerId = byte.MaxValue, byte targetId = byte.MaxValue)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetInvestgatorLimit, SendOption.Reliable, -1);
        writer.Write(operate);
        if (operate == 0)
        {
            writer.Write(playerId);
            writer.Write(targetId);
            writer.Write(MaxInvestigateLimit[playerId]);
            writer.Write(RoundInvestigateLimit[playerId]);
            return;
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        int operate = reader.ReadInt32();
        if (operate == 0)
        {
            byte investigatorID = reader.ReadByte();
            byte targetID = reader.ReadByte();
            if (!InvestigatedList.ContainsKey(investigatorID)) InvestigatedList[investigatorID] = [];
            InvestigatedList[investigatorID].Add(targetID);

            int maxLimit = reader.ReadInt32();
            MaxInvestigateLimit[investigatorID] = maxLimit;

            int roundLimit = reader.ReadInt32();
            MaxInvestigateLimit[investigatorID] = roundLimit;
        }
        if (operate == 1)
        {
            foreach (var playerid in RoundInvestigateLimit.Keys)
            {
                RoundInvestigateLimit[playerid] = InvestigateRoundMax.GetInt();
                InvestigatedList[playerid] = [];
            }
        }
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = InvestigateCooldown.GetFloat();
    public static bool CanUseKillButton(PlayerControl player)
    {
        if (!player.Is(CustomRoles.Investigator) || player == null) return false;
        byte pid = player.PlayerId;
        if (!MaxInvestigateLimit.ContainsKey(pid)) MaxInvestigateLimit[pid] = InvestigateMax.GetInt();
        if (!RoundInvestigateLimit.ContainsKey(pid)) RoundInvestigateLimit[pid] = InvestigateRoundMax.GetInt();
        return !player.Data.IsDead && MaxInvestigateLimit[player.PlayerId] >= 1 && RoundInvestigateLimit[player.PlayerId] >= 1;
    }
    public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return;
        if (!killer.Is(CustomRoles.Investigator) || !IsEnable) return;

        if (!MaxInvestigateLimit.ContainsKey(killer.PlayerId)) MaxInvestigateLimit[killer.PlayerId] = InvestigateMax.GetInt();
        if (!RoundInvestigateLimit.ContainsKey(killer.PlayerId)) RoundInvestigateLimit[killer.PlayerId] = InvestigateRoundMax.GetInt();

        if (MaxInvestigateLimit[killer.PlayerId] < 1 || RoundInvestigateLimit[killer.PlayerId] < 1) return;

        MaxInvestigateLimit[killer.PlayerId]--;
        RoundInvestigateLimit[killer.PlayerId]--;
        if (!InvestigatedList.ContainsKey(killer.PlayerId)) InvestigatedList[killer.PlayerId] = [];
        InvestigatedList[killer.PlayerId].Add(target.PlayerId);
        //sendRPC for max, round and targetlist
        //SendRPC();
        SendRPC(operate: 0, playerId: killer.PlayerId, targetId: target.PlayerId);

        killer.ResetKillCooldown();
        killer.SetKillCooldown();

        if (!DisableShieldAnimations.GetBool())
            killer.RpcGuardAndKill(target);
    }
    public static string InvestigatedColor(PlayerControl seer, PlayerControl target)
    {
        if (seer == null || target == null) return string.Empty;
        if (!seer.Is(CustomRoles.Investigator)) return string.Empty;
        if (!InvestigatedList.TryGetValue(seer.PlayerId, out var targetList)) return string.Empty;
        if (!targetList.Contains(target.PlayerId)) return string.Empty;
        if (ExtendedPlayerControl.HasKillButton(target) || CopyCat.playerIdList.Contains(target.PlayerId)) return "#FF1919";
        else return "#8CFFFF";
    }
    public static void OnReportDeadBody()
    {
        foreach (var playerid in RoundInvestigateLimit.Keys)
        {
            RoundInvestigateLimit[playerid] = InvestigateRoundMax.GetInt();
            InvestigatedList[playerid] = [];
        }
        SendRPC(1);
    }
    public static string GetInvestigateLimit(byte playerId) => Utils.ColorString(MaxInvestigateLimit[playerId] >= 1 ? Utils.GetRoleColor(CustomRoles.Investigator).ShadeColor(0.25f) : Color.gray, $"({MaxInvestigateLimit[playerId]})");
}
