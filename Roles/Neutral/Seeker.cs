using Hazel;
using InnerNet;
using System.Text;
using UnityEngine;
using TOHE.Roles.Core;
using static TOHE.Utils;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Seeker : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 14600;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Seeker);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralEvil;
    //==================================================================\\

    private static OptionItem PointsToWin;
    private static OptionItem TagCooldownOpt;

    private static int PointsToWinOpt;

    private static readonly HashSet<byte> Targets = [];
    private static readonly Dictionary<byte, float> DefaultSpeed = [];

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Seeker);
        PointsToWin = IntegerOptionItem.Create(Id + 10, "SeekerPointsToWin", new(1, 20, 1), 5, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Seeker]);
        TagCooldownOpt = FloatOptionItem.Create(Id + 11, "SeekerTagCooldown", new(0f, 180f, 2.5f), 12.5f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Seeker])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        Targets.Clear();
        DefaultSpeed.Clear();
    }

    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(0);
        DefaultSpeed[playerId] = Main.AllPlayerSpeed[playerId];
        PointsToWinOpt = PointsToWin.GetInt();

        if (AmongUsClient.Instance.AmHost)
            _ = new LateTask(() =>
            {
                ResetTarget(playerId.GetPlayer());
            }, 10f, "Seeker Round 1");
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = TagCooldownOpt.GetFloat();
    public override void SetAbilityButtonText(HudManager hud, byte playerId) => hud.KillButton.OverrideText(GetString("SeekerKillButtonText"));
    private void SendRPC(byte targetId = 0xff)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte targetId = reader.ReadByte();
        Targets.Add(targetId);
    }
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (GetTarget(killer) == target.PlayerId)
        {
            killer.RpcIncreaseAbilityUseLimitBy(1);
            ResetTarget(killer);
        }
        else
        {
            killer.RpcRemoveAbilityUse();
        }
        if (!Options.DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill();
        SetKillCooldown(killer.PlayerId);
        killer.SyncSettings();
        return false;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
       Main.AllPlayerSpeed[_state.PlayerId] = DefaultSpeed[_state.PlayerId];
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (lowLoad) return;
        var targetId = GetTarget(player);
        if (targetId == 0xff) return;

        var seekerId = player.PlayerId;
        var playerState = Main.PlayerStates[targetId];
        var totalPoints = seekerId.GetAbilityUseLimit();

        if (playerState.IsDead)
        {
            ResetTarget(player);
        }
        
        if (totalPoints >= PointsToWinOpt)
        {
            if (!CustomWinnerHolder.CheckForConvertedWinner(seekerId))
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Seeker);
                CustomWinnerHolder.WinnerIds.Add(seekerId);
            }
        }
    }
    private byte GetTarget(PlayerControl player)
    {
        if (player == null || Targets == null) return 0xff;

        if (!Targets.TryGetValue(player.PlayerId, out var targetId))
            targetId = ResetTarget(player);
        
        return targetId;
    }
    private static void FreezeSeeker(PlayerControl player)
    {
        var playerId = player.PlayerId;
        Main.AllPlayerSpeed[playerId] = Main.MinSpeed;
        ReportDeadBodyPatch.CanReport[playerId] = false;
        player?.MarkDirtySettings();

        _ = new LateTask(() =>
        {
            Main.AllPlayerSpeed[playerId] = DefaultSpeed[playerId];
            ReportDeadBodyPatch.CanReport[playerId] = true;
            player?.MarkDirtySettings();
        }, 5f, $"Freeze Seeker {player?.PlayerId}");
    }
    private byte ResetTarget(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return 0xff;

        var playerId = player.PlayerId;

        var cTargets = new List<PlayerControl>(Main.AllAlivePlayerControls.Where(pc => !pc.Is(CustomRoles.Seeker) && !pc.Is(CustomRoles.Solsticer)));

        if (cTargets.Count >= 2 && Targets.TryGetValue(player.PlayerId, out var nowTarget))
            cTargets.RemoveAll(x => x.PlayerId == nowTarget);

        if (!cTargets.Any())
        {
            Logger.Warn("Failed to specify target: Target candidate does not exist", "Seeker");
            return 0xff;
        }

        var target = cTargets.RandomElement();
        var targetId = target.PlayerId;

        Targets.Add(targetId);
        SendRPC(targetId);

        player.Notify(string.Format(GetString("SeekerNotify"), target.GetRealName()));
        target.Notify(GetString("SeekerTargetNotify"));

        NotifyRoles(SpecifySeer: player, ForceLoop: true);
        FreezeSeeker(player);
        return targetId;
    }
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target) => Targets.Contains(target.PlayerId) ? Main.roleColors[CustomRoles.Seeker] : "";
    public override string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        var TextColor = GetRoleColor(CustomRoles.Seeker).ShadeColor(0.25f);

        ProgressText.Append(ColorString(TextColor, ColorString(Color.white, " - ") + $"({playerId.GetAbilityUseLimit()}/{PointsToWin.GetInt()})"));
        return ProgressText.ToString();
    }
    public override void AfterMeetingTasks()
    {
        var player = _Player;
        if (player.IsAlive())
        {
            FreezeSeeker(player);
        }
    }
    public override void NotifyAfterMeeting()
    {
        var player = _Player;
        if (player.IsAlive())
        {
            var targetId = GetTarget(player);
            var target = targetId.GetPlayer();
            if (target == null) return;

            player.Notify(string.Format(GetString("SeekerNotify"), target.GetRealName()));
            target.Notify(GetString("SeekerTargetNotify"));
        }
    }
}
