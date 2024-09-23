using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class SoulCollector : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 15300;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.SoulCollector);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralApocalypse;
    //==================================================================\\

    private static OptionItem SoulCollectorPointsOpt;
    private static OptionItem GetPassiveSouls;
    public static OptionItem SoulCollectorCanVent;
    public static OptionItem DeathMeetingTimeIncrease;

    private byte TargetId;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.SoulCollector, 1, zeroOne: false);
        SoulCollectorPointsOpt = IntegerOptionItem.Create(Id + 10, "SoulCollectorPointsToWin", new(1, 14, 1), 3, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SoulCollector])
            .SetValueFormat(OptionFormat.Times);
        GetPassiveSouls = BooleanOptionItem.Create(Id + 12, "GetPassiveSouls", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SoulCollector]);
        SoulCollectorCanVent = BooleanOptionItem.Create(Id + 13, "SoulCollectorCanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SoulCollector]);
        DeathMeetingTimeIncrease = IntegerOptionItem.Create(Id + 14, "DeathMeetingTimeIncrease", new(0, 120, 1), 0, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SoulCollector])
            .SetValueFormat(OptionFormat.Seconds);
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

    public override string GetProgressText(byte playerId, bool cooms) => Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCollector).ShadeColor(0.25f),  $"({playerId.GetAbilityUseLimit()}/{SoulCollectorPointsOpt.GetInt()})");
    public override void SetAbilityButtonText(HudManager hud, byte playerId) => hud.KillButton.OverrideText(GetString("SoulCollectorKillButtonText"));
    
    private void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable);
        writer.WriteNetObject(_Player);
        writer.Write(TargetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte target = reader.ReadByte();
        TargetId =  target;
    }
    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) => KnowRoleTarget(seer, target);
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target)
        => (target.IsNeutralApocalypse() && seer.IsNeutralApocalypse());
    
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
        => TargetId == seen.PlayerId ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCollector), "♠") : string.Empty;
    
    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (TargetId == target.PlayerId && seer.IsNeutralApocalypse() && seer.PlayerId != _Player.PlayerId)
        {
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCollector), "♠");
        }
        return string.Empty;
    }
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
        if (!_Player.IsAlive() || !GetPassiveSouls.GetBool()) return;

        _Player.RpcIncreaseAbilityUseLimitBy(1);
        _ = new LateTask(() =>
        {
            Utils.SendMessage(GetString("PassiveSoulGained"), _Player.PlayerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCollector), GetString("SoulCollectorTitle")));

        }, 3f, "Passive Soul Gained");
    }
    private void OnPlayerDead(PlayerControl killer, PlayerControl deadPlayer, bool inMeeting)
    {
        if (!_Player.IsAlive()) return;
        if (TargetId == byte.MaxValue) return;

        var playerId = _Player.PlayerId;
        Main.PlayerStates.TryGetValue(TargetId, out var playerState);
        if (TargetId == deadPlayer.PlayerId && playerState.IsDead && !playerState.Disconnected)
        {
            TargetId = byte.MaxValue;
            _Player.RpcIncreaseAbilityUseLimitBy(1);
            if (GameStates.IsMeeting)
            {
                _ = new LateTask(() =>
                {
                    Utils.SendMessage(GetString("SoulCollectorMeetingDeath"), playerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCollector), GetString("SoulCollectorTitle")));

                }, 3f, "Soul Collector Meeting Death");
            }

            SendRPC();
            _Player.Notify(GetString("SoulCollectorSoulGained"));
        }
        if (_Player.GetAbilityUseLimit() >= SoulCollectorPointsOpt.GetInt())
        {
            if (!GameStates.IsMeeting)
            {
                PlayerControl sc = _Player;
                sc.RpcSetCustomRole(CustomRoles.Death);
                sc.Notify(GetString("SoulCollectorToDeath"));
                sc.RpcGuardAndKill(sc);
            }
        }
    }
    public override void AfterMeetingTasks()
    {
        if (!_Player.IsAlive()) return;
        TargetId = byte.MaxValue;
        SendRPC();

        if (_Player.GetAbilityUseLimit() >= SoulCollectorPointsOpt.GetInt() && !_Player.Is(CustomRoles.Death))
        {
            _Player.RpcSetCustomRole(CustomRoles.Death);
            _Player.Notify(GetString("SoulCollectorToDeath"));
            _Player.RpcGuardAndKill(_Player);
        }
    }
    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl guesser, CustomRoles role, ref bool guesserSuicide)
    {
        if (!ApocCanGuessApoc.GetBool() && target.IsNeutralApocalypse() && guesser.IsNeutralApocalypse())
        {
            guesser.ShowInfoMessage(isUI, GetString("GuessApocRole"));
            return true;
        }
        return false;
    }
}
internal class Death : RoleBase
{
    //===========================SETUP================================\\
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Death);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralApocalypse;
    //==================================================================\\

    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) => KnowRoleTarget(seer, target);
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target)
        => target.IsNeutralApocalypse() && seer.IsNeutralApocalypse();
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(true);
    public override bool CanUseImpostorVentButton(PlayerControl pc) => SoulCollector.SoulCollectorCanVent.GetBool();
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target) => false;
 
    public override void OnCheckForEndVoting(PlayerState.DeathReason deathReason, params byte[] exileIds)
    {
        if (_Player == null || deathReason != PlayerState.DeathReason.Vote) return;
        if (exileIds.Contains(_Player.PlayerId)) return;
        
        var deathList = new List<byte>();
        var death = _Player;
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.IsNeutralApocalypse()) continue;
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
    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl guesser, CustomRoles role, ref bool guesserSuicide)
    {
        if (!TransformedNeutralApocalypseCanBeGuessed.GetBool())
        {
            guesser.ShowInfoMessage(isUI, GetString("GuessImmune"));
            return true;
        }
        return false;
    }
}