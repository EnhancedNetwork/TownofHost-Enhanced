﻿using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Neutral;

internal class PlagueBearer : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 17600;
    public static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralApocalypse;
    //==================================================================\\

    private static OptionItem PlagueBearerCooldownOpt;
    public static OptionItem PestilenceCooldownOpt;
    public static OptionItem PestilenceCanVent;
    public static OptionItem PestilenceHasImpostorVision;

    private static readonly Dictionary<byte, HashSet<byte>> PlaguedList = [];

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.PlagueBearer, 1, zeroOne: false);
        PlagueBearerCooldownOpt = FloatOptionItem.Create(Id + 10, "PlagueBearerCooldown", new(0f, 180f, 2.5f), 22.5f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.PlagueBearer])
                .SetValueFormat(OptionFormat.Seconds);
        PestilenceCooldownOpt = FloatOptionItem.Create(Id + 11, "PestilenceCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.PlagueBearer])
                .SetValueFormat(OptionFormat.Seconds);
        PestilenceCanVent = BooleanOptionItem.Create(Id + 12, "PestilenceCanVent", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.PlagueBearer]);
        PestilenceHasImpostorVision = BooleanOptionItem.Create(Id + 13, "PestilenceHasImpostorVision", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.PlagueBearer]);
    }

    public override void Init()
    {
        playerIdList.Clear();
        PlaguedList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        PlaguedList[playerId] = [];

        CustomRoleManager.CheckDeadBodyOthers.Add(OnPlayerDead);

        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        PlaguedList.Remove(playerId);
        CustomRoleManager.CheckDeadBodyOthers.Remove(OnPlayerDead);
    }
    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) => KnowRoleTarget(seer, target);
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target)
        => (target.IsNeutralApocalypse() && seer.IsNeutralApocalypse());

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = PlagueBearerCooldownOpt.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    private static bool IsPlagued(byte pc, byte target) => PlaguedList.TryGetValue(pc, out var Targets) && Targets.Contains(target);
    
    public static void SendRPC(PlayerControl player, PlayerControl target)
    {
        MessageWriter writer;
        writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(Utils.GetPlayerById(playerIdList.First())); // setPlaguedPlayer
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte PlagueBearerId = reader.ReadByte();
        byte PlaguedId = reader.ReadByte();
        PlaguedList[PlagueBearerId].Add(PlaguedId);
    }
    public static void CheckAndInfect(PlayerControl seer, PlayerControl target)
    {
        var isDisconnectOrSelfKill = seer.PlayerId == target.PlayerId;
        bool needCheck = false;
        foreach (var (plagueBearerId, Targets) in PlaguedList)
        {
            var plagueBearer = GetPlayerById(plagueBearerId);
            if (plagueBearer == null || !plagueBearer.IsAlive()) continue;

            if (target.Is(CustomRoles.PlagueBearer) && !isDisconnectOrSelfKill)
            {
                PlaguedList[plagueBearerId].Add(seer.PlayerId);
                SendRPC(plagueBearer, seer);
                needCheck = true;
            }
            else if (isDisconnectOrSelfKill)
            {
                needCheck = true;
            }
            else if (Targets.Contains(seer.PlayerId) && !Targets.Contains(target.PlayerId))
            {
                PlaguedList[plagueBearerId].Add(target.PlayerId);
                SendRPC(plagueBearer, target);
                needCheck = true;
            }
            else if (!Targets.Contains(seer.PlayerId) && Targets.Contains(target.PlayerId))
            {
                PlaguedList[plagueBearerId].Add(seer.PlayerId);
                SendRPC(plagueBearer, seer);
                needCheck = true;
            }

            // Remove itself
            PlaguedList[plagueBearerId].Remove(plagueBearerId);
        }
        if (needCheck)
        {
            NotifyRoles();
            CheckPlagueAllPlayers();
        }
    }
    private static (int, int) PlaguedPlayerCount(byte playerId)
    {
        int all = Main.AllAlivePlayerControls.Count(pc => pc.PlayerId != playerId);
        int plagued = Main.AllAlivePlayerControls.Count(pc => pc.PlayerId != playerId && IsPlagued(playerId, pc.PlayerId));

        return (plagued, all);
    }
    private static bool IsPlaguedAll(PlayerControl player)
    {
        if (!player.Is(CustomRoles.PlagueBearer)) return false;

        var (plagued, all) = PlaguedPlayerCount(player.PlayerId);
        return plagued >= all;
    }
    public static void CheckPlagueAllPlayers()
    {
        foreach (var PlagueId in PlaguedList.Keys)
        {
            var plagueBearer = GetPlayerById(PlagueId);
            if (plagueBearer == null) continue;

            if (IsPlaguedAll(plagueBearer))
            {
                playerIdList.Remove(PlagueId);

                // Set Pestilence
                plagueBearer.RpcSetCustomRole(CustomRoles.Pestilence);
                plagueBearer.GetRoleClass()?.OnAdd(PlagueId);

                plagueBearer.Notify(GetString("PlagueBearerToPestilence"), time: 2f);
                plagueBearer.RpcGuardAndKill(plagueBearer);
                plagueBearer.ResetKillCooldown();

                NotifyRoles(SpecifySeer: plagueBearer);
                plagueBearer.MarkDirtySettings();
            }
        }
    }
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (IsPlagued(killer.PlayerId, target.PlayerId))
        {
            killer.Notify(GetString("PlagueBearerAlreadyPlagued"));
            return false;
        }
        PlaguedList[killer.PlayerId].Add(target.PlayerId);
        SendRPC(killer, target);
        NotifyRoles(SpecifySeer: killer);

        CheckPlagueAllPlayers();

        killer.ResetKillCooldown();
        killer.SetKillCooldown();

        return false;
    }
    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody, PlayerControl killer)
    {
        if (HasEnabled && deadBody != null && deadBody.Object != null)
        {
            CheckAndInfect(reporter, deadBody.Object);
        }
        return true;
    }
    private void OnPlayerDead(PlayerControl killer, PlayerControl deadBody, bool inMeeting)
    {
        if (HasEnabled)
        {
            CheckAndInfect(killer, deadBody);
        }
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        => IsPlagued(seer.PlayerId, seen.PlayerId) ? ColorString(GetRoleColor(CustomRoles.PlagueBearer), "⦿") : string.Empty;
    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (playerIdList.Any() && IsPlagued(playerIdList.First(), target.PlayerId) && seer.IsNeutralApocalypse() && seer.PlayerId != playerIdList.First())
        {
            return ColorString(GetRoleColor(CustomRoles.PlagueBearer), "⦿");
        }
        return string.Empty;
    }
    public override string GetProgressText(byte playerId, bool comms)
    {
        var (plagued, all) = PlaguedPlayerCount(playerId);
        return ColorString(GetRoleColor(CustomRoles.PlagueBearer).ShadeColor(0.25f), $"({plagued}/{all})");
    }
    
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("InfectiousKillButtonText"));
    }
}

internal class Pestilence : RoleBase
{
    //===========================SETUP================================\\
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.PlagueBearer);
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralApocalypse;
    //==================================================================\\

    public override void Add(byte playerId)
    {
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) => KnowRoleTarget(seer, target);
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target)
        => (target.IsNeutralApocalypse() && seer.IsNeutralApocalypse());
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = PlagueBearer.PestilenceCooldownOpt.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(PlagueBearer.PestilenceHasImpostorVision.GetBool());
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => PlagueBearer.PestilenceCanVent.GetBool();

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        killer.SetRealKiller(target);
        target.RpcMurderPlayer(killer);
        return false;
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.IsNeutralApocalypse()) return false;
        return true;
    }

    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl pc, CustomRoles role, ref bool guesserSuicide)
    {
        pc.ShowInfoMessage(isUI, GetString("GuessPestilence"));

        guesserSuicide = true;
        Logger.Msg($"Is Active: {guesserSuicide}", "guesserSuicide - Pestilence");
        return false;
    }
}