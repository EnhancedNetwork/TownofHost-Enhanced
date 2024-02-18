using Hazel;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TOHE.Roles.Impostor;

internal static class QuickShooter
{
    private static readonly int Id = 2200;
    public static List<byte> playerIdList = [];
    public static bool IsEnable = false;

    private static OptionItem KillCooldown;
    private static OptionItem MeetingReserved;
    public static OptionItem ShapeshiftCooldown;

    public static Dictionary<byte, int> ShotLimit = [];

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.QuickShooter);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 35f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.QuickShooter])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 12, "QuickShooterShapeshiftCooldown", new(0f, 180f, 2.5f), 15f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.QuickShooter])
            .SetValueFormat(OptionFormat.Seconds);
        MeetingReserved = IntegerOptionItem.Create(Id + 14, "MeetingReserved", new(0, 15, 1), 2, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.QuickShooter])
            .SetValueFormat(OptionFormat.Pieces);
    }
    public static void Init()
    {
        playerIdList = [];
        ShotLimit = [];
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        ShotLimit.TryAdd(playerId, 0);
        IsEnable = true;
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.QuickShooter);
        writer.Write(playerId);
        writer.Write(ShotLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte QuickShooterId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        ShotLimit.TryAdd(QuickShooterId, Limit);
        ShotLimit[QuickShooterId] = Limit;
    }
    public static void OnShapeshift(PlayerControl pc, bool shapeshifting)
    {
        if (pc.killTimer == 0 && shapeshifting)
        {
            ShotLimit[pc.PlayerId]++;
            SendRPC(pc.PlayerId);
            Storaging = true;
            pc.ResetKillCooldown();
            pc.SetKillCooldown();
            pc.Notify(Translator.GetString("QuickShooterStoraging"));
            Logger.Info($"{Utils.GetPlayerById(pc.PlayerId)?.GetNameWithRole()} : 残り{ShotLimit[pc.PlayerId]}発", "QuickShooter");
        }
    }
    private static bool Storaging;
    public static void SetKillCooldown(byte id)
    {
        Main.AllPlayerKillCooldown[id] = (Storaging || ShotLimit[id] < 1) ? KillCooldown.GetFloat() : 0.01f;
        Storaging = false;
    }
    public static void OnReportDeadBody()
    {
        Dictionary<byte, int> NewSL = [];

        foreach (var sl in ShotLimit)
            NewSL.Add(sl.Key, Math.Clamp(sl.Value, 0, MeetingReserved.GetInt()));

        foreach (var sl in NewSL)
        {
            ShotLimit[sl.Key] = sl.Value;
            SendRPC(sl.Key);
        }
    }
    public static void QuickShooterKill(PlayerControl killer)
    {
        ShotLimit.TryAdd(killer.PlayerId, 0);
        ShotLimit[killer.PlayerId]--;
        ShotLimit[killer.PlayerId] = Math.Max(ShotLimit[killer.PlayerId], 0);
        SendRPC(killer.PlayerId);
    }
    public static string GetShotLimit(byte playerId) => Utils.ColorString(ShotLimit.ContainsKey(playerId) && ShotLimit[playerId] > 0 ? Utils.GetRoleColor(CustomRoles.QuickShooter).ShadeColor(0.25f) : Color.gray, ShotLimit.TryGetValue(playerId, out var shotLimit) ? $"({shotLimit})" : "Invalid");
}
