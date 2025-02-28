using AmongUs.GameOptions;
using Hazel;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Neutral;

internal class Revolutionist : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Revolutionist;
    private const int Id = 15200;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralChaos;
    //==================================================================\\

    private static OptionItem RevolutionistDrawTime;
    private static OptionItem RevolutionistCooldown;
    private static OptionItem RevolutionistDrawCount;
    private static OptionItem RevolutionistKillProbability;
    private static OptionItem RevolutionistVentCountDown;


    public static readonly Dictionary<(byte, byte), bool> IsDraw = [];
    private static readonly Dictionary<byte, (PlayerControl, float)> RevolutionistTimer = [];
    private static readonly Dictionary<byte, long> RevolutionistStart = [];
    private static readonly Dictionary<byte, long> RevolutionistLastTime = [];
    private static readonly Dictionary<byte, int> RevolutionistCountdown = [];

    private static byte CurrentDrawTarget = byte.MaxValue;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(15200, TabGroup.NeutralRoles, CustomRoles.Revolutionist);
        RevolutionistDrawTime = FloatOptionItem.Create(15202, "RevolutionistDrawTime", new(0f, 10f, 1f), 3f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Revolutionist])
            .SetValueFormat(OptionFormat.Seconds);
        RevolutionistCooldown = FloatOptionItem.Create(15203, "RevolutionistCooldown", new(5f, 100f, 1f), 10f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Revolutionist])
            .SetValueFormat(OptionFormat.Seconds);
        RevolutionistDrawCount = IntegerOptionItem.Create(15204, "RevolutionistDrawCount", new(1, 14, 1), 6, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Revolutionist])
            .SetValueFormat(OptionFormat.Players);
        RevolutionistKillProbability = IntegerOptionItem.Create(15205, "RevolutionistKillProbability", new(0, 100, 5), 15, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Revolutionist])
            .SetValueFormat(OptionFormat.Percent);
        RevolutionistVentCountDown = FloatOptionItem.Create(15206, "RevolutionistVentCountDown", new(1f, 180f, 1f), 15f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Revolutionist])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        IsDraw.Clear();
        RevolutionistTimer.Clear();
        RevolutionistStart.Clear();
        RevolutionistLastTime.Clear();
        RevolutionistCountdown.Clear();
        CurrentDrawTarget = byte.MaxValue;
    }
    public override void Add(byte playerId)
    {
        CustomRoleManager.OnFixedUpdateOthers.Add(OnFixUpdateOthers);
        CustomRoleManager.CheckDeadBodyOthers.Add(CheckDeadBody);

        foreach (var ar in Main.AllPlayerControls)
            IsDraw.Add((playerId, ar.PlayerId), false);
    }
    public override void Remove(byte playerId)
    {
        CustomRoleManager.OnFixedUpdateOthers.Remove(OnFixUpdateOthers);
        CustomRoleManager.CheckDeadBodyOthers.Remove(CheckDeadBody);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = RevolutionistCooldown.GetFloat();

    public override string GetProgressText(byte playerId, bool comms)
    {
        var draw = GetDrawPlayerCount(playerId, out var _);
        return ColorString(GetRoleColor(CustomRoles.Revolutionist).ShadeColor(0.25f), $"({draw.Item1}/{draw.Item2})");
    }
    public override bool CanUseKillButton(PlayerControl pc) => !IsDrawDone(pc);
    public override bool CanUseImpostorVentButton(PlayerControl pc) => IsDrawDone(pc);
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        foreach (var x in RevolutionistStart.Keys.ToArray())
        {
            var tar = GetPlayerById(x);
            if (tar == null) continue;
            tar.Data.IsDead = true;
            tar.SetDeathReason(PlayerState.DeathReason.Sacrifice);
            tar.RpcExileV2();
            Main.PlayerStates[tar.PlayerId].SetDead();
            Logger.Info($"{tar.GetRealName()} 因会议革命失败", "Revolutionist");
        }
        RevolutionistTimer.Clear();
        RevolutionistStart.Clear();
        RevolutionistLastTime.Clear();
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("RevolutionistDrawButtonText"));
        hud.ImpostorVentButton.buttonLabelText.text = GetString("RevolutionistVentButtonText");
    }
    private static void SetDrawPlayerRPC(PlayerControl player, PlayerControl target, bool isDrawed)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDrawPlayer, SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        writer.Write(isDrawed);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveDrawPlayerRPC(MessageReader reader)
    {
        byte RevolutionistId = reader.ReadByte();
        byte DrawId = reader.ReadByte();
        bool drawed = reader.ReadBoolean();
        IsDraw[(RevolutionistId, DrawId)] = drawed;
    }

    private static void SetCurrentDrawTargetRPC(byte arsonistId, byte targetId)
    {
        if (PlayerControl.LocalPlayer.PlayerId == arsonistId)
        {
            CurrentDrawTarget = targetId;
        }
        else
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCurrentDrawTarget, SendOption.Reliable, -1);
            writer.Write(arsonistId);
            writer.Write(targetId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }
    public static void ReceiveSetCurrentDrawTarget(MessageReader reader)
    {
        byte RevolutionistId = reader.ReadByte();
        byte doTargetId = reader.ReadByte();
        if (PlayerControl.LocalPlayer.PlayerId == RevolutionistId)
            CurrentDrawTarget = doTargetId;
    }
    public static void ResetCurrentDrawTarget(byte arsonistId) => SetCurrentDrawTargetRPC(arsonistId, 255);
    public static bool IsDrawPlayer(PlayerControl arsonist, PlayerControl target)
    {
        if (arsonist == null && target == null && IsDraw == null) return false;
        IsDraw.TryGetValue((arsonist.PlayerId, target.PlayerId), out bool isDraw);
        return isDraw;
    }
    public static bool IsDrawDone(PlayerControl player)
    {
        var (countItem1, countItem2) = GetDrawPlayerCount(player.PlayerId, out var _);
        return countItem1 >= countItem2;
    }
    public static (int, int) GetDrawPlayerCount(byte playerId, out List<PlayerControl> winnerList)
    {
        int draw = 0;
        int all = RevolutionistDrawCount.GetInt();
        int max = Main.AllAlivePlayerControls.Length;
        if (!Main.PlayerStates[playerId].IsDead) max--;
        winnerList = [];
        if (all > max) all = max;

        foreach (var pc in Main.AllPlayerControls)
        {
            if (IsDraw.TryGetValue((playerId, pc.PlayerId), out var isDraw) && isDraw)
            {
                winnerList.Add(pc);
                draw++;
            }
        }
        return (draw, all);
    }
    public override string GetMark(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false)
    {
        if (IsDrawPlayer(seer, target))
            return $"<color={GetRoleColorCode(CustomRoles.Revolutionist)}>●</color>";

        if (RevolutionistTimer.TryGetValue(seer.PlayerId, out var re_kvp) && re_kvp.Item1 == target)
            return $"<color={GetRoleColorCode(CustomRoles.Revolutionist)}>○</color>";

        return string.Empty;
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
        => !isForMeeting ? ColorString(GetRoleColor(CustomRoles.Revolutionist), string.Format(GetString("EnterVentWinCountDown"), RevolutionistCountdown.TryGetValue(seer.PlayerId, out var x) ? x : 10)) : string.Empty;

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        killer.SetKillCooldown(RevolutionistDrawTime.GetFloat());
        if (!IsDraw[(killer.PlayerId, target.PlayerId)] && !RevolutionistTimer.ContainsKey(killer.PlayerId))
        {
            RevolutionistTimer.TryAdd(killer.PlayerId, (target, 0f));
            NotifyRoles(SpecifySeer: killer, SpecifyTarget: target);
            SetCurrentDrawTargetRPC(killer.PlayerId, target.PlayerId);
            killer.RpcSetVentInteraction();
        }
        return false;
    }
    private void CheckDeadBody(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        if (!_Player.IsAlive() || target.PlayerId == _Player.PlayerId || inMeeting || Main.MeetingIsStarted) return;

        _Player.RpcSetVentInteraction();
        _ = new LateTask(() => { NotifyRoles(SpecifySeer: _Player, ForceLoop: false); }, 1f, $"Update name for Revolutionist {_Player?.PlayerId}", shoudLog: false);
    }
    private static void OnFixUpdateOthers(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (RevolutionistTimer.TryGetValue(player.PlayerId, out var revolutionistTimerData))
        {
            var playerId = player.PlayerId;
            if (!player.IsAlive() || Pelican.IsEaten(playerId))
            {
                RevolutionistTimer.Remove(playerId);
                NotifyRoles(SpecifySeer: player);
                ResetCurrentDrawTarget(playerId);
            }
            else
            {
                var (rv_target, rv_time) = revolutionistTimerData;

                if (!rv_target.IsAlive())
                {
                    RevolutionistTimer.Remove(playerId);
                }
                else if (rv_time >= RevolutionistDrawTime.GetFloat())
                {
                    var rvTargetId = rv_target.PlayerId;
                    player.SetKillCooldown();
                    RevolutionistTimer.Remove(playerId);
                    IsDraw[(playerId, rvTargetId)] = true;
                    SetDrawPlayerRPC(player, rv_target, true);
                    NotifyRoles(SpecifySeer: player, SpecifyTarget: rv_target);
                    ResetCurrentDrawTarget(playerId);
                    if (IRandom.Instance.Next(1, 100) <= RevolutionistKillProbability.GetInt() && !rv_target.IsTransformedNeutralApocalypse())
                    {
                        rvTargetId.SetDeathReason(PlayerState.DeathReason.Sacrifice);
                        player.RpcMurderPlayer(rv_target);
                        rv_target.SetRealKiller(player);
                        Main.PlayerStates[rvTargetId].SetDead();
                        Logger.Info($"Revolutionist: {player.GetNameWithRole()} killed by {rv_target.GetNameWithRole()}", "Revolutionist");
                    }
                }
                else
                {
                    float range = NormalGameOptionsV08.KillDistances[Mathf.Clamp(player.Is(Reach.IsReach) ? 2 : Main.NormalOptions.KillDistance, 0, 2)] + 0.5f;
                    float dis = GetDistance(player.GetCustomPosition(), rv_target.GetCustomPosition());
                    if (dis <= range)
                    {
                        RevolutionistTimer[playerId] = (rv_target, rv_time + Time.fixedDeltaTime);
                    }
                    else
                    {
                        RevolutionistTimer.Remove(playerId);
                        NotifyRoles(SpecifySeer: player, SpecifyTarget: rv_target);
                        ResetCurrentDrawTarget(playerId);
                        Logger.Info($"Canceled: {player.GetNameWithRole()}", "Revolutionist");
                    }
                }
            }
        }
        if (!lowLoad && IsDrawDone(player) && player.IsAlive())
        {
            var playerId = player.PlayerId;
            if (RevolutionistStart.TryGetValue(playerId, out long startTime))
            {
                if (RevolutionistLastTime.TryGetValue(playerId, out long lastTime))
                {
                    if (lastTime != nowTime)
                    {
                        RevolutionistLastTime[playerId] = nowTime;
                        lastTime = nowTime;
                    }
                    int time = (int)(lastTime - startTime);
                    int countdown = RevolutionistVentCountDown.GetInt() - time;
                    RevolutionistCountdown.Clear();

                    if (countdown <= 0)
                    {
                        GetDrawPlayerCount(playerId, out var list);

                        foreach (var pc in list.Where(x => x.IsAlive()).ToArray())
                        {
                            pc.Data.IsDead = true;
                            pc.SetDeathReason(PlayerState.DeathReason.Sacrifice);
                            pc.RpcMurderPlayer(pc);
                            Main.PlayerStates[pc.PlayerId].SetDead();
                            NotifyRoles(SpecifySeer: pc);
                        }
                        player.Data.IsDead = true;
                        playerId.SetDeathReason(PlayerState.DeathReason.Sacrifice);
                        player.RpcMurderPlayer(player);
                        Main.PlayerStates[playerId].SetDead();
                    }
                    else
                    {
                        RevolutionistCountdown.TryAdd(playerId, countdown);
                        NotifyRoles(SpecifySeer: player, ForceLoop: false);
                    }
                }
                else
                {
                    RevolutionistLastTime.TryAdd(playerId, RevolutionistStart[playerId]);
                }
            }
            else
            {
                RevolutionistStart.TryAdd(playerId, GetTimeStamp());
            }
        }
    }
    public override bool OnCoEnterVentOthers(PlayerPhysics __instance, int ventId)
    {
        if (AmongUsClient.Instance.IsGameStarted && IsDrawDone(__instance.myPlayer))
        {
            if (!CustomWinnerHolder.CheckForConvertedWinner(__instance.myPlayer.PlayerId))
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Revolutionist);
                GetDrawPlayerCount(__instance.myPlayer.PlayerId, out var x);
                CustomWinnerHolder.WinnerIds.Add(__instance.myPlayer.PlayerId);
                foreach (var apc in x.ToArray())
                    CustomWinnerHolder.WinnerIds.Add(apc.PlayerId);
            }
            return true;
        }
        return false;
    }
}
