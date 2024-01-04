using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using TOHE.Modules;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

internal static class FFAManager
{
    private static Dictionary<byte, long> FFAShieldedList = new();
    private static Dictionary<byte, long> FFAIncreasedSpeedList = new();
    private static Dictionary<byte, long> FFADecreasedSpeedList = new();
    public static Dictionary<byte, long> FFALowerVisionList = new();
    public static Dictionary<byte, long> FFAEnterVentTime = new();
    public static Dictionary<byte, float> FFAVentDuration = new();

    private static Dictionary<byte, float> originalSpeed = new();
    public static Dictionary<byte, int> KBScore = new();
    public static Dictionary<byte, long> FFALastKill = new();
    public static int RoundTime;

    //Options
    public static OptionItem FFA_GameTime;
    public static OptionItem FFA_KCD;
    public static OptionItem FFA_LowerVision;
    public static OptionItem FFA_IncreasedSpeed;
    public static OptionItem FFA_DecreasedSpeed;
    public static OptionItem FFA_ShieldDuration;
    public static OptionItem FFA_ModifiedVisionDuration;
    public static OptionItem FFA_ModifiedSpeedDuration;
    public static OptionItem FFA_DisableVentingWhenTwoPlayersAlive;
    public static OptionItem FFA_DisableVentingWhenKCDIsUp;
    public static OptionItem FFA_EnableRandomAbilities;
    public static OptionItem FFA_EnableRandomTwists;
    public static OptionItem FFA_ShieldIsOneTimeUse;

