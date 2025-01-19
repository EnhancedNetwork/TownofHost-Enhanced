using Hazel;
using InnerNet;
using TOHE.Roles.Core;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Neutral;

internal class Seeker : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Seeker;
    private const int Id = 14600;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Seeker);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralEvil;
    //==================================================================\\

    private static OptionItem PointsToWin;
    private static OptionItem TagCooldownOpt;

    private int PointsToWinOpt;

    private byte Target;
    private int TotalPoints;
    private float DefaultSpeed;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Seeker);
        PointsToWin = IntegerOptionItem.Create(Id + 10, "SeekerPointsToWin", new(1, 20, 1), 5, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Seeker]);
        TagCooldownOpt = FloatOptionItem.Create(Id + 11, "SeekerTagCooldown", new(0f, 180f, 2.5f), 12.5f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Seeker])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Add(byte playerId)
    {
        TotalPoints = 0;
        DefaultSpeed = Main.AllPlayerSpeed[playerId];
        PointsToWinOpt = PointsToWin.GetInt();
        Target = 255;

        if (AmongUsClient.Instance.AmHost)
            _ = new LateTask(() =>
            {
                ResetTarget();
            }, 10f, "Seeker Round 1");
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = TagCooldownOpt.GetFloat();
    public override void SetAbilityButtonText(HudManager hud, byte playerId) => hud.KillButton.OverrideText(GetString("SeekerKillButtonText"));
    private void SendRPC(byte seekerId, byte targetId = 0xff, bool setTarget = true)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player); // SetSeekerTarget
        writer.Write(setTarget);


        if (!setTarget) // Sync seeker points
        {
            writer.Write(seekerId);
            writer.Write(TotalPoints);
        }
        else // Set target
        {
            writer.Write(seekerId);
            writer.Write(targetId);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        bool setTarget = reader.ReadBoolean();
        byte seekerId = reader.ReadByte();
        if (!setTarget)
        {
            int points = reader.ReadInt32();
            TotalPoints = points;
            return;
        }

        byte targetId = reader.ReadByte();

        Target = targetId;
    }
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (Target == target.PlayerId)
        {//if the target is correct
            TotalPoints += 1;
            ResetTarget();
        }
        else
        {
            TotalPoints -= 1;
        }

        killer.ResetKillCooldown();
        killer.SetKillCooldown(forceAnime: true);

        SendRPC(killer.PlayerId, setTarget: false);
        return false;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        Main.AllPlayerSpeed[_state.PlayerId] = DefaultSpeed;
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (lowLoad) return;

        if (!player.IsAlive()) return;

        var targetId = Target;
        if (targetId == 255)
        {
            ResetTarget();

            if (Target == 254)
            {
                // No target for seeker to find, normally this wont happen, seeker already loses the game.
                player.SetDeathReason(PlayerState.DeathReason.Suicide);
                player.RpcExileV2();
                player.SetRealKiller(player);
            }
            return;
        }

        var seekerId = player.PlayerId;
        var playerState = Main.PlayerStates[targetId] ?? null;
        var totalPoints = TotalPoints;

        if (playerState == null || playerState.IsDead)
        {
            ResetTarget();
        }

        if (totalPoints >= PointsToWinOpt)
        {
            TotalPoints = PointsToWinOpt;
            if (!CustomWinnerHolder.CheckForConvertedWinner(seekerId))
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Seeker);
                CustomWinnerHolder.WinnerIds.Add(seekerId);
            }
        }
    }
    private void FreezeSeeker()
    {
        var playerId = _Player.PlayerId;
        Main.AllPlayerSpeed[playerId] = Main.MinSpeed;
        ReportDeadBodyPatch.CanReport[playerId] = false;
        _Player?.MarkDirtySettings();

        _ = new LateTask(() =>
        {
            Main.AllPlayerSpeed[playerId] = DefaultSpeed;
            ReportDeadBodyPatch.CanReport[playerId] = true;
            _Player?.MarkDirtySettings();
        }, 5f, "UnFreeze Seeker");
    }
    private byte ResetTarget()
    {
        if (!AmongUsClient.Instance.AmHost) return 0xff;

        var playerId = _Player.PlayerId;

        var cTargets = new List<PlayerControl>(Main.AllAlivePlayerControls.Where(pc => !pc.Is(CustomRoles.Seeker) && !pc.Is(CustomRoles.Solsticer)));

        if (cTargets.Count >= 2)
            cTargets.RemoveAll(x => x.PlayerId == Target);

        if (!cTargets.Any())
        {
            Logger.Warn("Failed to specify target: Target candidate does not exist", "Seeker");
            Target = 254;
            return 0xff;
        }

        var target = cTargets.RandomElement();
        var targetId = target.PlayerId;
        Target = targetId;
        _Player.Notify(string.Format(GetString("SeekerNotify"), target.GetRealName()));
        target.Notify(GetString("SeekerTargetNotify"));


        SendRPC(_Player.PlayerId, targetId: targetId);
        NotifyRoles(SpecifySeer: _Player, ForceLoop: true);
        FreezeSeeker();
        return targetId;
    }
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target) => Target == target.PlayerId ? Main.roleColors[CustomRoles.Seeker] : "";
    public override string GetMarkOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (seer.PlayerId == _Player.PlayerId && seen.PlayerId == Target)
            return ColorString(GetRoleColor(CustomRoles.Seeker), " ★");

        return "";
    }
    public override string GetProgressText(byte PlayerId, bool comms) => ColorString(GetRoleColor(CustomRoles.Seeker).ShadeColor(0.25f), $"({TotalPoints}/{PointsToWin.GetInt()})");

    public override void AfterMeetingTasks()
    {
        var player = _Player;
        if (player.IsAlive())
        {
            FreezeSeeker();
        }
    }
    public override void NotifyAfterMeeting()
    {
        var player = _Player;
        if (player.IsAlive() && Target != 255)
        {
            var targetId = Target;
            player.Notify(string.Format(GetString("SeekerNotify"), GetPlayerById(targetId).GetRealName()));
            GetPlayerById(targetId)?.Notify(GetString("SeekerTargetNotify"));
        }
    }
}
