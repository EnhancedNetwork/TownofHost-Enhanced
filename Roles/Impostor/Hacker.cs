using Hazel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

public static class Hacker
{
    private static readonly int Id = 5300;
    private static List<byte> playerIdList = [];
    public static bool IsEnable = false;

    private static OptionItem HackLimitOpt;
    private static OptionItem KillCooldown;

    private static Dictionary<byte, int> HackLimit = [];
    private static List<byte> DeadBodyList = [];

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Hacker);
        KillCooldown = FloatOptionItem.Create(Id + 2, "KillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hacker])
            .SetValueFormat(OptionFormat.Seconds);
        HackLimitOpt = IntegerOptionItem.Create(Id + 4, "HackLimit", new(1, 15, 1), 3, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hacker])
            .SetValueFormat(OptionFormat.Times);
    }
    public static void Init()
    {
        playerIdList = [];
        HackLimit = [];
        DeadBodyList = [];
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        HackLimit.TryAdd(playerId, HackLimitOpt.GetInt());
        IsEnable = true;
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetHackerHackLimit, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(HackLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        HackLimit.TryAdd(PlayerId, HackLimitOpt.GetInt());
        HackLimit[PlayerId] = Limit;
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public static void ApplyGameOptions()
    {
        AURoleOptions.ShapeshifterCooldown = 1f;
        AURoleOptions.ShapeshifterDuration = 1f;
    }
    public static string GetHackLimit(byte playerId) => Utils.ColorString((HackLimit.TryGetValue(playerId, out var x) && x >= 1) ? Utils.GetRoleColor(CustomRoles.Hacker).ShadeColor(0.25f) : Color.gray, HackLimit.TryGetValue(playerId, out var hackLimit) ? $"({hackLimit})" : "Invalid");
    public static void GetAbilityButtonText(HudManager __instance, byte playerId)
    {
        if (HackLimit.TryGetValue(playerId, out var x) && x >= 1)
        {
            __instance.AbilityButton.OverrideText(GetString("HackerShapeshiftText"));
            __instance.AbilityButton.SetUsesRemaining(x);
        }
    }
    public static void OnReportDeadBody() => DeadBodyList = [];
    public static void AddDeadBody(PlayerControl target)
    {
        if (target != null && !DeadBodyList.Contains(target.PlayerId))
            DeadBodyList.Add(target.PlayerId);
    }
    public static void OnShapeshift(PlayerControl pc, bool shapeshifting, PlayerControl ssTarget)
    {
        if (!shapeshifting || !HackLimit.TryGetValue(pc.PlayerId, out var x) || x < 1 || ssTarget == null || ssTarget.Is(CustomRoles.Needy) || ssTarget.Is(CustomRoles.Lazy)) return;
        HackLimit[pc.PlayerId]--;
        SendRPC(pc.PlayerId);

        var targetId = byte.MaxValue;

        // 寻找骇客击杀的尸体
        foreach (var db in DeadBodyList)
        {
            var dp = Utils.GetPlayerById(db);
            if (dp == null || dp.GetRealKiller() == null) continue;
            if (dp.GetRealKiller().PlayerId == pc.PlayerId) targetId = db;
        }

        // 未找到骇客击杀的尸体，寻找其他尸体
        if (targetId == byte.MaxValue && DeadBodyList.Count > 0)
            targetId = DeadBodyList[IRandom.Instance.Next(0, DeadBodyList.Count)];

        if (targetId == byte.MaxValue)
            _ = new LateTask(() => ssTarget?.NoCheckStartMeeting(ssTarget?.Data), 0.15f, "Hacker Hacking Report Self");
        else
            _ = new LateTask(() => ssTarget?.NoCheckStartMeeting(Utils.GetPlayerById(targetId)?.Data), 0.15f, "Hacker Hacking Report");
    }
}