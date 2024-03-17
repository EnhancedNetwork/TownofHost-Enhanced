using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class PotionMaster : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 17700;
    public static HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem RitualMaxCount;
    private static OptionItem CanVent;
    private static OptionItem HasImpostorVision;

    private static Dictionary<byte, int> RitualCount = [];
    private static Dictionary<byte, List<byte>> RitualTarget = [];

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.PotionMaster, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 14, "KillCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PotionMaster])
            .SetValueFormat(OptionFormat.Seconds);
        RitualMaxCount = IntegerOptionItem.Create(Id + 11, "RitualMaxCount", new(1, 15, 1), 5, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PotionMaster])
            .SetValueFormat(OptionFormat.Times);
        CanVent = BooleanOptionItem.Create(Id + 12, "CanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PotionMaster]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PotionMaster]);
    }
    public override void Init()
    {
        playerIdList = [];
        RitualCount = [];
        RitualTarget = [];
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        RitualCount.TryAdd(playerId, RitualMaxCount.GetInt());
        RitualTarget.TryAdd(playerId, []);

        var pc = Utils.GetPlayerById(playerId);
        pc?.AddDoubleTrigger();

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    private static void SendRPC(byte playerId, byte targetId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.PotionMaster);
        writer.Write(playerId);
        writer.Write(RitualCount[playerId]);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte playerId = reader.ReadByte();
        {
            if (RitualCount.ContainsKey(playerId))
                RitualCount[playerId] = reader.ReadInt32();
            else
                RitualCount.Add(playerId, RitualMaxCount.GetInt());
        }
        {
            if (RitualCount.ContainsKey(playerId))
                RitualTarget[playerId].Add(reader.ReadByte());
            else
                RitualTarget.Add(playerId, []);
        }
    }
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();
    public override bool CanUseSabotage(PlayerControl pc) => true;

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (RitualCount[killer.PlayerId] > 0)
        {
            return killer.CheckDoubleTrigger(target, () => { SetRitual(killer, target); });
        }
        else return true;
    }

    public static bool IsRitual(byte seer, byte target)
    {
        if (RitualTarget[seer].Contains(target))
        {
            return true;
        }
        return false;
    }
    private static void SetRitual(PlayerControl killer, PlayerControl target)
    {
        if (!IsRitual(killer.PlayerId, target.PlayerId))
        {
            RitualCount[killer.PlayerId]--;
            RitualTarget[killer.PlayerId].Add(target.PlayerId);
            Logger.Info($"{killer.GetNameWithRole()}: Divined divination destination -> {target.GetNameWithRole()} || remaining {RitualCount[killer.PlayerId]} times", "PotionMaster");

            Utils.NotifyRoles(SpecifySeer: killer);
            SendRPC(killer.PlayerId, target.PlayerId);

            killer.SetKillCooldown();
        }
    }
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target)
    {
        var IsWatch = false;
        RitualTarget.Do(x =>
        {
            if (x.Value != null && seer.PlayerId == x.Key && x.Value.Contains(target.PlayerId) && Utils.GetPlayerById(x.Key).IsAlive())
                IsWatch = true;
        });
        return IsWatch;
    }
    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target)
        => KnowRoleTarget(seer, target);

    public override string GetProgressText(byte playerId, bool coooonms) => Utils.ColorString(RitualCount[playerId] > 0 ? Utils.GetRoleColor(CustomRoles.PotionMaster).ShadeColor(0.25f) : Color.gray, RitualCount.TryGetValue(playerId, out var shotLimit) ? $"({shotLimit})" : "Invalid");
}