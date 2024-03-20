using System;
using System.Collections.Generic;
using System.Linq;
using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Translator;
using Hazel;
using static UnityEngine.GraphicsBuffer;
using AmongUs.GameOptions;
using TOHE.Roles.AddOns.Common;
using UnityEngine;
using TOHE.Roles.Core;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace TOHE.Roles.Neutral
{
    internal class Revolutionist : RoleBase // 😓
    {
        //===========================SETUP================================\\
        private const int Id = 15200;
        private static readonly HashSet<byte> PlayerIds = [];
        public static bool HasEnabled => PlayerIds.Any();
        public override bool IsEnable => HasEnabled;
        public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
        //==================================================================\\

        public static OptionItem RevolutionistDrawTime;
        public static OptionItem RevolutionistCooldown;
        public static OptionItem RevolutionistDrawCount;
        public static OptionItem RevolutionistKillProbability;
        public static OptionItem RevolutionistVentCountDown;


        public static Dictionary<byte, (PlayerControl, float)> RevolutionistTimer = [];
        public static Dictionary<byte, long> RevolutionistStart = [];
        public static Dictionary<byte, long> RevolutionistLastTime = [];
        public static Dictionary<byte, int> RevolutionistCountdown = [];

        public static void SetupCustomOptions()
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
            RevolutionistTimer.Clear();
            RevolutionistStart.Clear();
            RevolutionistLastTime.Clear();
            RevolutionistCountdown.Clear();

            PlayerIds.Clear();
        }
        public override void Add(byte playerId)
        {
            PlayerIds.Add(playerId);
            CustomRoleManager.OnFixedUpdateOthers.Add(OnFixUpdateOthers);


            foreach (var ar in Main.AllPlayerControls)
                Main.isDraw.Add((playerId, ar.PlayerId), false);
        }
        public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = RevolutionistCooldown.GetFloat();

        public override string GetProgressText(byte playerId, bool comms)
        {
            var draw = GetDrawPlayerCount(playerId, out var _);
            return ColorString(GetRoleColor(CustomRoles.Revolutionist).ShadeColor(0.25f), $"({draw.Item1}/{draw.Item2})");
        }
        public override bool CanUseKillButton(PlayerControl pc) => !pc.IsDrawDone();
        public override bool CanUseImpostorVentButton(PlayerControl pc) => pc.IsDrawDone();
        public override void OnReportDeadBody(PlayerControl reporter, PlayerControl target)
        {
            RevolutionistTimer.Clear();
            RevolutionistStart.Clear();
            RevolutionistLastTime.Clear();


            foreach (var x in RevolutionistStart.Keys.ToArray())
            {
                var tar = GetPlayerById(x);
                if (tar == null) continue;
                tar.Data.IsDead = true;
                Main.PlayerStates[tar.PlayerId].deathReason = PlayerState.DeathReason.Sacrifice;
                tar.RpcExileV2();
                Main.PlayerStates[tar.PlayerId].SetDead();
                Logger.Info($"{tar.GetRealName()} 因会议革命失败", "Revolutionist");
            }
        }
        public override void SetAbilityButtonText(HudManager hud, byte playerId)
        {
            hud.KillButton.OverrideText(GetString("RevolutionistDrawButtonText"));
            hud.ImpostorVentButton.buttonLabelText.text = GetString("RevolutionistVentButtonText");
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            byte RevolutionistId = reader.ReadByte();
            byte DrawId = reader.ReadByte();
            bool drawed = reader.ReadBoolean();
            Main.isDraw[(RevolutionistId, DrawId)] = drawed;
        }
        public override string GetMark(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false)
        {
            if (seer.IsDrawPlayer(target))
                return $"<color={GetRoleColorCode(CustomRoles.Revolutionist)}>●</color>";

            if (RevolutionistTimer.TryGetValue(seer.PlayerId, out var re_kvp) && re_kvp.Item1 == target)
                return $"<color={GetRoleColorCode(CustomRoles.Revolutionist)}>○</color>";

            return "";
        }
        public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
        {
            killer.SetKillCooldown(RevolutionistDrawTime.GetFloat());
            if (!Main.isDraw[(killer.PlayerId, target.PlayerId)] && !RevolutionistTimer.ContainsKey(killer.PlayerId))
            {
                RevolutionistTimer.TryAdd(killer.PlayerId, (target, 0f));
                NotifyRoles(SpecifySeer: killer, SpecifyTarget: target);
                RPC.SetCurrentDrawTarget(killer.PlayerId, target.PlayerId);
            }
            return false;
        }
        private static void OnFixUpdateOthers(PlayerControl player) // jesus christ
        {
            if (RevolutionistTimer.TryGetValue(player.PlayerId, out var revolutionistTimerData))
            {
                var playerId = player.PlayerId;
                if (!player.IsAlive() || Pelican.IsEaten(playerId))
                {
                    RevolutionistTimer.Remove(playerId);
                    NotifyRoles(SpecifySeer: player);
                    RPC.ResetCurrentDrawTarget(playerId);
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
                        Main.isDraw[(playerId, rvTargetId)] = true;
                        player.RpcSetDrawPlayer(rv_target, true);
                        NotifyRoles(SpecifySeer: player, SpecifyTarget: rv_target);
                        RPC.ResetCurrentDrawTarget(playerId);
                        if (IRandom.Instance.Next(1, 100) <= RevolutionistKillProbability.GetInt())
                        {
                            rv_target.SetRealKiller(player);
                            Main.PlayerStates[rvTargetId].deathReason = PlayerState.DeathReason.Sacrifice;
                            player.RpcMurderPlayerV3(rv_target);
                            Main.PlayerStates[rvTargetId].SetDead();
                            Logger.Info($"Revolutionist: {player.GetNameWithRole()} killed by {rv_target.GetNameWithRole()}", "Revolutionist");
                        }
                    }
                    else
                    {
                        float range = NormalGameOptionsV07.KillDistances[Mathf.Clamp(player.Is(Reach.IsReach) ? 2 : Main.NormalOptions.KillDistance, 0, 2)] + 0.5f;
                        float dis = Vector2.Distance(player.GetCustomPosition(), rv_target.GetCustomPosition());
                        if (dis <= range)
                        {
                            RevolutionistTimer[playerId] = (rv_target, rv_time + Time.fixedDeltaTime);
                        }
                        else
                        {
                            RevolutionistTimer.Remove(playerId);
                            NotifyRoles(SpecifySeer: player, SpecifyTarget: rv_target);
                            RPC.ResetCurrentDrawTarget(playerId);
                            Logger.Info($"Canceled: {player.GetNameWithRole()}", "Revolutionist");
                        }
                    }
                }
            }
            if (player.IsDrawDone() && player.IsAlive())
            {
                var playerId = player.PlayerId;
                if (RevolutionistStart.TryGetValue(playerId, out long startTime))
                {
                    if (RevolutionistLastTime.TryGetValue(playerId, out long lastTime))
                    {
                        long nowtime = GetTimeStamp();
                        if (lastTime != nowtime)
                        {
                            RevolutionistLastTime[playerId] = nowtime;
                            lastTime = nowtime;
                        }
                        int time = (int)(lastTime - startTime);
                        int countdown = RevolutionistVentCountDown.GetInt() - time;
                        RevolutionistCountdown.Clear();

                        if (countdown <= 0)
                        {
                            GetDrawPlayerCount(playerId, out var list);

                            foreach (var pc in list.Where(x => x != null && x.IsAlive()).ToArray())
                            {
                                pc.Data.IsDead = true;
                                Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Sacrifice;
                                pc.RpcMurderPlayerV3(pc);
                                Main.PlayerStates[pc.PlayerId].SetDead();
                                NotifyRoles(SpecifySeer: pc);
                            }
                            player.Data.IsDead = true;
                            Main.PlayerStates[playerId].deathReason = PlayerState.DeathReason.Sacrifice;
                            player.RpcMurderPlayerV3(player);
                            Main.PlayerStates[playerId].SetDead();
                        }
                        else
                        {
                            RevolutionistCountdown.TryAdd(playerId, countdown);
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
            if (AmongUsClient.Instance.IsGameStarted && __instance.myPlayer.IsDrawDone())
            {
                if (!CustomWinnerHolder.CheckForConvertedWinner(__instance.myPlayer.PlayerId))
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Revolutionist);
                    Utils.GetDrawPlayerCount(__instance.myPlayer.PlayerId, out var x);
                    CustomWinnerHolder.WinnerIds.Add(__instance.myPlayer.PlayerId);
                    foreach (var apc in x.ToArray())
                        CustomWinnerHolder.WinnerIds.Add(apc.PlayerId);
                }
                return true;
            }
            return false;
        }
        public static void SetSeerName(PlayerControl seer, ref string SelfName)
        {
            if (seer.IsDrawDone())
                SelfName = $">{ColorString(seer.GetRoleColor(), string.Format(GetString("EnterVentWinCountDown"), Revolutionist.RevolutionistCountdown.TryGetValue(seer.PlayerId, out var x) ? x : 10))}";
        }
        public static void SetRealName(PlayerControl seer,PlayerControl target, ref string RealName)
        {
            if (target.IsDrawDone())
                RealName = ColorString(GetRoleColor(CustomRoles.Revolutionist), string.Format(GetString("EnterVentWinCountDown"), RevolutionistCountdown.TryGetValue(seer.PlayerId, out var x) ? x : 10));
        }
    }
}
