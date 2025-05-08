using AmongUs.GameOptions;
using Hazel;
using UnityEngine;

namespace TOHE.Roles.Impostor;

internal class Undertaker : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Undertaker;
    private const int Id = 4900;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\

    private static OptionItem SSCooldown;
    private static OptionItem KillCooldown;
    private static OptionItem FreezeTime;

    private static readonly Dictionary<byte, Vector2> MarkedLocation = [];

    private static float DefaultSpeed = new();

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Undertaker);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Undertaker])
            .SetValueFormat(OptionFormat.Seconds);
        SSCooldown = FloatOptionItem.Create(Id + 11, GeneralOption.ShapeshifterBase_ShapeshiftCooldown, new(0f, 180f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Undertaker])
            .SetValueFormat(OptionFormat.Seconds);
        FreezeTime = FloatOptionItem.Create(Id + 13, "UndertakerFreezeDuration", new(1f, 5f, 1f), 5, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Undertaker])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Init()
    {
        MarkedLocation.Clear();
        DefaultSpeed = new();
    }

    public override void Add(byte playerId)
    {
        MarkedLocation.TryAdd(playerId, ExtendedPlayerControl.GetBlackRoomPosition());
        DefaultSpeed = Main.AllPlayerSpeed[playerId];
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = SSCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.UndertakerLocationSync, SendOption.Reliable, -1);
        writer.Write(playerId);
        var xLoc = MarkedLocation[playerId].x;
        writer.Write(xLoc);
        var yLoc = MarkedLocation[playerId].y;
        writer.Write(yLoc);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        float xLoc = reader.ReadSingle();
        float yLoc = reader.ReadSingle();

        if (MarkedLocation.ContainsKey(PlayerId))
            MarkedLocation[PlayerId] = new Vector2(xLoc, yLoc);
        else
            MarkedLocation.Add(PlayerId, ExtendedPlayerControl.GetBlackRoomPosition());
    }
    public override void UnShapeShiftButton(PlayerControl shapeshifter)
    {
        var shapeshifterId = shapeshifter.PlayerId;
        MarkedLocation[shapeshifterId] = shapeshifter.GetCustomPosition();
        SendRPC(shapeshifterId);

        shapeshifter.Notify(Translator.GetString("RejectShapeshift.AbilityWasUsed"), time: 2f);
    }

    private static void FreezeUndertaker(PlayerControl player)
    {
        Main.AllPlayerSpeed[player.PlayerId] = Main.MinSpeed;
        ReportDeadBodyPatch.CanReport[player.PlayerId] = false;
        player.MarkDirtySettings();

        _ = new LateTask(() =>
        {
            Main.AllPlayerSpeed[player.PlayerId] = DefaultSpeed;
            ReportDeadBodyPatch.CanReport[player.PlayerId] = true;
            player.MarkDirtySettings();
        }, FreezeTime.GetFloat(), "Freeze Undertaker");
    }

    private static bool HasMarkedLoc(byte playerId) => MarkedLocation.TryGetValue(playerId, out var pos) && pos != ExtendedPlayerControl.GetBlackRoomPosition();

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!HasMarkedLoc(killer.PlayerId)) return true;
        if (target.Is(CustomRoles.Bait)) return true;

        if (target.CanBeTeleported())
        {
            var tempPos = MarkedLocation[killer.PlayerId];
            target.RpcTeleport(tempPos);
            killer.SetKillCooldown();

            target.RpcMurderPlayer(target);
            target.SetRealKiller(killer);

            MarkedLocation[killer.PlayerId] = ExtendedPlayerControl.GetBlackRoomPosition();

            SendRPC(killer.PlayerId);
            FreezeUndertaker(killer);

            killer.SyncSettings();
        }
        return false;
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        foreach (var playerId in MarkedLocation.Keys)
        {
            MarkedLocation[playerId] = ExtendedPlayerControl.GetBlackRoomPosition();
            Main.AllPlayerSpeed[playerId] = DefaultSpeed;
            SendRPC(playerId);
        }
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton?.OverrideText(Translator.GetString("KillButtonText"));
        hud.AbilityButton?.OverrideText(Translator.GetString("MarkButtonText"));
    }
}

