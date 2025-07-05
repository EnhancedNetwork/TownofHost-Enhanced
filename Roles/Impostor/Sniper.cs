using AmongUs.GameOptions;
using Hazel;
using System.Text;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Sniper : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Sniper;
    private const int Id = 2400;
    private static readonly HashSet<byte> PlayerIdList = [];
    public static bool HasEnabled => PlayerIdList.Any();

    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem SniperBulletCount;
    private static OptionItem SniperPrecisionShooting;
    private static OptionItem SniperAimAssist;
    private static OptionItem SniperAimAssistOnshot;
    private static OptionItem CanKillWithBullets;
    //private static OptionItem AlwaysShowShapeshiftAnimations;

    private static readonly Dictionary<byte, byte> snipeTarget = [];
    private static readonly Dictionary<byte, Vector3> snipeBasePosition = [];
    private static readonly Dictionary<byte, Vector3> LastPosition = [];
    private static readonly Dictionary<byte, List<byte>> shotNotify = [];
    private static readonly Dictionary<byte, bool> IsAim = [];
    private static readonly Dictionary<byte, float> AimTime = [];

    private static bool meetingReset;
    private static int maxBulletCount;
    private static bool precisionShooting;
    private static bool AimAssist;
    private static bool AimAssistOneshot;
    private static bool SniperCanUseKillButton;

    public override void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Sniper, 1);
        SniperBulletCount = IntegerOptionItem.Create(Id + 10, "SniperBulletCount", new(1, 20, 1), 2, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sniper])
            .SetValueFormat(OptionFormat.Pieces);
        SniperPrecisionShooting = BooleanOptionItem.Create(Id + 11, "SniperPrecisionShooting", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sniper]);
        SniperAimAssist = BooleanOptionItem.Create(Id + 12, "SniperAimAssist", true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sniper]);
        SniperAimAssistOnshot = BooleanOptionItem.Create(Id + 13, "SniperAimAssistOneshot", false, TabGroup.ImpostorRoles, false).SetParent(SniperAimAssist);
        CanKillWithBullets = BooleanOptionItem.Create(Id + 14, "SniperCanKill", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sniper]);
        //AlwaysShowShapeshiftAnimations = BooleanOptionItem.Create(Id + 15, GeneralOption.ShowShapeshiftAnimations, true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sniper]);
    }
    public override void Init()
    {
        Logger.Disable("Sniper");

        PlayerIdList.Clear();

        snipeBasePosition.Clear();
        LastPosition.Clear();
        snipeTarget.Clear();
        shotNotify.Clear();
        IsAim.Clear();
        AimTime.Clear();
        meetingReset = false;
    }
    public override void Add(byte playerId)
    {
        if (!PlayerIdList.Contains(playerId))
            PlayerIdList.Add(playerId);

        maxBulletCount = SniperBulletCount.GetInt();
        precisionShooting = SniperPrecisionShooting.GetBool();
        AimAssist = SniperAimAssist.GetBool();
        AimAssistOneshot = SniperAimAssistOnshot.GetBool();
        SniperCanUseKillButton = CanKillWithBullets.GetBool();

        snipeBasePosition[playerId] = new();
        LastPosition[playerId] = new();
        snipeTarget[playerId] = 0x7F;
        playerId.SetAbilityUseLimit(maxBulletCount);
        shotNotify[playerId] = [];
        IsAim[playerId] = false;
        AimTime[playerId] = 0f;

    }

    private static bool IsThisRole(byte playerId) => PlayerIdList.Contains(playerId);

    public static bool SnipeIsActive(byte playerId) => snipeTarget.ContainsValue(playerId);

    private static void SendRPC(byte playerId)
    {
        Logger.Info($"Player{playerId}:SendRPC", "Sniper");
        var msg = new RpcSniperSync(PlayerControl.LocalPlayer.NetId, playerId, shotNotify[playerId]);
        RpcUtils.LateBroadcastReliableMessage(msg);
    }
    public static void ReceiveRPC(MessageReader msg)
    {
        var playerId = msg.ReadByte();
        shotNotify[playerId].Clear();
        var count = msg.ReadInt32();
        while (count > 0)
        {
            shotNotify[playerId].Add(msg.ReadByte());
            count--;
        }
        Logger.Info($"Player{playerId}:ReceiveRPC", "Sniper");
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = /*!AlwaysShowShapeshiftAnimations.GetBool() ? 1f :*/ Options.DefaultShapeshiftCooldown.GetFloat();
        //AURoleOptions.ShapeshifterDuration = 1f;
    }
    public override bool CanUseKillButton(PlayerControl pc)
    {
        if (!pc.IsAlive()) return false;
        var canUse = false;
        if (pc.GetAbilityUseLimit() <= 0)
        {
            canUse = true;
        }
        if (SniperCanUseKillButton)
        {
            canUse = true;
        }

        Logger.Info($" CanUseKillButton:{canUse}", "Sniper");
        return canUse;
    }
    private static Dictionary<PlayerControl, float> GetSnipeTargets(PlayerControl sniper)
    {
        var targets = new Dictionary<PlayerControl, float>();

        var snipeBasePos = snipeBasePosition[sniper.PlayerId];
        var snipePos = sniper.transform.position;
        var dir = (snipePos - snipeBasePos).normalized;

        snipePos -= dir;

        foreach (var target in Main.AllAlivePlayerControls)
        {
            if (target.PlayerId == sniper.PlayerId) continue;
            var target_pos = target.transform.position - snipePos;
            if (target_pos.magnitude < 1) continue;
            var target_dir = target_pos.normalized;
            var target_dot = Vector3.Dot(dir, target_dir);
            Logger.Info($"{target?.Data?.PlayerName}:pos={target_pos} dir={target_dir}", "Sniper");
            Logger.Info($"  Dot={target_dot}", "Sniper");

            if (target_dot < 0.995) continue;

            if (precisionShooting)
            {
                var err = Vector3.Cross(dir, target_pos).magnitude;
                Logger.Info($"  err={err}", "Sniper");
                if (err < 0.5)
                {
                    targets.Add(target, err);
                }
            }
            else
            {
                var err = target_pos.magnitude;
                Logger.Info($"  err={err}", "Sniper");
                targets.Add(target, err);
            }
        }
        return targets;

    }
    public override void OnShapeshift(PlayerControl shapeshifter, PlayerControl target, bool animate, bool shapeshifting)
    {
        var sniper = shapeshifter;
        var sniperId = sniper.PlayerId;

        if (sniperId.GetAbilityUseLimit() <= 0) return;

        // first shapeshift
        if (shapeshifting)
        {
            meetingReset = false;

            snipeBasePosition[sniperId] = sniper.transform.position;

            LastPosition[sniperId] = sniper.transform.position;
            IsAim[sniperId] = true;
            AimTime[sniperId] = 0f;

            return;
        }

        IsAim[sniperId] = false;
        AimTime[sniperId] = 0f;

        if (meetingReset)
        {
            meetingReset = false;
            return;
        }

        sniper.RpcRemoveAbilityUse();

        if (!AmongUsClient.Instance.AmHost) return;

        sniper.RPCPlayCustomSound("AWP");

        var targets = GetSnipeTargets(sniper);

        if (targets.Count != 0)
        {
            var snipedTarget = targets.OrderBy(c => c.Value).First().Key;
            snipeTarget[sniperId] = snipedTarget.PlayerId;
            snipedTarget.CheckMurder(snipedTarget);

            if (!Options.DisableShieldAnimations.GetBool())
                sniper.RpcGuardAndKill();
            else
                sniper.SetKillCooldown();

            snipeTarget[sniperId] = 0x7F;

            targets.Remove(snipedTarget);
            var snList = shotNotify[sniperId];
            snList.Clear();

            foreach (var otherPc in targets.Keys)
            {
                snList.Add(otherPc.PlayerId);
                Utils.NotifyRoles(SpecifySeer: otherPc);
            }
            SendRPC(sniperId);

            _ = new LateTask(() =>
            {
                snList.Clear();
                foreach (var otherPc in targets.Keys)
                {
                    Utils.NotifyRoles(SpecifySeer: otherPc);
                }
                SendRPC(sniperId);
            }, 0.5f, "Sniper shot Notify");
        }
    }
    public static void OnFixedUpdateGlobal(PlayerControl pc)
    {
        if (!HasEnabled || !IsThisRole(pc.PlayerId) || !pc.IsAlive()) return;

        if (!AimAssist) return;

        var sniper = pc;
        var sniperId = sniper.PlayerId;
        if (!IsAim[sniperId]) return;

        if (!GameStates.IsInTask)
        {
            IsAim[sniperId] = false;
            AimTime[sniperId] = 0f;
            return;
        }

        var pos = sniper.transform.position;
        if (pos != LastPosition[sniperId])
        {
            AimTime[sniperId] = 0f;
            LastPosition[sniperId] = pos;
        }
        else
        {
            AimTime[sniperId] += Time.fixedDeltaTime;
            Utils.NotifyRoles(SpecifySeer: sniper);
        }
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        meetingReset = true;
    }
    public override string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();

        var abilityLimit = playerId.GetAbilityUseLimit();
        Color TextColor = abilityLimit > 0 ? Color.yellow : Color.gray;

        ProgressText.Append(Utils.ColorString(TextColor, $"({abilityLimit})"));
        return ProgressText.ToString();
    }
    public static bool TryGetSniper(byte targetId, ref PlayerControl sniper)
    {
        foreach (var kvp in snipeTarget)
        {
            if (kvp.Value == targetId)
            {
                sniper = Utils.GetPlayerById(kvp.Key);
                return true;
            }
        }
        return false;
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        if (isForMeeting) return string.Empty;
        seen ??= seer;
        var sniper = Utils.GetPlayerById(PlayerIdList.First());
        if (!(sniper == seer) || !(sniper == seen)) return string.Empty;

        var seerId = seer.PlayerId;

        if (AimAssist)
        {
            if (0.5f < AimTime[seerId] && (!AimAssistOneshot || AimTime[seerId] < 1.0f))
            {
                if (GetSnipeTargets(Utils.GetPlayerById(seerId)).Any())
                {
                    return $"<size=200%>{Utils.ColorString(Palette.ImpostorRed, "◎")}</size>";
                }
            }
        }
        return string.Empty;
    }
    public override string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        if (isForMeeting) return string.Empty;

        foreach (var sniper in PlayerIdList)
        {
            var snList = shotNotify[sniper];
            if (snList.Any() && snList.Contains(seer.PlayerId))
            {
                return $"<size=200%>{Utils.ColorString(Palette.ImpostorRed, "!")}</size>";
            }
        }
        return string.Empty;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        if (IsThisRole(playerId))
            hud.AbilityButton?.OverrideText(GetString(playerId.GetAbilityUseLimit() <= 0 ? "DefaultShapeshiftText" : "SniperSnipeButtonText"));
    }
}