    public static void SetupCustomOption()
    {
        FFA_GameTime = IntegerOptionItem.Create(67_223_001, "FFA_GameTime", new(30, 600, 10), 300, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.FFA)
            .SetColor(new Color32(0, 255, 165, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds)
            .SetHeader(true);
        FFA_KCD = FloatOptionItem.Create(67_223_002, "FFA_KCD", new(1f, 60f, 1f), 10f, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.FFA)
            .SetColor(new Color32(0, 255, 165, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds);
        FFA_DisableVentingWhenTwoPlayersAlive = BooleanOptionItem.Create(67_223_003, "FFA_DisableVentingWhenTwoPlayersAlive", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.FFA)
            .SetColor(new Color32(0, 255, 165, byte.MaxValue));
        FFA_DisableVentingWhenKCDIsUp = BooleanOptionItem.Create(67_223_004, "FFA_DisableVentingWhenKCDIsUp", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.FFA)
            .SetColor(new Color32(0, 255, 165, byte.MaxValue));
        FFA_EnableRandomAbilities = BooleanOptionItem.Create(67_223_005, "FFA_EnableRandomAbilities", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.FFA)
            .SetColor(new Color32(0, 255, 165, byte.MaxValue));
        FFA_ShieldDuration = FloatOptionItem.Create(67_223_006, "FFA_ShieldDuration", new(1f, 70f, 1f), 7f, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.FFA)
            .SetColor(new Color32(0, 255, 165, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds);
        FFA_IncreasedSpeed = FloatOptionItem.Create(67_223_007, "FFA_IncreasedSpeed", new(0.1f, 5f, 0.1f), 1.5f, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.FFA)
            .SetColor(new Color32(0, 255, 165, byte.MaxValue))
            .SetValueFormat(OptionFormat.Multiplier);
        FFA_DecreasedSpeed = FloatOptionItem.Create(67_223_008, "FFA_DecreasedSpeed", new(0.1f, 5f, 0.1f), 1f, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.FFA)
            .SetColor(new Color32(0, 255, 165, byte.MaxValue))
            .SetValueFormat(OptionFormat.Multiplier);
        FFA_ModifiedSpeedDuration = FloatOptionItem.Create(67_223_009, "FFA_ModifiedSpeedDuration", new(1f, 60f, 1f), 10f, TabGroup.GameSettings, false).SetGameMode(CustomGameMode.FFA).SetColor(new Color32(0, 255, 165, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds);
        FFA_LowerVision = FloatOptionItem.Create(67_223_010, "FFA_LowerVision", new(0f, 1f, 0.05f), 0.5f, TabGroup.GameSettings, false).SetGameMode(CustomGameMode.FFA).SetColor(new Color32(0, 255, 165, byte.MaxValue))
            .SetValueFormat(OptionFormat.Multiplier);
        FFA_ModifiedVisionDuration = FloatOptionItem.Create(67_223_011, "FFA_ModifiedVisionDuration", new(1f, 70f, 1f), 5f, TabGroup.GameSettings, false).SetGameMode(CustomGameMode.FFA).SetColor(new Color32(0, 255, 165, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds);
        FFA_EnableRandomTwists = BooleanOptionItem.Create(67_223_012, "FFA_EnableRandomTwists", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.FFA)
            .SetColor(new Color32(0, 255, 165, byte.MaxValue));
        FFA_ShieldIsOneTimeUse = BooleanOptionItem.Create(67_223_013, "FFA_ShieldIsOneTimeUse", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.FFA)
            .SetColor(new Color32(0, 255, 165, byte.MaxValue));
    }

    public static void Init()
    {
        if (Options.CurrentGameMode != CustomGameMode.FFA) return;

        FFADecreasedSpeedList = new();
        FFAIncreasedSpeedList = new();
        FFALowerVisionList = new();
        FFAShieldedList = new();

        originalSpeed = new();
        KBScore = new();
        if (FFA_DisableVentingWhenKCDIsUp.GetBool())
        {
            FFALastKill = new();
            FFAVentDuration = new();
            FFAEnterVentTime = new();
        }

        _ = new LateTask( ()=>
        {
            Utils.SetChatVisible();
            RoundTime = FFA_GameTime.GetInt() + 8;
            var now = Utils.GetTimeStamp() + 8;
            foreach (PlayerControl pc in Main.AllAlivePlayerControls)
            {
                KBScore.TryAdd(pc.PlayerId, 0);
                if (FFA_DisableVentingWhenKCDIsUp.GetBool()) FFALastKill.TryAdd(pc.PlayerId, now);
            }
        }, 10f, "Set Chat Visible for Everyone");
    }
    private static void SendRPCSyncFFAPlayer(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncFFAPlayer, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(KBScore[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPCSyncFFAPlayer(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        KBScore[PlayerId] = reader.ReadInt32();
    }
    public static void SendRPCSyncNameNotify(PlayerControl pc)
    {
        if (pc.AmOwner || !pc.IsModClient()) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncFFANameNotify, SendOption.Reliable, pc.GetClientId());
        if (NameNotify.ContainsKey(pc.PlayerId))
            writer.Write(NameNotify[pc.PlayerId].TEXT);
        else writer.Write(string.Empty);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPCSyncNameNotify(MessageReader reader)
    {
        var name = reader.ReadString();
        NameNotify.Remove(PlayerControl.LocalPlayer.PlayerId);
        if (name != null && name != string.Empty)
            NameNotify.Add(PlayerControl.LocalPlayer.PlayerId, (name, 0));
    }
    public static Dictionary<byte, (string TEXT, long TIMESTAMP)> NameNotify = new();
    public static void GetNameNotify(PlayerControl player, ref string name)
    {
        if (Options.CurrentGameMode != CustomGameMode.FFA || player == null) return;
        if (NameNotify.ContainsKey(player.PlayerId))
        {
            name = NameNotify[player.PlayerId].TEXT;
            return;
        }
    }
    public static string GetDisplayScore(byte playerId)
    {
        int rank = GetRankOfScore(playerId);
        string score = KBScore.TryGetValue(playerId, out var s) ? $"{s}" : "Invalid";
        string text = string.Format(GetString("FFADisplayScore"), rank.ToString(), score);
        Color color = Utils.GetRoleColor(CustomRoles.Killer);
        return Utils.ColorString(color, text);
    }
    public static int GetRankOfScore(byte playerId)
    {
        try
        {
            int ms = KBScore[playerId];
            int rank = 1 + KBScore.Values.Where(x => x > ms).Count();
            rank += KBScore.Where(x => x.Value == ms).ToList().IndexOf(new(playerId, ms));
            return rank;
        }
        catch
        {
            return Main.AllPlayerControls.Length;
        }
    }
    public static string GetHudText()
    {
        return string.Format(GetString("FFATimeRemain"), RoundTime.ToString());
    }
    public static void OnPlayerAttack(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null || Options.CurrentGameMode != CustomGameMode.FFA) return;
        if (target.inVent)
        {
            Logger.Info("Target is in a vent, kill blocked", "FFA");
            return;
        }
        var totalalive = Main.AllAlivePlayerControls.Length;
        if (FFAShieldedList.TryGetValue(target.PlayerId, out var dur))
        {
            killer.Notify(GetString("FFA_TargetIsShielded"));
            Logger.Info($"{killer.GetRealName().RemoveHtmlTags()} attacked shielded player {target.GetRealName().RemoveHtmlTags()}, their shield expires in {FFA_ShieldDuration.GetInt() - (Utils.GetTimeStamp() - dur)}s", "FFA");
            if (FFA_ShieldIsOneTimeUse.GetBool())
            {
                FFAShieldedList.Remove(target.PlayerId);
                target.Notify(GetString("FFA_ShieldBroken"));
                Logger.Info($"{target.GetRealName().RemoveHtmlTags()}'s shield was removed because {killer.GetRealName().RemoveHtmlTags()} tried to kill them and the shield is one-time-use according to settings", "FFA");
            }
            return;
        }

        OnPlayerKill(killer);

        SendRPCSyncFFAPlayer(target.PlayerId);

        if (totalalive == 3)
        {
            PlayerControl otherPC = null;
            foreach (var pc in Main.AllAlivePlayerControls.Where(a => a.PlayerId != killer.PlayerId && a.PlayerId != target.PlayerId && a.IsAlive()).ToArray())
            {
                TargetArrow.Add(killer.PlayerId, pc.PlayerId);
                TargetArrow.Add(pc.PlayerId, killer.PlayerId);
                otherPC = pc;
            }
            Logger.Info($"The last 2 players ({killer.GetRealName().RemoveHtmlTags()} & {otherPC?.GetRealName().RemoveHtmlTags()}) now have an arrow toward each other", "FFA");
        }

        if (FFA_EnableRandomAbilities.GetBool())
        {
            bool sync = false;
            bool mark = false;
            var nowKCD = Main.AllPlayerKillCooldown[killer.PlayerId];
            byte EffectType;
            if (Main.NormalOptions.MapId != 4) EffectType = (byte)HashRandom.Next(0, 10);
            else EffectType = (byte)HashRandom.Next(4, 10);
            if (EffectType <= 7) // Buff
            {
                byte EffectID = (byte)HashRandom.Next(0, 3);
                if (Main.NormalOptions.MapId == 4) EffectID = 2;
                switch (EffectID)
                {
                    case 0:
                        FFAShieldedList.TryAdd(killer.PlayerId, Utils.GetTimeStamp());
                        killer.Notify(GetString("FFA-Event-GetShield"), FFA_ShieldDuration.GetFloat());
                        Main.AllPlayerKillCooldown[killer.PlayerId] = FFA_KCD.GetFloat();
                        break;
                    case 1:
                        if (FFAIncreasedSpeedList.ContainsKey(killer.PlayerId))
                        {
                            FFAIncreasedSpeedList.Remove(killer.PlayerId);
                            FFAIncreasedSpeedList.Add(killer.PlayerId, Utils.GetTimeStamp());
                        }
                        else
                        {
                            FFAIncreasedSpeedList.TryAdd(killer.PlayerId, Utils.GetTimeStamp());
                            originalSpeed.TryAdd(killer.PlayerId, Main.AllPlayerSpeed[killer.PlayerId]);
                            Main.AllPlayerSpeed[killer.PlayerId] = FFA_IncreasedSpeed.GetFloat();
                        }
                        killer.Notify(GetString("FFA-Event-GetIncreasedSpeed"), FFA_ModifiedSpeedDuration.GetFloat());
                        Main.AllPlayerKillCooldown[killer.PlayerId] = FFA_KCD.GetFloat();
                        sync = true;
                        break;
                    case 2:
                        Main.AllPlayerKillCooldown[killer.PlayerId] = System.Math.Clamp(FFA_KCD.GetFloat() - 3f, 1f, 60f);
                        killer.Notify(GetString("FFA-Event-GetLowKCD"));
                        sync = true;
                        break;
                    default:
                        Main.AllPlayerKillCooldown[killer.PlayerId] = FFA_KCD.GetFloat();
                        sync = true;
                        break;
                }
            }
            else if (EffectType == 8) // De-Buff
            {
                byte EffectID = (byte)HashRandom.Next(0, 3);
                if (Main.NormalOptions.MapId == 4) EffectID = 1;
                switch (EffectID)
                {
                    case 0:
                        if (FFADecreasedSpeedList.ContainsKey(killer.PlayerId))
                        {
                            FFADecreasedSpeedList.Remove(killer.PlayerId);
                            FFADecreasedSpeedList.Add(killer.PlayerId, Utils.GetTimeStamp());
                        }
                        else
                        {
                            FFADecreasedSpeedList.TryAdd(killer.PlayerId, Utils.GetTimeStamp());
                            originalSpeed.TryAdd(killer.PlayerId, Main.AllPlayerSpeed[killer.PlayerId]);
                            Main.AllPlayerSpeed[killer.PlayerId] = FFA_DecreasedSpeed.GetFloat();
                        }
                        killer.Notify(GetString("FFA-Event-GetDecreasedSpeed"), FFA_ModifiedSpeedDuration.GetFloat());
                        Main.AllPlayerKillCooldown[killer.PlayerId] = FFA_KCD.GetFloat();
                        sync = true;
                        break;
                    case 1:
                        Main.AllPlayerKillCooldown[killer.PlayerId] = System.Math.Clamp(FFA_KCD.GetFloat() + 3f, 1f, 60f);
                        killer.Notify(GetString("FFA-Event-GetHighKCD"));
                        sync = true;
                        break;
                    case 2:
                        FFALowerVisionList.TryAdd(killer.PlayerId, Utils.GetTimeStamp());
                        Main.AllPlayerKillCooldown[killer.PlayerId] = FFA_KCD.GetFloat();
                        killer.Notify(GetString("FFA-Event-GetLowVision"));
                        mark = true;
                        break;
                    default:
                        Main.AllPlayerKillCooldown[killer.PlayerId] = FFA_KCD.GetFloat();
                        sync = true;
                        break;
                }
            }
            else // Mixed
            {
                _ = new LateTask(killer.RpcRandomVentTeleport, 0.5f, "FFA-Event-TP");
                killer.Notify(GetString("FFA-Event-GetTP"));
                Main.AllPlayerKillCooldown[killer.PlayerId] = FFA_KCD.GetFloat();
            }

            if (sync || nowKCD != Main.AllPlayerKillCooldown[killer.PlayerId])
            {
                mark = false;
                killer.SyncSettings();
            }
            if (mark)
            {
                killer.MarkDirtySettings();
            }
        }
        var now = Utils.GetTimeStamp();
        if (FFA_DisableVentingWhenKCDIsUp.GetBool())
        {
            FFALastKill[killer.PlayerId] = now;
            FFAVentDuration[killer.PlayerId] = 0;
            FFAEnterVentTime.Remove(killer.PlayerId);
        }

        killer.RpcMurderPlayerV3(target);
    }

    public static void OnPlayerKill(PlayerControl killer)
    {
        if (PlayerControl.LocalPlayer.Is(CustomRoles.GM))
            PlayerControl.LocalPlayer.KillFlash();

        KBScore[killer.PlayerId]++;
    }

    public static string GetPlayerArrow(PlayerControl seer, PlayerControl target = null)
    {
        if (GameStates.IsMeeting) return string.Empty;
        if (target != null && seer.PlayerId != target.PlayerId) return string.Empty;
        if (Main.AllAlivePlayerControls.Length != 2) return string.Empty;

        string arrows = string.Empty;
        PlayerControl otherPlayer = null;
        foreach (var pc in Main.AllAlivePlayerControls.Where(pc => pc.IsAlive() && pc.PlayerId != seer.PlayerId).ToArray())
        {
            otherPlayer = pc;
            break;
        }
        if (otherPlayer == null) return string.Empty;

        var arrow = TargetArrow.GetArrows(seer, otherPlayer.PlayerId);
        arrows += Utils.ColorString(Utils.GetRoleColor(CustomRoles.Killer), arrow);

        return arrows;
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    class FixedUpdateInGameModeFFAPatch
    {
        private static long LastFixedUpdate;
        public static void Postfix(PlayerControl __instance)
        {
            if (!GameStates.IsInTask || Options.CurrentGameMode != CustomGameMode.FFA) return;

            var now = Utils.GetTimeStamp();

            if (LastFixedUpdate == now) return;
            LastFixedUpdate = now;

            RoundTime--;
            if (AmongUsClient.Instance.AmHost)
            {
                foreach (var pc in Main.AllPlayerControls.Where(pc => NameNotify.TryGetValue(pc.PlayerId, out var nn) && nn.TIMESTAMP < now).ToArray())
                {
                    NameNotify.Remove(pc.PlayerId);
                    SendRPCSyncNameNotify(pc);
                    Utils.NotifyRoles(SpecifySeer: pc, SpecifyTarget: pc);
                }

                var rd = IRandom.Instance;
                byte FFAdoTPdecider = (byte)rd.Next(0, 100);
                bool FFAdoTP = false;
                if (FFAdoTPdecider == 0) FFAdoTP = true;

                if (FFA_EnableRandomTwists.GetBool() && FFAdoTP)
                {
                    Logger.Info("Swap everyone with someone", "FFA");

                    List<byte> changePositionPlayers = new();

                    foreach (PlayerControl pc in Main.AllAlivePlayerControls)
                    {
                        if (changePositionPlayers.Contains(pc.PlayerId) || !pc.IsAlive() || pc.onLadder || pc.inVent) continue;

                        var filtered = Main.AllAlivePlayerControls.Where(a =>
                            pc.IsAlive() && !pc.inVent && a.PlayerId != pc.PlayerId && !changePositionPlayers.Contains(a.PlayerId)).ToArray();
                        if (filtered.Length == 0) break;

                        PlayerControl target = filtered[rd.Next(0, filtered.Length)];

                        if (pc.inVent || target.inVent) continue;

                        changePositionPlayers.Add(target.PlayerId);
                        changePositionPlayers.Add(pc.PlayerId);

                        pc.RPCPlayCustomSound("Teleport");

                        var originPs = target.GetCustomPosition();
                        target.RpcTeleport(pc.GetCustomPosition());
                        pc.RpcTeleport(originPs);

                        target.Notify(Utils.ColorString(new Color32(0, 255, 165, byte.MaxValue), string.Format(GetString("FFA-Event-RandomTP"), pc.GetRealName())));
                        pc.Notify(Utils.ColorString(new Color32(0, 255, 165, byte.MaxValue), string.Format(GetString("FFA-Event-RandomTP"), target.GetRealName())));
                    }

                    changePositionPlayers.Clear();
                }

                if (Main.NormalOptions.MapId == 4) return;

                foreach (PlayerControl pc in Main.AllAlivePlayerControls)
                {
                    if (pc == null) return;

                    bool sync = false;

                    if (FFADecreasedSpeedList.TryGetValue(pc.PlayerId, out var dstime) && dstime + FFA_ModifiedSpeedDuration.GetInt() < now)
                    {
                        Logger.Info(pc.GetRealName() + "'s decreased speed expired", "FFA");
                        FFADecreasedSpeedList.Remove(pc.PlayerId);
                        Main.AllPlayerSpeed[pc.PlayerId] = originalSpeed[pc.PlayerId];
                        originalSpeed.Remove(pc.PlayerId);
                        sync = true;
                    }
                    if (FFAIncreasedSpeedList.TryGetValue(pc.PlayerId, out var istime) && istime + FFA_ModifiedSpeedDuration.GetInt() < now)
                    {
                        Logger.Info(pc.GetRealName() + "'s increased speed expired", "FFA");
                        FFAIncreasedSpeedList.Remove(pc.PlayerId);
                        Main.AllPlayerSpeed[pc.PlayerId] = originalSpeed[pc.PlayerId];
                        originalSpeed.Remove(pc.PlayerId);
                        sync = true;
                    }
                    if (FFALowerVisionList.TryGetValue(pc.PlayerId, out var lvtime) && lvtime + FFA_ModifiedSpeedDuration.GetInt() < now)
                    {
                        Logger.Info(pc.GetRealName() + "'s lower vision effect expired", "FFA");
                        FFALowerVisionList.Remove(pc.PlayerId);
                        sync = true;
                    }
                    if (FFAShieldedList.TryGetValue(pc.PlayerId, out var stime) && stime + FFA_ShieldDuration.GetInt() < now)
                    {
                        Logger.Info(pc.GetRealName() + "'s shield expired", "FFA");
                        FFAShieldedList.Remove(pc.PlayerId);
                    }

                    if (sync) pc.MarkDirtySettings();
                }
            }
        }
    }
}