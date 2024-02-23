using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Anonymous : RoleBase
{
    private const int Id = 5300;
    private static List<byte> playerIdList = [];
    
    public static bool On;
    public override bool IsEnable => On;

    private static OptionItem HackLimitOpt;
    private static OptionItem KillCooldown;

    private static Dictionary<byte, int> HackLimit = [];
    private static List<byte> DeadBodyList = [];

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Anonymous);
        KillCooldown = FloatOptionItem.Create(Id + 2, "KillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Anonymous])
            .SetValueFormat(OptionFormat.Seconds);
        HackLimitOpt = IntegerOptionItem.Create(Id + 4, "HackLimit", new(1, 15, 1), 3, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Anonymous])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        playerIdList = [];
        HackLimit = [];
        DeadBodyList = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        HackLimit.TryAdd(playerId, HackLimitOpt.GetInt());
        On = true;
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Anonymous);
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
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = 1f;
        AURoleOptions.ShapeshifterDuration = 1f;
    }
    public static string GetHackLimit(byte playerId) => Utils.ColorString((HackLimit.TryGetValue(playerId, out var x) && x >= 1) ? Utils.GetRoleColor(CustomRoles.Anonymous).ShadeColor(0.25f) : Color.gray, HackLimit.TryGetValue(playerId, out var hackLimit) ? $"({hackLimit})" : "Invalid");
    public override void SetAbilityButtonText(HudManager __instance, byte playerId)
    {
        __instance.ReportButton.OverrideText(GetString("ReportButtonText"));

        if (HackLimit.TryGetValue(playerId, out var x) && x >= 1)
        {
            __instance.AbilityButton.OverrideText(GetString("AnonymousShapeshiftText"));
            __instance.AbilityButton.SetUsesRemaining(x);
        }
    }
    public override void OnReportDeadBody(PlayerControl reporter, PlayerControl target) => DeadBodyList = [];
    public override void OnMurder(PlayerControl killer, PlayerControl target)
    {
        if (target != null && !DeadBodyList.Contains(target.PlayerId))
            DeadBodyList.Add(target.PlayerId);
    }
    public override void OnShapeshift(PlayerControl pc, PlayerControl ssTarget, bool shapeshifting, bool shapeshiftIsHidden)
    {
        if (!shapeshifting || !HackLimit.TryGetValue(pc.PlayerId, out var x) || x < 1 || ssTarget == null || ssTarget.Is(CustomRoles.Needy) || ssTarget.Is(CustomRoles.Lazy) || ssTarget.Is(CustomRoles.NiceMini) && Mini.Age < 18) return;
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
            _ = new LateTask(() => ssTarget?.NoCheckStartMeeting(ssTarget?.Data), 0.15f, "Anonymous Hacking Report Self");
        else
            _ = new LateTask(() => ssTarget?.NoCheckStartMeeting(Utils.GetPlayerById(targetId)?.Data), 0.15f, "Anonymous Hacking Report");
    }
}