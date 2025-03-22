using AmongUs.GameOptions;
using Hazel;
using TOHE.Modules;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Neutral;

internal class Arsonist : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Arsonist;
    private const int id = 15900;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => CanIgniteAnytime() ? Custom_RoleType.NeutralKilling : Custom_RoleType.NeutralEvil;
    //==================================================================\\

    private static OptionItem ArsonistDouseTime;
    private static OptionItem ArsonistCooldown;
    private static OptionItem ArsonistCanIgniteAnytimeOpt;
    private static OptionItem ArsonistMinPlayersToIgnite;
    private static OptionItem ArsonistMaxPlayersToIgnite;

    private static readonly Dictionary<byte, (PlayerControl, float)> ArsonistTimer = [];
    private static readonly Dictionary<(byte, byte), bool> IsDoused = [];

    private static byte CurrentDousingTarget = byte.MaxValue;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(id, TabGroup.NeutralRoles, CustomRoles.Arsonist);
        ArsonistDouseTime = FloatOptionItem.Create(id + 10, "ArsonistDouseTime", new(0f, 10f, 1f), 0f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Arsonist])
            .SetValueFormat(OptionFormat.Seconds);
        ArsonistCooldown = FloatOptionItem.Create(id + 11, GeneralOption.Cooldown, new(0f, 180f, 1f), 25f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Arsonist])
            .SetValueFormat(OptionFormat.Seconds);
        ArsonistCanIgniteAnytimeOpt = BooleanOptionItem.Create(id + 12, "ArsonistCanIgniteAnytime", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Arsonist]);
        ArsonistMinPlayersToIgnite = IntegerOptionItem.Create(id + 13, "ArsonistMinPlayersToIgnite", new(1, 14, 1), 1, TabGroup.NeutralRoles, false).SetParent(ArsonistCanIgniteAnytimeOpt);
        ArsonistMaxPlayersToIgnite = IntegerOptionItem.Create(id + 14, "ArsonistMaxPlayersToIgnite", new(1, 14, 1), 3, TabGroup.NeutralRoles, false).SetParent(ArsonistCanIgniteAnytimeOpt);
    }
    public override void Init()
    {
        ArsonistTimer.Clear();
        IsDoused.Clear();
        CurrentDousingTarget = byte.MaxValue;
    }
    public override void Add(byte playerId)
    {
        foreach (var ar in Main.AllPlayerControls)
            IsDoused.Add((playerId, ar.PlayerId), false);

        CustomRoleManager.CheckDeadBodyOthers.Add(CheckDeadBody);
    }

    private static void SendCurrentDousingTargetRPC(byte arsonistId, byte targetId)
    {
        if (PlayerControl.LocalPlayer.PlayerId == arsonistId)
        {
            CurrentDousingTarget = targetId;
        }
        else
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCurrentDousingTarget, SendOption.Reliable);
            writer.Write(arsonistId);
            writer.Write(targetId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }
    public static void ReceiveCurrentDousingTargetRPC(MessageReader reader)
    {
        byte arsonistId = reader.ReadByte();
        byte dousingTargetId = reader.ReadByte();

        if (PlayerControl.LocalPlayer.PlayerId == arsonistId)
            CurrentDousingTarget = dousingTargetId;
    }

    private static void SendSetDousedPlayerRPC(PlayerControl player, PlayerControl target, bool isDoused)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDousedPlayer, SendOption.Reliable);
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        writer.Write(isDoused);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveSetDousedPlayerRPC(MessageReader reader)
    {
        byte ArsonistId = reader.ReadByte();
        byte DousedId = reader.ReadByte();
        bool doused = reader.ReadBoolean();

        IsDoused[(ArsonistId, DousedId)] = doused;
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = ArsonistCooldown.GetFloat();

    public override bool CanUseKillButton(PlayerControl pc)
        => CanIgniteAnytime() ? GetDousedPlayerCount(pc.PlayerId).Item1 < ArsonistMaxPlayersToIgnite.GetInt() : !IsDouseDone(pc);

    public override bool CanUseImpostorVentButton(PlayerControl pc)
        => IsDouseDone(pc) || (CanIgniteAnytime() && (GetDousedPlayerCount(pc.PlayerId).Item1 >= ArsonistMinPlayersToIgnite.GetInt() || pc.inVent));

    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);

    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        killer.SetKillCooldown(ArsonistDouseTime.GetFloat());
        if (!IsDoused[(killer.PlayerId, target.PlayerId)] && !ArsonistTimer.ContainsKey(killer.PlayerId))
        {
            ArsonistTimer.Add(killer.PlayerId, (target, 0f));
            NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
            SendCurrentDousingTargetRPC(killer.PlayerId, target.PlayerId);
            killer.RpcSetVentInteraction();
        }
        return false;
    }
    private void CheckDeadBody(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        if (!_Player.IsAlive() || target.PlayerId == _Player.PlayerId || inMeeting || Main.MeetingIsStarted) return;

        _Player.RpcSetVentInteraction();
        _ = new LateTask(() => { NotifyRoles(SpecifySeer: _Player, ForceLoop: false); }, 1f, $"Update name for Arsonist {_Player?.PlayerId}", shoudLog: false);
    }
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (ArsonistTimer.TryGetValue(player.PlayerId, out var arsonistTimerData))
        {
            var playerId = player.PlayerId;
            if (!player.IsAlive() || Pelican.IsEaten(playerId))
            {
                ArsonistTimer.Remove(playerId);
                NotifyRoles(SpecifySeer: player);
                ResetCurrentDousingTarget(playerId);
            }
            else
            {
                var (arTarget, arTime) = arsonistTimerData;

                if (!arTarget.IsAlive())
                {
                    ArsonistTimer.Remove(playerId);
                }
                else if (arTime >= ArsonistDouseTime.GetFloat())
                {
                    player.SetKillCooldown();
                    ArsonistTimer.Remove(playerId);
                    IsDoused[(playerId, arTarget.PlayerId)] = true;
                    SendSetDousedPlayerRPC(player, arTarget, true);
                    NotifyRoles(SpecifySeer: player, SpecifyTarget: arTarget, ForceLoop: true);
                    ResetCurrentDousingTarget(playerId);
                }
                else
                {
                    float range = NormalGameOptionsV08.KillDistances[Mathf.Clamp(player.Is(Reach.IsReach) ? 2 : Main.NormalOptions.KillDistance, 0, 2)] + 0.5f;
                    float distance = GetDistance(player.GetCustomPosition(), arTarget.GetCustomPosition());

                    if (distance <= range)
                    {
                        ArsonistTimer[playerId] = (arTarget, arTime + Time.fixedDeltaTime);
                    }
                    else
                    {
                        ArsonistTimer.Remove(playerId);
                        NotifyRoles(SpecifySeer: player, SpecifyTarget: arTarget, ForceLoop: true);
                        ResetCurrentDousingTarget(playerId);

                        Logger.Info($"Canceled: {player.GetNameWithRole()}", "Arsonist");
                    }
                }
            }
        }
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
        => ArsonistTimer.Clear();

    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        if (seen == null) return string.Empty;

        if (IsDousedPlayer(seer, seen))
            return ColorString(GetRoleColor(CustomRoles.Arsonist), "▲");

        if (!isForMeeting && ArsonistTimer.TryGetValue(seer.PlayerId, out var ar_kvp) && ar_kvp.Item1 == seen)
            return ColorString(GetRoleColor(CustomRoles.Arsonist), "△");

        return string.Empty;
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
        => !isForMeeting && IsDouseDone(seer) ? ColorString(GetRoleColor(CustomRoles.Arsonist), GetString("EnterVentToWin")) : string.Empty;

    public override string GetProgressText(byte playerId, bool comms)
    {
        var (doused, all) = GetDousedPlayerCount(playerId);

        if (!CanIgniteAnytime())
            return ColorString(GetRoleColor(CustomRoles.Arsonist).ShadeColor(0.25f), $"({doused}/{all})");
        else
            return ColorString(GetRoleColor(CustomRoles.Arsonist).ShadeColor(0.25f), $"({doused}/{ArsonistMaxPlayersToIgnite.GetInt()})");
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("ArsonistDouseButtonText"));
        hud.ImpostorVentButton.OverrideText(GetString("ArsonistVentButtonText"));
    }

    public override Sprite ImpostorVentButtonSprite(PlayerControl player)
        => (IsDouseDone(player) || (CanIgniteAnytime() && GetDousedPlayerCount(player.PlayerId).Item1 >= ArsonistMinPlayersToIgnite.GetInt())) ? CustomButton.Get("Ignite") : null;

    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Douse");

    public override void OnCoEnterVent(PlayerPhysics __instance, int ventId)
    {
        if (AmongUsClient.Instance.IsGameStarted)
        {
            if (IsDouseDone(__instance.myPlayer))
            {
                CustomSoundsManager.RPCPlayCustomSoundAll("Boom");

                if (!CustomWinnerHolder.CheckForConvertedWinner(__instance.myPlayer.PlayerId))
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Arsonist);
                    CustomWinnerHolder.WinnerIds.Add(__instance.myPlayer.PlayerId);
                }

                foreach (var pc in Main.AllPlayerControls)
                {
                    pc.KillFlash();

                    if (pc.IsAlive() && pc != __instance.myPlayer)
                    {
                        pc.SetDeathReason(PlayerState.DeathReason.Torched);
                        pc.RpcMurderPlayer(pc);
                        pc.SetRealKiller(__instance.myPlayer);
                    }
                }
                return;
            }
            else if (CanIgniteAnytime())
            {
                var douseCount = GetDousedPlayerCount(__instance.myPlayer.PlayerId).Item1;
                if (douseCount >= ArsonistMinPlayersToIgnite.GetInt()) // Don't check for max, since the player would not be able to ignite at all if they somehow get more players doused than the max
                {
                    if (douseCount > ArsonistMaxPlayersToIgnite.GetInt()) Logger.Warn("Arsonist Ignited with more players doused than the maximum amount in the settings", "Arsonist Ignite");
                    foreach (var pc in Main.AllAlivePlayerControls)
                    {
                        if (!IsDousedPlayer(__instance.myPlayer, pc)) continue;
                        if (pc.IsTransformedNeutralApocalypse()) continue;
                        pc.KillFlash();
                        pc.SetDeathReason(PlayerState.DeathReason.Torched);
                        pc.RpcMurderPlayer(pc);
                        pc.SetRealKiller(__instance.myPlayer);
                    }
                    if (Main.AllAlivePlayerControls.Length == 1)
                    {
                        if (!CustomWinnerHolder.CheckForConvertedWinner(__instance.myPlayer.PlayerId))
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Arsonist);
                            CustomWinnerHolder.WinnerIds.Add(__instance.myPlayer.PlayerId);
                        }
                    }
                    return;
                }
            }
        }
    }

    public static bool CanIgniteAnytime() => ArsonistCanIgniteAnytimeOpt == null ? false : ArsonistCanIgniteAnytimeOpt.GetBool();

    private static void ResetCurrentDousingTarget(byte arsonistId) => SendCurrentDousingTargetRPC(arsonistId, 255);

    public static bool IsDousedPlayer(PlayerControl arsonist, PlayerControl target)
    {
        if (arsonist == null || target == null || IsDoused == null) return false;
        IsDoused.TryGetValue((arsonist.PlayerId, target.PlayerId), out bool isDoused);
        return isDoused;
    }

    public static bool IsDouseDone(PlayerControl player)
    {
        if (!player.Is(CustomRoles.Arsonist)) return false;
        var (doused, all) = GetDousedPlayerCount(player.PlayerId);
        return doused >= all;
    }

    public static (int, int) GetDousedPlayerCount(byte playerId)
    {
        int doused = 0, all = 0;

        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.PlayerId == playerId) continue;

            all++;
            if (IsDoused.TryGetValue((playerId, pc.PlayerId), out var isDoused) && isDoused)
                doused++;
        }

        return (doused, all);
    }
}
