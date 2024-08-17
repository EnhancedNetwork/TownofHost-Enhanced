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
    public static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralApocalypse;
    //==================================================================\\

    private static OptionItem SoulCollectorPointsOpt;
    private static OptionItem GetPassiveSouls;
    public static OptionItem SoulCollectorCanVent;
    public static OptionItem DeathMeetingTimeIncrease;

    private static readonly Dictionary<byte, byte> SoulCollectorTarget = [];
    private static readonly Dictionary<byte, int> SoulCollectorPoints = [];

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
        playerIdList.Clear();
        SoulCollectorTarget.Clear();
        SoulCollectorPoints.Clear();
    }

    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        SoulCollectorTarget.TryAdd(playerId, byte.MaxValue);
        SoulCollectorPoints.TryAdd(playerId, 0);

        CustomRoleManager.CheckDeadBodyOthers.Add(OnPlayerDead);
    }

    public override string GetProgressText(byte playerId, bool cvooms) => Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCollector).ShadeColor(0.25f), SoulCollectorPoints.TryGetValue(playerId, out var x) ? $"({x}/{SoulCollectorPointsOpt.GetInt()})" : "Invalid");
    public override void SetAbilityButtonText(HudManager hud, byte playerId) => hud.KillButton.OverrideText(GetString("SoulCollectorKillButtonText"));
    private void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player); //SetSoulCollectorLimit
        writer.Write(playerId);
        writer.Write(SoulCollectorPoints[playerId]);
        writer.Write(SoulCollectorTarget[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte SoulCollectorId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        byte target = reader.ReadByte();

        if (SoulCollectorPoints.ContainsKey(SoulCollectorId))
            SoulCollectorPoints[SoulCollectorId] = Limit;
        else
            SoulCollectorPoints.Add(SoulCollectorId, 0);

        if (SoulCollectorTarget.ContainsKey(SoulCollectorId))
            SoulCollectorTarget[SoulCollectorId] = target;
        else
            SoulCollectorTarget.Add(SoulCollectorId, byte.MaxValue);
    }
    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) => KnowRoleTarget(seer, target);
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target)
        => (target.IsNeutralApocalypse() && seer.IsNeutralApocalypse());
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    => SoulCollectorTarget[seer.PlayerId] == seen.PlayerId ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCollector), "♠") : string.Empty;
    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (playerIdList.Any() && SoulCollectorTarget[playerIdList.First()] == target.PlayerId && seer.IsNeutralApocalypse() && seer.PlayerId != playerIdList.First())
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
        if (SoulCollectorTarget[killer.PlayerId] != byte.MaxValue)
        {
            killer.Notify(GetString("SoulCollectorTargetUsed"));
            return false;
        }
        SoulCollectorTarget.Remove(killer.PlayerId);
        SoulCollectorTarget.TryAdd(killer.PlayerId, target.PlayerId);
        Logger.Info($"{killer.GetNameWithRole()} predicted the death of {target.GetNameWithRole()}", "SoulCollector");
        killer.Notify(string.Format(GetString("SoulCollectorTarget"), target.GetRealName()));
        return false;
    }
    public override void OnReportDeadBody(PlayerControl ryuak, NetworkedPlayerInfo iscute)
    {
        PlayerControl sc = Utils.GetPlayerById(playerIdList.First());
        foreach (var playerId in SoulCollectorTarget.Keys)
        {
            if (GetPassiveSouls.GetBool() && sc.IsAlive())
            {
                SoulCollectorPoints[playerId]++;                
                _ = new LateTask(() =>
                {
                    Utils.SendMessage(GetString("PassiveSoulGained"), playerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCollector), GetString("SoulCollectorTitle")));

                }, 3f, "Set Chat Visible for Everyone");
            }
        }
    }
    private void OnPlayerDead(PlayerControl killer, PlayerControl deadPlayer, bool inMeeting)
    {
        foreach (var (playerId, targetId) in SoulCollectorTarget)
        {
            if (targetId == byte.MaxValue) continue;

            Main.PlayerStates.TryGetValue(targetId, out var playerState);
            if (targetId == deadPlayer.PlayerId && playerState.IsDead && !playerState.Disconnected)
            {
                SoulCollectorTarget[playerId] = byte.MaxValue;
                SoulCollectorPoints[playerId]++;
                if (GameStates.IsMeeting) _ = new LateTask(() =>
                {
                    Utils.SendMessage(GetString("SoulCollectorMeetingDeath"), playerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCollector), GetString("SoulCollectorTitle")));

                }, 3f, "Set Chat Visible for Everyone");
                Utils.GetPlayerById(playerId).Notify(GetString("SoulCollectorSoulGained"));
                SendRPC(playerId);
                Utils.NotifyRoles(SpecifySeer: Utils.GetPlayerById(playerId), ForceLoop: false);
            }
            if (SoulCollectorPoints[playerId] >= SoulCollectorPointsOpt.GetInt())
            {
                SoulCollectorPoints[playerId] = SoulCollectorPointsOpt.GetInt();
                if (!GameStates.IsMeeting) { 
                    PlayerControl sc = Utils.GetPlayerById(playerId);
                    sc.RpcSetCustomRole(CustomRoles.Death);
                    sc.Notify(GetString("SoulCollectorToDeath"));
                    sc.RpcGuardAndKill(sc);
                }
            }
        }
    }
    public override void AfterMeetingTasks()
    {
        foreach (var playerId in SoulCollectorTarget.Keys)
        {
            SoulCollectorTarget[playerId] = byte.MaxValue;
        }
        if (playerIdList.Any())
        {
            PlayerControl sc = Utils.GetPlayerById(playerIdList.First());
            if (sc == null) return;

            if (SoulCollectorPoints[sc.PlayerId] >= SoulCollectorPointsOpt.GetInt() && !sc.Is(CustomRoles.Death))
            {
                sc.RpcSetCustomRole(CustomRoles.Death);
                sc.Notify(GetString("SoulCollectorToDeath"));
                sc.RpcGuardAndKill(sc);
            }
        }
    }
    public static void OnCheckForEndVoting(PlayerState.DeathReason deathReason, params byte[] exileIds)
    {
        if (!HasEnabled || deathReason != PlayerState.DeathReason.Vote) return;
        if (!CustomRoles.Death.RoleExist()) return;
        if (exileIds.Contains(playerIdList.First())) return;
        var deathList = new List<byte>();
        PlayerControl sc = Utils.GetPlayerById(playerIdList.First());
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.IsNeutralApocalypse()) continue;
            if (sc != null && sc.IsAlive())
            {
                if (!Main.AfterMeetingDeathPlayers.ContainsKey(pc.PlayerId))
                {
                    pc.SetRealKiller(sc);
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
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.SoulCollector);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralApocalypse;
    //==================================================================\\

    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) => KnowRoleTarget(seer, target);
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target)
        => (target.IsNeutralApocalypse() && seer.IsNeutralApocalypse());
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(true);
    public override bool CanUseImpostorVentButton(PlayerControl pc) => SoulCollector.SoulCollectorCanVent.GetBool();
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target) => false;
 
    public static void OnCheckForEndVoting(PlayerState.DeathReason deathReason, params byte[] exileIds)
    {
        SoulCollector.OnCheckForEndVoting(deathReason, exileIds);
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