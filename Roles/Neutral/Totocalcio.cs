using Hazel;
using Il2CppSystem;
using System.Collections.Generic;
using System.Linq;
using TOHE.Modules;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

public static class Totocalcio
{
    private static readonly int Id = 12800;
    public static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    private static OptionItem MaxBetTimes;
    public static OptionItem BetCooldown;
    private static OptionItem BetCooldownIncrese;
    private static OptionItem MaxBetCooldown;
    private static OptionItem KnowTargetRole;
    private static OptionItem BetTargetKnowTotocalcio;

    private static Dictionary<byte, int> BetTimes = new();
    public static Dictionary<byte, byte> BetPlayer = new();

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Totocalcio, 1, zeroOne: false);
        MaxBetTimes = IntegerOptionItem.Create(Id + 10, "TotocalcioMaxBetTimes", new(1, 20, 1), 3, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Totocalcio])
            .SetValueFormat(OptionFormat.Times);
        BetCooldown = FloatOptionItem.Create(Id + 12, "TotocalcioBetCooldown", new(0f, 180f, 2.5f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Totocalcio])
            .SetValueFormat(OptionFormat.Seconds);
        BetCooldownIncrese = FloatOptionItem.Create(Id + 14, "TotocalcioBetCooldownIncrese", new(0f, 60f, 1f), 4f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Totocalcio])
            .SetValueFormat(OptionFormat.Seconds);
        MaxBetCooldown = FloatOptionItem.Create(Id + 16, "TotocalcioMaxBetCooldown", new(0f, 180f, 2.5f), 50f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Totocalcio])
            .SetValueFormat(OptionFormat.Seconds);
        KnowTargetRole = BooleanOptionItem.Create(Id + 18, "TotocalcioKnowTargetRole", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Totocalcio]);
        BetTargetKnowTotocalcio = BooleanOptionItem.Create(Id + 20, "TotocalcioBetTargetKnowTotocalcio", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Totocalcio]);
    }
    public static void Init()
    {
        playerIdList = new();
        BetTimes = new();
        BetPlayer = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        BetTimes.Add(playerId, MaxBetTimes.GetInt());
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncTotocalcioTargetAndTimes, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(BetTimes.TryGetValue(playerId, out var times) ? times : MaxBetTimes.GetInt());
        writer.Write(BetPlayer.TryGetValue(playerId, out var player) ? player : byte.MaxValue);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Times = reader.ReadInt32();
        byte Target = reader.ReadByte();
        BetTimes.Remove(PlayerId);
        BetPlayer.Remove(PlayerId);
        BetTimes.Add(PlayerId, Times);
        if (Target != byte.MaxValue)
            BetPlayer.Add(PlayerId, Target);
    }
    public static bool CanUseKillButton(PlayerControl player) => !player.Data.IsDead && (!BetTimes.TryGetValue(player.PlayerId, out var times) || times >= 1);
    public static void SetKillCooldown(byte id)
    {
        if (BetTimes.TryGetValue(id, out var times) && times < 1)
        {
            Main.AllPlayerKillCooldown[id] = 300;
            return;
        }
        float cd = BetCooldown.GetFloat();
        cd += Main.AllPlayerControls.Count(x => !x.IsAlive()) * BetCooldownIncrese.GetFloat();
        cd = Math.Min(cd, MaxBetCooldown.GetFloat());
        Main.AllPlayerKillCooldown[id] = cd;
    }
    public static bool KnowRole(PlayerControl player, PlayerControl target)
    {
        if (!KnowTargetRole.GetBool()) return false;
        return player.Is(CustomRoles.Totocalcio) && BetPlayer.TryGetValue(player.PlayerId, out var tar) && tar == target.PlayerId;
    }
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (killer.PlayerId == target.PlayerId) return true;
        if (BetPlayer.TryGetValue(killer.PlayerId, out var tar) && tar == target.PlayerId) return false;
        if (!BetTimes.TryGetValue(killer.PlayerId, out var times) || times < 1) return false;

        BetTimes[killer.PlayerId]--;
        if (BetPlayer.TryGetValue(killer.PlayerId, out var originalTarget) && Utils.GetPlayerById(originalTarget) != null)
            Utils.NotifyRoles(SpecifySeer: Utils.GetPlayerById(originalTarget));
        BetPlayer.Remove(killer.PlayerId);
        BetPlayer.Add(killer.PlayerId, target.PlayerId);
        SendRPC(killer.PlayerId);

        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        killer.RPCPlayCustomSound("Bet");

        killer.Notify(GetString("TotocalcioBetPlayer"));
        if (BetTargetKnowTotocalcio.GetBool())
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Totocalcio), GetString("TotocalcioBetOnYou")));

        Logger.Info($"赌徒下注：{killer.GetNameWithRole()} => {target.GetNameWithRole()}", "Totocalcio");
        return false;
    }
    public static string TargetMark(PlayerControl seer, PlayerControl target)
    {
        if (!seer.Is(CustomRoles.Totocalcio))
        {
            if (!BetTargetKnowTotocalcio.GetBool()) return "";
            return (BetPlayer.TryGetValue(target.PlayerId, out var x) && seer.PlayerId == x) ?
                Utils.ColorString(Utils.GetRoleColor(CustomRoles.Totocalcio), "♦") : "";
        }
        var GetValue = BetPlayer.TryGetValue(seer.PlayerId, out var targetId);
        return GetValue && targetId == target.PlayerId ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Totocalcio), "♦") : "";
    }
    public static string GetProgressText(byte playerId)
    {
        var player = Utils.GetPlayerById(playerId);
        if (player == null) return null;
        return Utils.ColorString(CanUseKillButton(player) ? Utils.GetRoleColor(CustomRoles.Totocalcio) : Color.gray, $"({(BetTimes.TryGetValue(playerId, out var times) ? times : "0")})");
    }
}