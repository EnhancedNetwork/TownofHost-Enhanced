using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using TOHE.Modules;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

public static class Romantic
{
    private static readonly int Id = 13500;
    public static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    private static readonly int MaxBetTimes = 1;
    public static bool isProtect = false;
    public static bool isRomanticAlive = true;
    public static bool isPartnerProtected = false;

    public static OptionItem BetCooldown;
    private static OptionItem ProtectCooldown;
    private static OptionItem ProtectDuration;
    private static OptionItem KnowTargetRole;
    private static OptionItem BetTargetKnowRomantic;
    public static OptionItem VengefulKCD;
    public static OptionItem VengefulCanVent;
    public static OptionItem RuthlessKCD;
    public static OptionItem RuthlessCanVent;

    private static Dictionary<byte, int> BetTimes = new();
    public static Dictionary<byte, byte> BetPlayer = new();

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Romantic, 1, zeroOne: false);
        BetCooldown = FloatOptionItem.Create(Id + 10, "RomanticBetCooldown", new(0f, 60f, 1f), 7f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Romantic])
            .SetValueFormat(OptionFormat.Seconds);
        ProtectCooldown = FloatOptionItem.Create(Id + 11, "RomanticProtectCooldown", new(0f, 60f, 2.5f), 25f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Romantic])
            .SetValueFormat(OptionFormat.Seconds);
        ProtectDuration = FloatOptionItem.Create(Id + 12, "RomanticProtectDuration", new(0f, 60f, 2.5f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Romantic])
            .SetValueFormat(OptionFormat.Seconds);
        KnowTargetRole = BooleanOptionItem.Create(Id + 13, "RomanticKnowTargetRole", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Romantic]);
        BetTargetKnowRomantic = BooleanOptionItem.Create(Id + 14, "RomanticBetTargetKnowRomantic", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Romantic]);
        VengefulKCD = FloatOptionItem.Create(Id + 15, "VengefulKCD", new(0f, 60f, 2.5f), 22.5f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Romantic])
            .SetValueFormat(OptionFormat.Seconds);
        VengefulCanVent = BooleanOptionItem.Create(Id + 16, "VengefulCanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Romantic]);
        RuthlessKCD = FloatOptionItem.Create(Id + 17, "RuthlessKCD", new(0f, 60f, 2.5f), 22.5f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Romantic])
            .SetValueFormat(OptionFormat.Seconds);
        RuthlessCanVent = BooleanOptionItem.Create(Id + 18, "RuthlessCanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Romantic]);
    }
    public static void Init()
    {
        playerIdList = new();
        BetTimes = new();
        BetPlayer = new();
        isProtect = false;
        isPartnerProtected = false;
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        BetTimes.Add(playerId, MaxBetTimes);
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRomanticTarget, SendOption.Reliable, -1);
        writer.Write(playerId);
        //writer.Write(BetTimes.TryGetValue(playerId, out var times) ? times : MaxBetTimes);
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
    public static bool CanUseKillButton(PlayerControl player) => !player.Data.IsDead;
    public static void SetKillCooldown(byte id)
    {
        if (BetTimes.TryGetValue(id, out var times) && times < 1)
        {
            Main.AllPlayerKillCooldown[id] = ProtectCooldown.GetFloat();
            return;
        }
        else Main.AllPlayerKillCooldown[id] = BetCooldown.GetFloat();
        //float cd = BetCooldown.GetFloat();
        //cd += Main.AllPlayerControls.Count(x => !x.IsAlive()) * BetCooldownIncrese.GetFloat();
        //cd = Math.Min(cd, MaxBetCooldown.GetFloat());
        //Main.AllPlayerKillCooldown[id] = cd;
    }
    public static bool KnowRole(PlayerControl player, PlayerControl target)
    {
        if (!KnowTargetRole.GetBool()) return false;
        return player.Is(CustomRoles.Romantic) && BetPlayer.TryGetValue(player.PlayerId, out var tar) && tar == target.PlayerId;
    }
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (killer.PlayerId == target.PlayerId) return true;
        if (Mini.Age < 18 && (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Succubus), GetString("CantRecruit")));
            return false;
        }
        //if (BetPlayer.TryGetValue(killer.PlayerId, out var tar) && tar == target.PlayerId) return false;
        if (!BetTimes.TryGetValue(killer.PlayerId, out var times) || times < 1) isProtect = true;

        if (!isProtect)
        {
            BetTimes[killer.PlayerId]--;

            if (BetPlayer.TryGetValue(killer.PlayerId, out var originalTarget) && Utils.GetPlayerById(originalTarget) != null)
            {
                Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: Utils.GetPlayerById(originalTarget), ForceLoop: true);
                Utils.NotifyRoles(SpecifySeer: Utils.GetPlayerById(originalTarget), SpecifyTarget: killer, ForceLoop: true);
            }

            BetPlayer.Remove(killer.PlayerId);
            BetPlayer.Add(killer.PlayerId, target.PlayerId);
            SendRPC(killer.PlayerId);

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            killer.RPCPlayCustomSound("Bet");

            killer.Notify(GetString("RomanticBetPlayer"));
            if (BetTargetKnowRomantic.GetBool())
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Romantic), GetString("RomanticBetOnYou")));

            Logger.Info($"赌徒下注：{killer.GetNameWithRole()} => {target.GetNameWithRole()}", "Romantic");
        }
        else
        {
            if (BetPlayer.TryGetValue(killer.PlayerId, out var originalTarget))
            {
                var tpc = Utils.GetPlayerById(originalTarget);
                isPartnerProtected = true;
                killer.ResetKillCooldown();
                killer.SetKillCooldown();
                killer.RPCPlayCustomSound("Shield");
                killer.Notify(GetString("RomanticProtectPartner"));
                tpc.Notify(GetString("RomanticIsProtectingYou"));
                _ = new LateTask(() =>
                {
                    if (!GameStates.IsInTask || !tpc.IsAlive()) return;
                    isPartnerProtected = false;
                    killer.Notify("ProtectingOver");
                    tpc.Notify("ProtectingOver");
                    killer.SetKillCooldown();
                }, ProtectDuration.GetFloat());
            }
        }

        return false;
    }
    public static string TargetMark(PlayerControl seer, PlayerControl target)
    {
        if (!seer.Is(CustomRoles.Romantic))
        {
            if (!BetTargetKnowRomantic.GetBool()) return "";
            return (BetPlayer.TryGetValue(target.PlayerId, out var x) && seer.PlayerId == x) ?
                Utils.ColorString(Utils.GetRoleColor(CustomRoles.Romantic), "♥") : "";
        }
        var GetValue = BetPlayer.TryGetValue(seer.PlayerId, out var targetId);
        return GetValue && targetId == target.PlayerId ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Romantic), "♥") : "";
    }
    public static string GetProgressText(byte playerId)
    {
        var player = Utils.GetPlayerById(playerId);
        if (player == null) return null;
        return Utils.ColorString(BetTimes.TryGetValue(playerId, out var timesV1) && timesV1 >= 1 ? Color.white : Utils.GetRoleColor(CustomRoles.Romantic), $"<color=#ffffff>-</color> {(BetTimes.TryGetValue(playerId, out var timesV2) && timesV2 >= 1 && timesV2 >= 1 ? "♡" : "♥")}");
    }
    public static void OnReportDeadBody()
    {
        isPartnerProtected = false;
    }
    public static void ChangeRole(byte playerId)
    {
        var player = Utils.GetPlayerById(playerId);
        if (player == null) return;
        byte Romantic = 0x73;
        BetPlayer.Do(x =>
        {
            if (x.Value == playerId)
                Romantic = x.Key;
        });
        if (Romantic == 0x73) return;
        var pc = Utils.GetPlayerById(Romantic);
        if (player.GetCustomRole().IsImpostorTeamV3())
        {
            Logger.Info($"Impostor Romantic Partner Died changing {pc.GetNameWithRole()} to Refugee", "Romantic");
            pc.RpcSetCustomRole(CustomRoles.Refugee);
        }
        else if (player.IsNeutralKiller())
        {
            Logger.Info($"Neutral Romantic Partner Died changing {pc.GetNameWithRole()} to Ruthless Romantic", "Romantic");
            RuthlessRomantic.Add(Romantic);
            pc.RpcSetCustomRole(CustomRoles.RuthlessRomantic);
        }
        else
        {
            _ = new LateTask(() =>
            {
                Logger.Info($"Crew/nnk Romantic Partner Died changing {pc.GetNameWithRole()} to Vengeful romantic", "Romantic");

                var killerId = player.GetRealKiller().PlayerId;
                VengefulRomantic.Add(pc.PlayerId, killerId);
                VengefulRomantic.SendRPC(pc.PlayerId);
                pc.RpcSetCustomRole(CustomRoles.VengefulRomantic);
            }, 0.2f, "Convert to Vengeful Romantic");
        }

        Utils.GetPlayerById(Romantic).ResetKillCooldown();
        Utils.GetPlayerById(Romantic).SetKillCooldown();
    }
}

