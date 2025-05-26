using AmongUs.GameOptions;
using Hazel;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class SoulCollector : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.SoulCollector;
    private const int Id = 15300;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.SoulCollector);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralApocalypse;
    //==================================================================\\

    private static OptionItem SoulCollectorPointsOpt;
    private static OptionItem GetPassiveSouls;
    public static OptionItem SoulCollectorCanVent;
    private static OptionItem SoulCollectorHasImpostorVision;
    public static OptionItem DeathMeetingTimeIncrease;

    private byte TargetId;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.SoulCollector, 1, zeroOne: false);
        SoulCollectorPointsOpt = IntegerOptionItem.Create(Id + 10, "SoulCollectorPointsToWin", new(1, 14, 1), 3, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SoulCollector])
            .SetValueFormat(OptionFormat.Times);
        GetPassiveSouls = BooleanOptionItem.Create(Id + 11, "GetPassiveSouls", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SoulCollector]);
        SoulCollectorCanVent = BooleanOptionItem.Create(Id + 12, "SoulCollectorCanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SoulCollector]);
        DeathMeetingTimeIncrease = IntegerOptionItem.Create(Id + 13, "DeathMeetingTimeIncrease", new(0, 120, 1), 0, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SoulCollector])
            .SetValueFormat(OptionFormat.Seconds);
        SoulCollectorHasImpostorVision = BooleanOptionItem.Create(Id + 14, "SoulCollectorHasImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SoulCollector]);
    }
    public override void Init()
    {
        TargetId = byte.MaxValue;
    }

    public override void Add(byte playerId)
    {
        TargetId = byte.MaxValue;
        playerId.SetAbilityUseLimit(0);

        CustomRoleManager.CheckDeadBodyOthers.Add(OnPlayerDead);
    }

    public override string GetProgressText(byte playerId, bool comms) => Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCollector).ShadeColor(0.25f), $"({playerId.GetAbilityUseLimit()}/{SoulCollectorPointsOpt.GetInt()})");

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
        => TargetId == seen.PlayerId ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCollector), "♠") : string.Empty;

    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (_Player == null) return string.Empty;
        if (TargetId == target.PlayerId && seer.IsNeutralApocalypse() && seer.PlayerId != _Player.PlayerId && !Main.PlayerStates[seer.PlayerId].IsNecromancer)
        {
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCollector), "♠");
        }
        return string.Empty;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
        => opt.SetVision(SoulCollectorHasImpostorVision.GetBool());
    public override bool CanUseKillButton(PlayerControl pc) => pc.Is(CustomRoles.SoulCollector);
    public override bool CanUseImpostorVentButton(PlayerControl pc) => SoulCollectorCanVent.GetBool();
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        if (TargetId != byte.MaxValue)
        {
            killer.Notify(GetString("SoulCollectorTargetUsed"));
            return false;
        }
        TargetId = target.PlayerId;
        SendRPC();
        Logger.Info($"{killer.GetNameWithRole()} predicted the death of {target.GetNameWithRole()}", "SoulCollector");
        killer.Notify(string.Format(GetString("SoulCollectorTarget"), target.GetRealName()));
        return false;
    }
    public override void OnReportDeadBody(PlayerControl ryuak, NetworkedPlayerInfo iscute)
    {
        if (_Player == null || !_Player.IsAlive() || !GetPassiveSouls.GetBool()) return;

        _Player.RpcIncreaseAbilityUseLimitBy(1);
    }
    public override void OnMeetingHudStart(PlayerControl pc)
    {
        if (!pc.IsAlive() || !GetPassiveSouls.GetBool()) return;

        MeetingHudStartPatch.AddMsg(GetString("PassiveSoulGained"), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCollector), GetString("SoulCollector").ToUpper()));
    }
    private void OnPlayerDead(PlayerControl killer, PlayerControl deadPlayer, bool inMeeting)
    {
        if (_Player == null || !_Player.IsAlive()) return;
        if (TargetId == byte.MaxValue) return;

        var playerId = _Player.PlayerId;
        Main.PlayerStates.TryGetValue(TargetId, out var playerState);
        if (TargetId == deadPlayer.PlayerId && playerState.IsDead && !playerState.Disconnected)
        {
            TargetId = byte.MaxValue;
            _Player.RpcIncreaseAbilityUseLimitBy(1);

            if (inMeeting)
            {
                _ = new LateTask(() =>
                {
                    Utils.SendMessage(GetString("SoulCollectorMeetingDeath"), playerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCollector), GetString("SoulCollector").ToUpper()));

                }, 3f, "Soul Collector Meeting Death");
            }

            SendRPC();
            _Player.Notify(GetString("SoulCollectorSoulGained"));
        }
        if (_Player.GetAbilityUseLimit() >= SoulCollectorPointsOpt.GetInt() && !inMeeting)
        {
            PlayerControl sc = _Player;

            sc.RpcSetCustomRole(CustomRoles.Death);
            sc.GetRoleClass()?.OnAdd(sc.PlayerId);

            sc.Notify(GetString("SoulCollectorToDeath"));
            sc.RpcGuardAndKill(sc);
        }
    }
    public override void AfterMeetingTasks()
    {
        if (_Player == null || !_Player.IsAlive()) return;
        TargetId = byte.MaxValue;
        SendRPC();

        var player = _Player;
        if (player.GetAbilityUseLimit() >= SoulCollectorPointsOpt.GetInt() && !player.Is(CustomRoles.Death))
        {
            player.RpcSetCustomRole(CustomRoles.Death);
            player.GetRoleClass()?.OnAdd(player.PlayerId);

            player.Notify(GetString("SoulCollectorToDeath"));
            player.RpcGuardAndKill(player);
        }
    }
}
internal class Death : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Death;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Death);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralApocalypse;
    //==================================================================\\

    public override bool CanUseImpostorVentButton(PlayerControl pc) => SoulCollector.SoulCollectorCanVent.GetBool();
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target) => false;

    public override void OnCheckForEndVoting(PlayerState.DeathReason deathReason, params byte[] exileIds)
    {
        if (_Player == null || exileIds == null || exileIds.Contains(_Player.PlayerId)) return;

        var deathList = new List<byte>();
        var death = _Player;
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.IsNeutralApocalypse() && !Main.PlayerStates[pc.PlayerId].IsNecromancer) continue;
            if (death.IsAlive())
            {
                if (!Main.AfterMeetingDeathPlayers.ContainsKey(pc.PlayerId))
                {
                    pc.SetRealKiller(death);
                    deathList.Add(pc.PlayerId);
                }
            }
            else
            {
                Main.AfterMeetingDeathPlayers.Remove(pc.PlayerId);
            }
        }
        CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Armageddon, [.. deathList]);
    }
    public override void CheckExileTarget(NetworkedPlayerInfo exiled, ref bool DecidedWinner, bool isMeetingHud, ref string name)
    {
        if (exiled == null) return;
        var sc = Utils.GetPlayerListByRole(CustomRoles.Death).FirstOrDefault();
        if (sc == null || !sc.IsAlive() || sc.Data.Disconnected) return;

        if (isMeetingHud)
        {
            if (exiled.PlayerId == sc.PlayerId)
            {
                name = string.Format(GetString("ExiledDeath"), Main.LastVotedPlayer, Utils.GetDisplayRoleAndSubName(exiled.PlayerId, exiled.PlayerId, false, true));
            }
            else
            {
                name = string.Format(GetString("ExiledNotDeath"), Main.LastVotedPlayer, Utils.GetDisplayRoleAndSubName(exiled.PlayerId, exiled.PlayerId, false, true));
            }
        }
    }
}
