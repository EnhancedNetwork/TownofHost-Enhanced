using AmongUs.GameOptions;
using System.Collections.Generic;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Translator;
using TOHE.Roles.AddOns.Common;
using TOHE.Modules;

namespace TOHE.Roles.Neutral;

internal class Arsonist : RoleBase
{
    //===========================SETUP================================\\
    private const int id = 15900;
    private static HashSet<byte> PlayerIds = [];
    public static bool HasEnabled = PlayerIds.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    //==================================================================\\

    private static OptionItem ArsonistDouseTime;
    private static OptionItem ArsonistCooldown;
    private static OptionItem ArsonistCanIgniteAnytime;
    private static OptionItem ArsonistMinPlayersToIgnite;
    private static OptionItem ArsonistMaxPlayersToIgnite;

    public static Dictionary<byte, (PlayerControl, float)> ArsonistTimer = [];
    public static void SetupCustomOptions()
    {
        SetupRoleOptions(id, TabGroup.NeutralRoles, CustomRoles.Arsonist);
        ArsonistDouseTime = FloatOptionItem.Create(id + 10, "ArsonistDouseTime", new(0f, 10f, 1f), 0f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Arsonist])
            .SetValueFormat(OptionFormat.Seconds);
        ArsonistCooldown = FloatOptionItem.Create(id + 11, "Cooldown", new(0f, 180f, 1f), 25f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Arsonist])
            .SetValueFormat(OptionFormat.Seconds);
        ArsonistCanIgniteAnytime = BooleanOptionItem.Create(id + 12, "ArsonistCanIgniteAnytime", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Arsonist]);
        ArsonistMinPlayersToIgnite = IntegerOptionItem.Create(id + 13, "ArsonistMinPlayersToIgnite", new(1, 14, 1), 1, TabGroup.NeutralRoles, false).SetParent(ArsonistCanIgniteAnytime);
        ArsonistMaxPlayersToIgnite = IntegerOptionItem.Create(id + 14, "ArsonistMaxPlayersToIgnite", new(1, 14, 1), 3, TabGroup.NeutralRoles, false).SetParent(ArsonistCanIgniteAnytime);
    }
    public override void Init()
    {
        PlayerIds = [];
        ArsonistTimer = [];
    }
    public override void Add(byte playerId)
    {
        PlayerIds.Add(playerId);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = ArsonistCooldown.GetFloat();
    
    public override bool CanUseKillButton(PlayerControl pc)
        => ArsonistCanIgniteAnytime.GetBool() ? GetDousedPlayerCount(pc.PlayerId).Item1 < ArsonistMaxPlayersToIgnite.GetInt() : !pc.IsDouseDone();
    public override bool CanUseImpostorVentButton(PlayerControl pc)
        => pc.IsDouseDone() || (ArsonistCanIgniteAnytime.GetBool() && (GetDousedPlayerCount(pc.PlayerId).Item1 >= ArsonistMinPlayersToIgnite.GetInt() || pc.inVent));
    
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        killer.SetKillCooldown(ArsonistDouseTime.GetFloat());
        if (!Main.isDoused[(killer.PlayerId, target.PlayerId)] && !ArsonistTimer.ContainsKey(killer.PlayerId))
        {
            ArsonistTimer.Add(killer.PlayerId, (target, 0f));
            NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
            RPC.SetCurrentDousingTarget(killer.PlayerId, target.PlayerId);
        }
        return false;
    }
    
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (ArsonistTimer.TryGetValue(player.PlayerId, out var arsonistTimerData))
        {
            var playerId = player.PlayerId;
            if (!player.IsAlive() || Pelican.IsEaten(playerId))
            {
                ArsonistTimer.Remove(playerId);
                NotifyRoles(SpecifySeer: player);
                RPC.ResetCurrentDousingTarget(playerId);
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
                    Main.isDoused[(playerId, arTarget.PlayerId)] = true;
                    player.RpcSetDousedPlayer(arTarget, true);
                    NotifyRoles(SpecifySeer: player, SpecifyTarget: arTarget, ForceLoop: true);
                    RPC.ResetCurrentDousingTarget(playerId);
                }
                else
                {
                    float range = NormalGameOptionsV07.KillDistances[Mathf.Clamp(player.Is(Reach.IsReach) ? 2 : Main.NormalOptions.KillDistance, 0, 2)] + 0.5f;
                    float distance = Vector2.Distance(player.GetCustomPosition(), arTarget.GetCustomPosition());

                    if (distance <= range)
                    {
                        ArsonistTimer[playerId] = (arTarget, arTime + Time.fixedDeltaTime);
                    }
                    else
                    {
                        ArsonistTimer.Remove(playerId);
                        NotifyRoles(SpecifySeer: player, SpecifyTarget: arTarget, ForceLoop: true);
                        RPC.ResetCurrentDousingTarget(playerId);

                        Logger.Info($"Canceled: {player.GetNameWithRole()}", "Arsonist");
                    }
                }
            }
        }
    }

    public static bool CanIgniteAnytime() => ArsonistCanIgniteAnytime.GetBool();

    public override void OnReportDeadBody(PlayerControl reporter, PlayerControl target)
        => ArsonistTimer.Clear();
    
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
        => seer.IsDousedPlayer(seen) ? $"<color={GetRoleColorCode(CustomRoles.Arsonist)}>▲</color>" :
            (ArsonistTimer.TryGetValue(seer.PlayerId, out var ar_kvp) && ar_kvp.Item1 == seen ? $"<color={GetRoleColorCode(CustomRoles.Arsonist)}>△</color>" : "");
    
    public override string GetProgressText(byte playerId, bool comms)
    {
        var doused = GetDousedPlayerCount(playerId);
        if (!ArsonistCanIgniteAnytime.GetBool()) return ColorString(GetRoleColor(CustomRoles.Arsonist).ShadeColor(0.25f), $"({doused.Item1}/{doused.Item2})");
        else return ColorString(GetRoleColor(CustomRoles.Arsonist).ShadeColor(0.25f), $"({doused.Item1}/{ArsonistMaxPlayersToIgnite.GetInt()})");
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("ArsonistDouseButtonText"));
        hud.ImpostorVentButton.buttonLabelText.text = GetString("ArsonistVentButtonText");
    }
    public override Sprite ImpostorVentButtonSprite(PlayerControl player)
        => (player.IsDouseDone() || (ArsonistCanIgniteAnytime.GetBool() && GetDousedPlayerCount(player.PlayerId).Item1 >= ArsonistMinPlayersToIgnite.GetInt())) ? CustomButton.Get("Ignite") : null;
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Douse");

    public override void OnCoEnterVent(PlayerPhysics __instance, int ventId)
    {

        if (AmongUsClient.Instance.IsGameStarted)
        {
            if (__instance.myPlayer.IsDouseDone())
            {
                CustomSoundsManager.RPCPlayCustomSoundAll("Boom");
                foreach (var pc in Main.AllAlivePlayerControls)
                {
                    if (pc != __instance.myPlayer)
                    {
                        //生存者は焼殺
                        pc.SetRealKiller(__instance.myPlayer);
                        Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Torched;
                        pc.RpcMurderPlayerV3(pc);
                        Main.PlayerStates[pc.PlayerId].SetDead();
                    }
                }
                foreach (var pc in Main.AllPlayerControls) pc.KillFlash();
                if (!CustomWinnerHolder.CheckForConvertedWinner(__instance.myPlayer.PlayerId))
                {
                    CustomWinnerHolder.ShiftWinnerAndSetWinner(CustomWinner.Arsonist); //焼殺で勝利した人も勝利させる
                    CustomWinnerHolder.WinnerIds.Add(__instance.myPlayer.PlayerId);
                }
                return;
            }
            else if (ArsonistCanIgniteAnytime.GetBool())
            {
                var douseCount = GetDousedPlayerCount(__instance.myPlayer.PlayerId).Item1;
                if (douseCount >= ArsonistMinPlayersToIgnite.GetInt()) // Don't check for max, since the player would not be able to ignite at all if they somehow get more players doused than the max
                {
                    if (douseCount > ArsonistMaxPlayersToIgnite.GetInt()) Logger.Warn("Arsonist Ignited with more players doused than the maximum amount in the settings", "Arsonist Ignite");
                    foreach (var pc in Main.AllAlivePlayerControls)
                    {
                        if (!__instance.myPlayer.IsDousedPlayer(pc)) continue;
                        pc.KillFlash();
                        pc.SetRealKiller(__instance.myPlayer);
                        Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Torched;
                        pc.RpcMurderPlayerV3(pc);
                        Main.PlayerStates[pc.PlayerId].SetDead();
                    }
                    if (Main.AllAlivePlayerControls.Length == 1)
                    {
                        if (!CustomWinnerHolder.CheckForConvertedWinner(__instance.myPlayer.PlayerId))
                        {
                            CustomWinnerHolder.ShiftWinnerAndSetWinner(CustomWinner.Arsonist); //焼殺で勝利した人も勝利させる
                            CustomWinnerHolder.WinnerIds.Add(__instance.myPlayer.PlayerId);
                        }
                    }
                    return;
                }
            }
        }
    }
}

static class ArsonistPlayerControls 
{
    // classes using "TOHE.Roles.Neutrals are directly able to use type 'this var' methods"
    public static bool IsDousedPlayer(this PlayerControl arsonist, PlayerControl target)
    {
        if (arsonist == null || target == null || Main.isDoused == null) return false;
        Main.isDoused.TryGetValue((arsonist.PlayerId, target.PlayerId), out bool isDoused);
        return isDoused;
    }
    public static bool IsDrawPlayer(this PlayerControl arsonist, PlayerControl target)
    {
        if (arsonist == null && target == null && Main.isDraw == null) return false;
        Main.isDraw.TryGetValue((arsonist.PlayerId, target.PlayerId), out bool isDraw);
        return isDraw;
    }
    public static bool IsDouseDone(this PlayerControl player)
    {
        if (!player.Is(CustomRoles.Arsonist)) return false;
        var (countItem1, countItem2) = GetDousedPlayerCount(player.PlayerId);
        return countItem1 >= countItem2;
    }
}
