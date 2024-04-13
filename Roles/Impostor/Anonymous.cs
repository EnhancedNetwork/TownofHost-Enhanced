using AmongUs.GameOptions;
using Hazel;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Anonymous : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 5300;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorHindering;
    //==================================================================\\
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Hack");

    private static OptionItem HackLimitOpt;
    private static OptionItem KillCooldown;

    private static readonly Dictionary<byte, int> HackLimit = [];
    private static readonly List<byte> DeadBodyList = [];

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
        playerIdList.Clear();
        HackLimit.Clear();
        DeadBodyList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        HackLimit.TryAdd(playerId, HackLimitOpt.GetInt());
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        HackLimit.Remove(playerId);
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Anonymous);
        writer.Write(playerId);
        writer.Write(HackLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
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
    public override string GetProgressText(byte playerId, bool coomsd) => Utils.ColorString((HackLimit.TryGetValue(playerId, out var x) && x >= 1) ? Utils.GetRoleColor(CustomRoles.Anonymous).ShadeColor(0.25f) : Color.gray, HackLimit.TryGetValue(playerId, out var hackLimit) ? $"({hackLimit})" : "Invalid");
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));

        if (HackLimit.TryGetValue(playerId, out var x) && x >= 1)
        {
            hud.AbilityButton.OverrideText(GetString("AnonymousShapeshiftText"));
            hud.AbilityButton.SetUsesRemaining(x);
        }
    }
    public override void OnReportDeadBody(PlayerControl reporter, PlayerControl target) => DeadBodyList.Clear();
    public override void OnMurderPlayerAsKiller(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (inMeeting || isSuicide) return;

        if (target != null && !DeadBodyList.Contains(target.PlayerId))
            DeadBodyList.Add(target.PlayerId);
    }
    public override void OnShapeshift(PlayerControl shapeshifter, PlayerControl ssTarget, bool IsAnimate, bool shapeshifting)
    {
        if (!shapeshifting || !HackLimit.TryGetValue(shapeshifter.PlayerId, out var x) || x < 1 || ssTarget == null || ssTarget.Is(CustomRoles.LazyGuy) || ssTarget.Is(CustomRoles.Lazy) || ssTarget.Is(CustomRoles.NiceMini) && Mini.Age < 18) return;
        HackLimit[shapeshifter.PlayerId]--;
        SendRPC(shapeshifter.PlayerId);

        var targetId = byte.MaxValue;

        // Finding real killer
        foreach (var db in DeadBodyList)
        {
            var dp = Utils.GetPlayerById(db);
            if (dp == null || dp.GetRealKiller() == null) continue;
            if (dp.GetRealKiller().PlayerId == shapeshifter.PlayerId) targetId = db;
        }

        // No body found. Look for another body
        if (targetId == byte.MaxValue && DeadBodyList.Any())
            targetId = DeadBodyList[IRandom.Instance.Next(0, DeadBodyList.Count)];

        // Anonymous report Self
        if (targetId == byte.MaxValue)
            _ = new LateTask(() => ssTarget?.NoCheckStartMeeting(ssTarget?.Data), 0.15f, "Anonymous Hacking Report Self");
        else
            _ = new LateTask(() => ssTarget?.NoCheckStartMeeting(Utils.GetPlayerById(targetId)?.Data), 0.15f, "Anonymous Hacking Report");
    }
}