public static class VengefulRomantic
{
    public static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    public static bool hasKilledKiller = false;
    public static Dictionary<byte, byte> VengefulTarget = new();

    public static void Init()
    {
        playerIdList = new();
        VengefulTarget = new();
        hasKilledKiller = false;
        IsEnable = false;
    }
    public static void Add(byte playerId, byte killerId = byte.MaxValue)
    {
        playerIdList.Add(playerId);
        VengefulTarget.Add(playerId, killerId);
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    public static bool CanUseKillButton(PlayerControl player) => !player.Data.IsDead && !hasKilledKiller;

    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (killer.PlayerId == target.PlayerId) return true; //set it to true coz shaman can do this, and killer shd die

        if (VengefulTarget.TryGetValue(killer.PlayerId, out var PartnerKiller) && target.PlayerId == PartnerKiller)
        {
            hasKilledKiller = true;
            return true;
        }
        else
        {
            killer.RpcMurderPlayerV3(killer);
            Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Misfire;
            return false;
        }
    }
    public static string GetProgressText(byte playerId)
    {
        var player = Utils.GetPlayerById(playerId);
        if (player == null) return null;
        return Utils.ColorString(hasKilledKiller ? Color.green : Utils.GetRoleColor(CustomRoles.VengefulRomantic), $"<color=#777777>-</color> {((hasKilledKiller) ? "♥" : "♡")}");
    }
    public static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncVengefulRomanticTarget, SendOption.Reliable, -1);
        writer.Write(playerId);
        //writer.Write(BetTimes.TryGetValue(playerId, out var times) ? times : MaxBetTimes);
        writer.Write(VengefulTarget.TryGetValue(playerId, out var player) ? player : byte.MaxValue);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        byte Target = reader.ReadByte();
        VengefulTarget.Remove(PlayerId);
        if (Target != byte.MaxValue)
            VengefulTarget.Add(PlayerId, Target);
    }
}

public static class RuthlessRomantic
{
    public static List<byte> playerIdList = new();
    public static bool IsEnable = false;
    public static void Init()
    {
        playerIdList = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
}
