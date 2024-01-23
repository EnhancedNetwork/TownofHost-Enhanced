using Hazel;
using System.Collections.Generic;
using System.Linq;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;
public static class Seeker
{
    private static readonly int Id = 14600;
    private static List<byte> playerIdList = [];
    public static bool IsEnable = false;

    public static OptionItem PointsToWin;
    private static OptionItem TagCooldownOpt;

    public static int PointsToWinOpt;

    public static Dictionary<byte, byte> Targets = [];
    public static Dictionary<byte, int> TotalPoints = [];
    private static float DefaultSpeed = new();

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Seeker);
        PointsToWin = IntegerOptionItem.Create(Id + 10, "SeekerPointsToWin", new(1, 20, 1), 5, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Seeker]);
        TagCooldownOpt = FloatOptionItem.Create(Id + 11, "SeekerTagCooldown", new(0f, 180f, 2.5f), 12.5f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Seeker])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public static void Init()
    {
        playerIdList = [];
        Targets = [];
        TotalPoints = [];
        DefaultSpeed = new();
        IsEnable = false;
    }

    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;

        TotalPoints.Add(playerId, 0);
        DefaultSpeed = Main.AllPlayerSpeed[playerId];
        PointsToWinOpt = PointsToWin.GetInt();

        if (AmongUsClient.Instance.AmHost)
            _ = new LateTask(() =>
            {
                ResetTarget(Utils.GetPlayerById(playerId));
            }, 10f, "Seeker Round 1");

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = TagCooldownOpt.GetFloat();

    private static void SendRPC(byte seekerId, byte targetId = 0xff, bool setTarget = true)
    {
        MessageWriter writer;
        if (!setTarget)
        {
            writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetSeekerPoints, SendOption.Reliable, -1);
            writer.Write(seekerId);
            writer.Write(TotalPoints[seekerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            return;
        }
        writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetSeekerTarget, SendOption.Reliable, -1);
        writer.Write(seekerId);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader, bool setTarget = true)
    {
        byte seekerId = reader.ReadByte();
        if (!setTarget)
        {
            int points = reader.ReadInt32();
            if (TotalPoints.ContainsKey(seekerId))
                TotalPoints[seekerId] = points;
            else
                TotalPoints.Add(seekerId, 0);
            return;
        }

        byte targetId = reader.ReadByte();

        Targets[seekerId] = targetId;
    }
    public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (GetTarget(killer) == target.PlayerId)
        {//ターゲットをキルした場合
            TotalPoints[killer.PlayerId] += 1;
            ResetTarget(killer);
        }
        else
        {
            TotalPoints[killer.PlayerId] -= 1;
        }
        killer.SyncSettings();  //IDK WHAT DOES THIS DO!!
        if (!Options.DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill();
        SetKillCooldown(killer.PlayerId);
        SendRPC(killer.PlayerId, setTarget: false);
    }
    public static void OnReportDeadBody()
    {
        foreach (var playerId in playerIdList)
        {
            Main.AllPlayerSpeed[playerId] = DefaultSpeed;
        }
    }

    public static void OnFixedUpdate(PlayerControl player)
    {
        if (player == null) return;
        var targetId = GetTarget(player);
        var seekerId = player.PlayerId;
        var playerState = Main.PlayerStates[targetId];
        var totalPoints = TotalPoints[seekerId];

        if (playerState.IsDead)
        {
            ResetTarget(player);
        }
        
        if (totalPoints >= PointsToWinOpt)
        {
            TotalPoints[seekerId] = PointsToWinOpt;
            if (!CustomWinnerHolder.CheckForConvertedWinner(seekerId))
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Seeker);
                CustomWinnerHolder.WinnerIds.Add(seekerId);
            }
        }
    }
    public static byte GetTarget(PlayerControl player)
    {
        if (player == null) return 0xff;
        if (Targets == null) Targets = [];

        if (!Targets.TryGetValue(player.PlayerId, out var targetId))
            targetId = ResetTarget(player);
        return targetId;
    }
    public static void FreezeSeeker(PlayerControl player)
    {
        Main.AllPlayerSpeed[player.PlayerId] = Main.MinSpeed;
        ReportDeadBodyPatch.CanReport[player.PlayerId] = false;
        player.MarkDirtySettings();
        _ = new LateTask(() =>
        {
            Main.AllPlayerSpeed[player.PlayerId] = DefaultSpeed;
            ReportDeadBodyPatch.CanReport[player.PlayerId] = true;
            player.MarkDirtySettings(); // dont know what the hell is this
        }, 5f, "Freeze Seeker");
    }
    public static byte ResetTarget(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return 0xff;

        var playerId = player.PlayerId;

        var cTargets = new List<PlayerControl>(Main.AllAlivePlayerControls.Where(pc => !pc.Is(CustomRoles.Seeker) && !pc.Is(CustomRoles.Solsticer)));

        if (cTargets.Count >= 2 && Targets.TryGetValue(player.PlayerId, out var nowTarget))
            cTargets.RemoveAll(x => x.PlayerId == nowTarget);

        if (cTargets.Count <= 0)
        {
            Logger.Warn("Failed to specify target: Target candidate does not exist", "Seeker");
            return 0xff;
        }

        var rand = IRandom.Instance;
        var target = cTargets[rand.Next(0, cTargets.Count)];
        var targetId = target.PlayerId;
        Targets[playerId] = targetId;
        player.Notify(string.Format(GetString("SeekerNotify"), target.GetRealName()));
        target.Notify(GetString("SeekerTargetNotify"));


        SendRPC(player.PlayerId, targetId: targetId);
        Utils.NotifyRoles(SpecifySeer: player, ForceLoop: true);
        FreezeSeeker(player);
        return targetId;
    }
    public static void AfterMeetingTasks()
    {
        if (!IsEnable) return;

        foreach (var id in playerIdList)
        {
            if (!Main.PlayerStates[id].IsDead)
            {
                FreezeSeeker(Utils.GetPlayerById(id));
                var targetId = GetTarget(Utils.GetPlayerById(id));
                Utils.GetPlayerById(id).Notify(string.Format(GetString("SeekerNotify"), Utils.GetPlayerById(targetId).GetRealName()));
                Utils.GetPlayerById(targetId).Notify(GetString("SeekerTargetNotify"));

            }
        }
    }
}
