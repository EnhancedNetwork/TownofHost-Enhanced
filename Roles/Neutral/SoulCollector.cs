﻿using AmongUs.GameOptions;
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
        AbilityLimit = 0;

        CustomRoleManager.CheckDeadBodyOthers.Add(OnPlayerDead);
    }

    public override string GetProgressText(byte playerId, bool cvooms) => Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCollector).ShadeColor(0.25f),  $"({AbilityLimit}/{SoulCollectorPointsOpt.GetInt()})");
    public override void SetAbilityButtonText(HudManager hud, byte playerId) => hud.KillButton.OverrideText(GetString("SoulCollectorKillButtonText"));
    private void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable);
        writer.WriteNetObject(_Player);
        writer.Write(AbilityLimit);
        writer.Write(TargetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        var limit = reader.ReadSingle();
        byte target = reader.ReadByte();

        AbilityLimit = limit;
        TargetId =  target;
    }
    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) => KnowRoleTarget(seer, target);
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target)
        => (target.IsNeutralApocalypse() && seer.IsNeutralApocalypse());
    
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
        => TargetId == seen.PlayerId ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCollector), "♠") : string.Empty;
    
    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (_Player == null) return string.Empty;
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
        Logger.Info($"{killer.GetNameWithRole()} predicted the death of {target.GetNameWithRole()}", "SoulCollector");
        killer.Notify(string.Format(GetString("SoulCollectorTarget"), target.GetRealName()));
        return false;
    }
    public override void OnReportDeadBody(PlayerControl ryuak, NetworkedPlayerInfo iscute)
    {
        if (_Player == null || !_Player.IsAlive() || !GetPassiveSouls.GetBool()) return;
        
        AbilityLimit++;
        SendRPC();
    }
    public override void OnMeetingHudStart(PlayerControl pc)
    {
        if (!pc.IsAlive() || !GetPassiveSouls.GetBool()) return;

        MeetingHudStartPatch.AddMsg(GetString("PassiveSoulGained"), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCollector), GetString("SoulCollectorTitle")));
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
            AbilityLimit++;
            if (inMeeting)
            {
                _ = new LateTask(() =>
                {
                    Utils.SendMessage(GetString("SoulCollectorMeetingDeath"), playerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCollector), GetString("SoulCollectorTitle")));

                }, 3f, "Soul Collector Meeting Death");
            }

            SendRPC();
            _Player.Notify(GetString("SoulCollectorSoulGained"));
        }
        if (AbilityLimit >= SoulCollectorPointsOpt.GetInt() && !inMeeting)
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

        if (AbilityLimit >= SoulCollectorPointsOpt.GetInt() && !_Player.Is(CustomRoles.Death))
        {
            _Player.RpcSetCustomRole(CustomRoles.Death);
            _Player.GetRoleClass()?.OnAdd(_Player.PlayerId);

            _Player.Notify(GetString("SoulCollectorToDeath"));
            _Player.RpcGuardAndKill(_Player);
        }
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
        if (_Player == null || exileIds == null || exileIds.Contains(_Player.PlayerId)) return;
        
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
    public override void CheckExileTarget(NetworkedPlayerInfo exiled, ref bool DecidedWinner, bool isMeetingHud, ref string name)
    {
        if (exiled == null) return;
        var sc = Utils.GetPlayerListByRole(CustomRoles.Death).FirstOrDefault();
        if (sc == null || !sc.IsAlive() || sc.Data.Disconnected) return;

        if (isMeetingHud)
        {
            if (exiled.PlayerId == sc.PlayerId)
            {
                name = string.Format(GetString("ExiledDeath"), Main.LastVotedPlayer, Utils.GetDisplayRoleAndSubName(exiled.PlayerId, exiled.PlayerId, true));
            }
            else
            {
                name = string.Format(GetString("ExiledNotDeath"), Main.LastVotedPlayer, Utils.GetDisplayRoleAndSubName(exiled.PlayerId, exiled.PlayerId, true));
            }
        }
    }
}