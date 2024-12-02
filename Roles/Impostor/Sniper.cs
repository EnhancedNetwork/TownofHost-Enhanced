using AmongUs.GameOptions;
using Hazel;
using TOHE.Modules;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Sniper : RoleBase
{
    //===========================SETUP================================\\
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
    private static readonly Dictionary<byte, int> bulletCount = [];
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
        bulletCount.Clear();
        shotNotify.Clear();
        IsAim.Clear();
        AimTime.Clear();
        meetingReset = false;
    }
    public override void Add(byte playerId)
    {
        PlayerIdList.Add(playerId);

        maxBulletCount = SniperBulletCount.GetInt();
        precisionShooting = SniperPrecisionShooting.GetBool();
        AimAssist = SniperAimAssist.GetBool();
        AimAssistOneshot = SniperAimAssistOnshot.GetBool();
        SniperCanUseKillButton = CanKillWithBullets.GetBool();

        snipeBasePosition[playerId] = new();
        LastPosition[playerId] = new();
        snipeTarget[playerId] = 0x7F;
        bulletCount[playerId] = maxBulletCount;
        shotNotify[playerId] = [];
        IsAim[playerId] = false;
        AimTime[playerId] = 0f;

    }

    private static bool IsThisRole(byte playerId) => PlayerIdList.Contains(playerId);

    public static bool SnipeIsActive(byte playerId) => snipeTarget.ContainsValue(playerId);

    private static void SendRPC(byte playerId)
    {
        Logger.Info($"Player{playerId}:SendRPC", "Sniper");
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SniperSync, SendOption.Reliable, -1);
        writer.Write(playerId);
        var snList = shotNotify[playerId];
        writer.Write(snList.Count);
        foreach (var sn in snList)
        {
            writer.Write(sn);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
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
        if (!bulletCount.ContainsKey(pc.PlayerId))
        {
            Logger.Info($" Sniper not Init yet.", "Sniper");
            return false;
        }
        if (bulletCount[pc.PlayerId] <= 0)
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
        //変身開始地点→解除地点のベクトル
        var snipeBasePos = snipeBasePosition[sniper.PlayerId];
        var snipePos = sniper.transform.position;
        var dir = (snipePos - snipeBasePos).normalized;

        //至近距離で外す対策に一歩後ろから判定を開始する
        snipePos -= dir;

        foreach (var target in Main.AllAlivePlayerControls)
        {
            //自分には当たらない
            if (target.PlayerId == sniper.PlayerId) continue;
            //死んでいない対象の方角ベクトル作成
            var target_pos = target.transform.position - snipePos;
            //自分より後ろの場合はあたらない
            if (target_pos.magnitude < 1) continue;
            //正規化して
            var target_dir = target_pos.normalized;
            //内積を取る
            var target_dot = Vector3.Dot(dir, target_dir);
            Logger.Info($"{target?.Data?.PlayerName}:pos={target_pos} dir={target_dir}", "Sniper");
            Logger.Info($"  Dot={target_dot}", "Sniper");

            //ある程度正確なら登録
            if (target_dot < 0.995) continue;

            if (precisionShooting)
            {
                //射線との誤差確認
                //単位ベクトルとの外積をとれば大きさ=誤差になる。
                var err = Vector3.Cross(dir, target_pos).magnitude;
                Logger.Info($"  err={err}", "Sniper");
                if (err < 0.5)
                {
                    //ある程度正確なら登録
                    targets.Add(target, err);
                }
            }
            else
            {
                //近い順に判定する
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

        if (bulletCount[sniperId] <= 0) return;

        // first shapeshift
        if (shapeshifting)
        {
            //Aim開始
            meetingReset = false;

            //スナイプ地点の登録
            snipeBasePosition[sniperId] = sniper.transform.position;

            LastPosition[sniperId] = sniper.transform.position;
            IsAim[sniperId] = true;
            AimTime[sniperId] = 0f;

            return;
        }

        //エイム終了
        IsAim[sniperId] = false;
        AimTime[sniperId] = 0f;

        //ミーティングによる変身解除なら射撃しない
        if (meetingReset)
        {
            meetingReset = false;
            return;
        }

        //一発消費して
        bulletCount[sniperId]--;

        //命中判定はホストのみ行う
        if (!AmongUsClient.Instance.AmHost) return;

        sniper.RPCPlayCustomSound("AWP");

        var targets = GetSnipeTargets(sniper);

        if (targets.Count != 0)
        {
            //一番正確な対象がターゲット
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
            },
                0.5f, "Sniper shot Notify");
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
            //エイム終了
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
        return Utils.ColorString(Color.yellow, $"({bulletCount[playerId]})");
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
            //エイムアシスト中のスナイパー
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

        //各スナイパーから
        foreach (var sniper in PlayerIdList)
        {
            //射撃音が聞こえるプレイヤー
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
            hud.AbilityButton?.OverrideText(GetString(bulletCount[playerId] <= 0 ? "DefaultShapeshiftText" : "SniperSnipeButtonText"));
    }
}
