using AmongUs.GameOptions;
using Hazel;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Neutral;

internal class PlagueBearer : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.PlagueBearer;
    private const int Id = 17600;
    public static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();

    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralApocalypse;
    //==================================================================\\

    private static OptionItem PlagueBearerCooldownOpt;
    public static OptionItem PestilenceCooldownOpt;
    private static OptionItem PlagueBearerCanVent;
    private static OptionItem PlagueBearerHasImpostorVision;
    public static OptionItem PestilenceCanVent;
    public static OptionItem PestilenceHasImpostorVision;
    public static OptionItem PestilenceKillsGuessers;

    private static readonly Dictionary<byte, HashSet<byte>> PlaguedList = [];

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.PlagueBearer, 1, zeroOne: false);
        PlagueBearerCooldownOpt = FloatOptionItem.Create(Id + 10, "PlagueBearerCooldown", new(0f, 180f, 2.5f), 22.5f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.PlagueBearer])
                .SetValueFormat(OptionFormat.Seconds);
        PlagueBearerCanVent = BooleanOptionItem.Create(Id + 14, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.PlagueBearer]);
        PlagueBearerHasImpostorVision = BooleanOptionItem.Create(Id + 15, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.PlagueBearer]);
        PestilenceCooldownOpt = FloatOptionItem.Create(Id + 11, "PestilenceCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.PlagueBearer])
                .SetValueFormat(OptionFormat.Seconds);
        PestilenceCanVent = BooleanOptionItem.Create(Id + 12, "PestilenceCanVent", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.PlagueBearer]);
        PestilenceHasImpostorVision = BooleanOptionItem.Create(Id + 13, "PestilenceHasImpostorVision", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.PlagueBearer]);
        PestilenceKillsGuessers = BooleanOptionItem.Create(Id + 16, "PestilenceKillGuessers", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.PlagueBearer]);
    }

    public override void Init()
    {
        playerIdList.Clear();
        PlaguedList.Clear();
    }
    public override void Add(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);

        PlaguedList[playerId] = [];

        CustomRoleManager.CheckDeadBodyOthers.Add(OnPlayerDead);
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        PlaguedList.Remove(playerId);
        CustomRoleManager.CheckDeadBodyOthers.Remove(OnPlayerDead);
    }
    public override bool CanUseImpostorVentButton(PlayerControl pc) => PlagueBearerCanVent.GetBool();
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
        => opt.SetVision(PlagueBearerHasImpostorVision.GetBool());
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = PlagueBearerCooldownOpt.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    private static bool IsPlagued(byte pc, byte target) => PlaguedList.TryGetValue(pc, out var Targets) && Targets.Contains(target);

    public static void SendRPC(PlayerControl player, PlayerControl target)
    {
        var pc = playerIdList.First().GetPlayer();

        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, pc.NetId, writer));
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
            var plagueBearer = plagueBearerId.GetPlayer();
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
            NotifyRoles(SpecifyTarget: seer);
            NotifyRoles(SpecifyTarget: target);
            CheckPlagueAllPlayers();
        }
    }
    private static (int, int) PlaguedPlayerCount(byte playerId)
    {
        if (Main.AllAlivePlayerControls.Length == 0) return (0, 100);
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
            var plagueBearer = PlagueId.GetPlayer();
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
    public override void AfterMeetingTasks()
    {
        CheckPlagueAllPlayers();
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        => IsPlagued(seer.PlayerId, seen.PlayerId) ? ColorString(GetRoleColor(CustomRoles.PlagueBearer), "⦿") : string.Empty;
    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (playerIdList.Any() && IsPlagued(playerIdList.First(), target.PlayerId) && seer.IsNeutralApocalypse() && seer.PlayerId != playerIdList.First() && !Main.PlayerStates[seer.PlayerId].IsNecromancer)
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
    public override CustomRoles Role => CustomRoles.Pestilence;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Pestilence);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralApocalypse;
    //==================================================================\\

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = PlagueBearer.PestilenceCooldownOpt.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(PlagueBearer.PestilenceHasImpostorVision.GetBool());
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => PlagueBearer.PestilenceCanVent.GetBool();

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (killer.IsNeutralApocalypse() && !Main.PlayerStates[killer.PlayerId].IsNecromancer) return false;
        target.RpcMurderPlayer(killer);
        killer.SetRealKiller(target);
        return false;
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.IsNeutralApocalypse() && !Main.PlayerStates[target.PlayerId].IsNecromancer) return false;
        return true;
    }

    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl pc, CustomRoles role, ref bool guesserSuicide)
    {
        if (PlagueBearer.PestilenceKillsGuessers.GetBool())
        {
            if (role != CustomRoles.Pestilence) return false;
            pc.ShowInfoMessage(isUI, GetString("GuessPestilence"));

            guesserSuicide = true;
            Logger.Msg($"Is Active: {guesserSuicide}", "guesserSuicide - Pestilence");
        }
        return false;
    }
}
