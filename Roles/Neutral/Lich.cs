using AmongUs.GameOptions;
using Hazel;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Lich : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Lich;
    private const int Id = 32100;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Lich);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralApocalypse;
    //==================================================================\\
    private static OptionItem LichPointsOpt;
    private static OptionItem GetPassiveCharges;
    public static OptionItem LichCanVent;
    private static OptionItem LichHasImpostorVision;

    private static byte TargetId;
    private static PlayerControl LichPlayer;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Lich, 1, zeroOne: false);
        LichPointsOpt = IntegerOptionItem.Create(Id + 10, "LichPointsToWin", new(1, 14, 1), 3, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lich])
            .SetValueFormat(OptionFormat.Times);
        GetPassiveCharges = BooleanOptionItem.Create(Id + 11, "GetPassiveCharges", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lich]);
        LichCanVent = BooleanOptionItem.Create(Id + 12, "LichCanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lich]);
        // SoulCollector.DeathMeetingTimeIncrease = IntegerOptionItem.Create(Id + 13, "DeathMeetingTimeIncrease", new(0, 120, 1), 0, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SoulCollector])
        //     .SetValueFormat(OptionFormat.Seconds);
        LichHasImpostorVision = BooleanOptionItem.Create(Id + 14, "LichHasImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lich]);
    }

    public override void Init()
    {
        TargetId = byte.MaxValue;
    }

    public override void Add(byte playerId)
    {
        TargetId = byte.MaxValue;
        playerId.SetAbilityUseLimit(0);
        LichPlayer = _Player;
    }

    public override void Remove(byte playerId)
    {
        LichPlayer = null;
    }

    public override string GetProgressText(byte playerId, bool comms) => Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCollector).ShadeColor(0.25f), $"({playerId.GetAbilityUseLimit()}/{LichPointsOpt.GetInt()})");

    private void SendRPC()
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(TargetId);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte target = reader.ReadByte();

        TargetId = target;
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
        => TargetId == seen.PlayerId ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lich), "🜏") : string.Empty;

    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (_Player == null) return string.Empty;
        if (TargetId == target.PlayerId && seer.IsNeutralApocalypse() && seer.PlayerId != _Player.PlayerId && !Main.PlayerStates[seer.PlayerId].IsNecromancer)
        {
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lich), "🜏");
        }
        return string.Empty;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
        => opt.SetVision(LichHasImpostorVision.GetBool());
    public override bool CanUseKillButton(PlayerControl pc) => pc.Is(CustomRoles.Lich);
    public override bool CanUseImpostorVentButton(PlayerControl pc) => LichCanVent.GetBool();
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        if (TargetId != byte.MaxValue)
        {
            killer.Notify(string.Format(GetString("LichTargetUsed"), target.GetRealName()));
        }
        else
        {
            killer.Notify(string.Format(GetString("LichTarget"), target.GetRealName()));
        }
        TargetId = target.PlayerId;
        SendRPC();
        Logger.Info($"{killer.GetNameWithRole()} cursed {target.GetNameWithRole()}", "Lich");
        return false;
    }
    public override void OnReportDeadBody(PlayerControl player, NetworkedPlayerInfo netInf)
    {
        if (_Player == null || !_Player.IsAlive() || !GetPassiveCharges.GetBool()) return;

        _Player.RpcIncreaseAbilityUseLimitBy(1);
    }
    public override void OnMeetingHudStart(PlayerControl pc)
    {
        if (!pc.IsAlive() || !GetPassiveCharges.GetBool()) return;

        MeetingHudStartPatch.AddMsg(GetString("PassiveChargeGained"), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lich), GetString("Lich").ToUpper()));
    }

    private void OnTargetVote()
    {
        if (_Player == null || !_Player.IsAlive()) return;
        if (TargetId == byte.MaxValue) return;

        var playerId = _Player.PlayerId;

        TargetId = byte.MaxValue;
        _Player.RpcIncreaseAbilityUseLimitBy(1);

        SendRPC();
        _Player.Notify(GetString("LichChargeGained"));
    }   

    public static void OnTargetVote(PlayerControl target)
    {
        if (LichPlayer == null || !LichPlayer.IsAlive()) return;

        if (target.PlayerId == TargetId)
            (LichPlayer.GetRoleClass() as Lich).OnTargetVote();
    }

    public static bool IsCursed(PlayerControl player) => player.PlayerId == TargetId;

    public override void AfterMeetingTasks()
    {
        if (_Player == null || !_Player.IsAlive()) return;
        TargetId = byte.MaxValue;
        SendRPC();

        var player = _Player;
        if (player.GetAbilityUseLimit() >= LichPointsOpt.GetInt() && !player.Is(CustomRoles.Death))
        {
            player.RpcSetCustomRole(CustomRoles.Death);
            player.GetRoleClass()?.OnAdd(player.PlayerId);

            player.Notify(GetString("SoulCollectorToDeath"));
            player.RpcGuardAndKill(player);
        }
    }
}