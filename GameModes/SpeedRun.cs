using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using System.Text;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE;

public static class SpeedRun
{
    private const int Id = 67_225_001;

    public static OptionItem SpeedRun_NumCommonTasks;
    public static OptionItem SpeedRun_NumShortTasks;
    public static OptionItem SpeedRun_NumLongTasks;

    public static OptionItem SpeedRun_ShowChatInGame;

    public static OptionItem SpeedRun_RunnerNormalSpeed;
    public static OptionItem SpeedRun_RunnerKcd;
    public static OptionItem SpeedRun_RunnerKcdPerDeadPlayer;
    public static OptionItem SpeedRun_RunnerSpeedAfterFinishTasks;
    public static OptionItem SpeedRun_AllowCloseDoor;

    public static OptionItem SpeedRun_ArrowPlayers;
    public static OptionItem SpeedRun_ArrowPlayersPlayerLiving; // Only * players left show arrows

    public static OptionItem SpeedRun_SpeedBoostAfterTask;
    public static OptionItem SpeedRun_SpeedBoostSpeed;
    public static OptionItem SpeedRun_SpeedBoostDuration;

    public static OptionItem SpeedRun_ProtectAfterTask;
    public static OptionItem SpeedRun_ProtectDuration;
    public static OptionItem SpeedRun_ProtectOnlyOnce;
    public static OptionItem SpeedRun_ProtectKcd;

    public static OptionItem SpeedRun_EndGameForTime;
    public static OptionItem SpeedRun_MaxTimeForTie;

    public static long StartedAt = 0;
    public static Dictionary<byte, (int, int)> PlayerTaskCounts = [];
    public static Dictionary<byte, long> PlayerTaskFinishedAt = [];
    public static Dictionary<byte, byte> PlayerNumKills = [];

    public static void SetupCustomOption()
    {
        TextOptionItem.Create(10000029, "MenuTitle.SpeedRun", TabGroup.ModSettings)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetColor(new Color32(255, 251, 0, byte.MaxValue));

        SpeedRun_NumCommonTasks = IntegerOptionItem.Create(Id + 1, "SpeedRun_NumCommonTasks", new(0, 10, 1), 1, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetColor(new Color32(255, 251, 0, byte.MaxValue))
            .SetHeader(true);
        SpeedRun_NumShortTasks = IntegerOptionItem.Create(Id + 2, "SpeedRun_NumShortTasks", new(0, 15, 1), 1, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetColor(new Color32(255, 251, 0, byte.MaxValue));
        SpeedRun_NumLongTasks = IntegerOptionItem.Create(Id + 3, "SpeedRun_NumLongTasks", new(0, 15, 1), 1, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetColor(new Color32(255, 251, 0, byte.MaxValue));

        /*
        SpeedRun_ShowChatInGame = BooleanOptionItem.Create(Id + 20, "SpeedRun_ShowChatInGame", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetColor(new Color32(255, 251, 0, byte.MaxValue));
        */

        SpeedRun_RunnerNormalSpeed = FloatOptionItem.Create(Id + 4, "SpeedRun_RunnerNormalSpeed", new(0.25f, 5f, 0.25f), 1.5f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetValueFormat(OptionFormat.Multiplier)
            .SetColor(new Color32(255, 251, 0, byte.MaxValue))
            .SetHeader(true);
        SpeedRun_RunnerKcd = FloatOptionItem.Create(Id + 5, "SpeedRun_RunnerKcd", new(0.5f, 120f, 0.5f), 15f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetColor(new Color32(255, 251, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds);
        SpeedRun_RunnerKcdPerDeadPlayer = FloatOptionItem.Create(Id + 6, "SpeedRun_RunnerKcdPerDeadPlayer", new(0f, 60f, 0.05f), 0f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetColor(new Color32(255, 251, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds);
        SpeedRun_RunnerSpeedAfterFinishTasks = FloatOptionItem.Create(Id + 7, "SpeedRun_RunnerSpeedAfterFinishTasks", new(0.25f, 5f, 0.25f), 2.5f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetColor(new Color32(255, 251, 0, byte.MaxValue))
            .SetValueFormat(OptionFormat.Multiplier);
        SpeedRun_AllowCloseDoor = BooleanOptionItem.Create(Id + 8, "SpeedRun_AllowCloseDoor", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetColor(new Color32(255, 251, 0, byte.MaxValue));

        SpeedRun_ArrowPlayers = BooleanOptionItem.Create(Id + 9, "SpeedRun_ArrowPlayers", true, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetColor(new Color32(255, 251, 0, byte.MaxValue))
            .SetHeader(true);
        SpeedRun_ArrowPlayersPlayerLiving = IntegerOptionItem.Create(Id + 10, "SpeedRun_ArrowPlayersPlayerLiving", new(2, 127, 1), 1, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetParent(SpeedRun_ArrowPlayers, false)
            .SetColor(new Color32(255, 251, 0, byte.MaxValue));

        SpeedRun_SpeedBoostAfterTask = BooleanOptionItem.Create(Id + 11, "SpeedRun_SpeedBoostAfterTask", true, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetColor(new Color32(255, 251, 0, byte.MaxValue))
            .SetHeader(true);
        SpeedRun_SpeedBoostSpeed = FloatOptionItem.Create(Id + 12, "SpeedRun_SpeedBoostSpeed", new(0.25f, 5f, 0.25f), 2.5f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetColor(new Color32(255, 251, 0, byte.MaxValue))
            .SetParent(SpeedRun_SpeedBoostAfterTask, false)
            .SetValueFormat(OptionFormat.Multiplier);
        SpeedRun_SpeedBoostDuration = FloatOptionItem.Create(Id + 13, "SpeedRun_SpeedBoostDuration", new(0.5f, 60f, 0.5f), 2.5f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetColor(new Color32(255, 251, 0, byte.MaxValue))
            .SetParent(SpeedRun_SpeedBoostAfterTask, false)
            .SetValueFormat(OptionFormat.Seconds);

        SpeedRun_ProtectAfterTask = BooleanOptionItem.Create(Id + 14, "SpeedRun_ProtectAfterTask", true, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetColor(new Color32(255, 251, 0, byte.MaxValue))
            .SetHeader(true);
        SpeedRun_ProtectDuration = FloatOptionItem.Create(Id + 15, "SpeedRun_ProtectDuration", new(0.5f, 60f, 0.5f), 2.5f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetColor(new Color32(255, 251, 0, byte.MaxValue))
            .SetParent(SpeedRun_ProtectAfterTask, false)
            .SetValueFormat(OptionFormat.Seconds);
        SpeedRun_ProtectOnlyOnce = BooleanOptionItem.Create(Id + 16, "SpeedRun_ProtectOnlyOnce", true, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetColor(new Color32(255, 251, 0, byte.MaxValue))
            .SetParent(SpeedRun_ProtectAfterTask, false);
        SpeedRun_ProtectKcd = FloatOptionItem.Create(Id + 17, "SpeedRun_ProtectKcd", new(0.5f, 60f, 0.5f), 5f, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetColor(new Color32(255, 251, 0, byte.MaxValue))
            .SetParent(SpeedRun_ProtectAfterTask, false)
            .SetValueFormat(OptionFormat.Seconds);

        SpeedRun_EndGameForTime = BooleanOptionItem.Create(Id + 18, "SpeedRun_EndGameForTime", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetColor(new Color32(255, 251, 0, byte.MaxValue))
            .SetHeader(true);
        SpeedRun_MaxTimeForTie = IntegerOptionItem.Create(Id + 19, "SpeedRun_MaxTimeForTie", new(30, 3600, 15), 600, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.SpeedRun)
            .SetColor(new Color32(255, 251, 0, byte.MaxValue))
            .SetParent(SpeedRun_EndGameForTime, false)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public static void Init()
    {
        StartedAt = GetTimeStamp();
        PlayerTaskCounts = [];
        PlayerTaskFinishedAt = [];
        PlayerNumKills = [];
    }

    public static void RpcSyncSpeedRunStates(byte specificPlayerId = 255) // Not 255, Sync single single player
    {
        if (specificPlayerId != 255)
        {
            if (Main.PlayerStates.TryGetValue(specificPlayerId, out var state) && state.MainRole == CustomRoles.Runner)
            {
                var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncSpeedRunStates, ExtendedPlayerControl.RpcSendOption);
                writer.Write(StartedAt.ToString());
                writer.Write((byte)1);
                writer.Write(specificPlayerId);
                writer.Write((byte)PlayerTaskCounts[specificPlayerId].Item1);
                writer.Write((byte)PlayerTaskCounts[specificPlayerId].Item2);
                writer.Write(PlayerNumKills[specificPlayerId]);
                if (PlayerTaskFinishedAt.ContainsKey(specificPlayerId))
                {
                    writer.Write(true);
                    writer.Write(PlayerTaskFinishedAt[specificPlayerId].ToString());
                }
                else
                {
                    writer.Write(false);
                }
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }

            return;
        }

        {
            var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncSpeedRunStates, ExtendedPlayerControl.RpcSendOption);
            var amount = Main.PlayerStates.Count(x => x.Value.MainRole == CustomRoles.Runner);
            writer.Write(StartedAt.ToString());
            writer.Write((byte)amount);

            foreach (var id in Main.PlayerStates.Where(x => x.Value.MainRole == CustomRoles.Runner).Select(x => x.Value.PlayerId))
            {
                writer.Write(id);
                writer.Write((byte)PlayerTaskCounts[id].Item1);
                writer.Write((byte)PlayerTaskCounts[id].Item2);
                writer.Write(PlayerNumKills[id]);
                if (PlayerTaskFinishedAt.ContainsKey(id))
                {
                    writer.Write(true);
                    writer.Write(PlayerTaskFinishedAt[id].ToString());
                }
                else
                {
                    writer.Write(false);
                }
            }
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }

    public static void HandleSyncSpeedRunStates(MessageReader reader)
    {
        var start = reader.ReadString();
        if (!long.TryParse(start, out StartedAt))
        {
            Logger.Error("Failed to parse StartedAt timestamp from " + start, "HandleSyncSpeedRunStates");
        }

        var amount = reader.ReadByte();
        for (int i = 0; i < amount; i++)
        {
            var id = reader.ReadByte();
            var taskCount = reader.ReadByte();
            var totalTaskCount = reader.ReadByte();
            var numKills = reader.ReadByte();
            var hasFinishedAt = reader.ReadBoolean();
            if (hasFinishedAt)
            {
                var finish = reader.ReadString();
                if (!long.TryParse(finish, out var finishedAt))
                {
                    Logger.Error($"Failed to parse finishedAt timestamp for player {id} {finish}.", "HandleSyncSpeedRunStates");
                }
                else
                {
                    PlayerTaskFinishedAt[id] = finishedAt;
                }
            }
            PlayerTaskCounts[id] = (taskCount, totalTaskCount);
            PlayerNumKills[id] = numKills;
        }
    }

    public static void OnMurderPlayer(PlayerControl killer, PlayerControl target)
    {
        if (killer.PlayerId != target.PlayerId) // Check Disconnect
        {
            PlayerNumKills[killer.PlayerId]++;
            RpcSyncSpeedRunStates(killer.PlayerId);
        }

        RpcSyncSpeedRunStates(target.PlayerId);
        if (!SpeedRun_ArrowPlayers.GetBool()) return;

        var list = Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Runner));
        if (list.Count() <= SpeedRun_ArrowPlayersPlayerLiving.GetInt())
        {
            foreach (var seer in list)
            {
                foreach (var seen in list)
                {
                    if (seer.PlayerId == seen.PlayerId) continue;
                    TargetArrow.Add(seer.PlayerId, seen.PlayerId);
                }
            }
        }

        TargetArrow.RemoveAllTarget(target.PlayerId);
        NotifyRoles();
    }

    public static string GetGameState(bool forGameEnd = false)
    {
        StringBuilder builder = new();
        var playerInfoList = new List<(byte playerId, string playerName, bool isAlive, bool finishedTasks, int kills, long finishTime, int completedTasks, int totalTasks)>();

        foreach (var kvp in Main.PlayerStates)
        {
            var playerId = kvp.Value.PlayerId;
            var playerState = kvp.Value;

            if (kvp.Value.MainRole is not CustomRoles.Runner) continue;

            var playerName = ColorString(Main.PlayerColors.GetValueOrDefault(playerId, Color.white), Main.AllPlayerNames[playerId] ?? "ERROR");
            bool isAlive = !playerState.IsDead;
            bool finishedTasks = false;
            int kills = 0;
            long finishTime = long.MaxValue;
            int completedTasks = 0;
            int totalTasks = 0;

            if (PlayerTaskCounts.ContainsKey(playerId))
            {
                var taskCount = PlayerTaskCounts[playerId];
                completedTasks = taskCount.Item1;
                totalTasks = taskCount.Item2;

                if (taskCount.Item1 >= taskCount.Item2 && taskCount.Item1 != 0)
                {
                    finishedTasks = true;
                    finishTime = PlayerTaskFinishedAt.ContainsKey(playerId) ?
                        PlayerTaskFinishedAt[playerId] - StartedAt : long.MaxValue;
                    kills = PlayerNumKills.ContainsKey(playerId) ? PlayerNumKills[playerId] : 0;
                }
            }

            playerInfoList.Add((playerId, playerName, isAlive, finishedTasks, kills, finishTime, completedTasks, totalTasks));
        }

        // Alive > Finish all tasks > Kill number > Time cost to finish all tasks > Not yet finish tasks then task number
        playerInfoList = playerInfoList.OrderByDescending(p => p.isAlive)
                                     .ThenByDescending(p => p.finishedTasks)
                                     .ThenByDescending(p => p.kills)
                                     .ThenBy(p => p.finishTime)
                                     .ThenByDescending(p => p.completedTasks)
                                     .ToList();

        if (forGameEnd)
        {
            builder.Append(ColorString(new Color32(255, 251, 0, byte.MaxValue), GetString("SpeedRun_LeaderBoard")));
            builder.AppendLine();
        }

        for (int i = 0; i < playerInfoList.Count; i++)
        {
            var info = playerInfoList[i];
            builder.Append((forGameEnd && info.isAlive ? "<#c4aa02>★</color>" : $"{i + 1}. ") + $"{info.playerName} ({(info.isAlive ? ColorString(Color.green, GetString("Alive")) : ColorString(Color.gray, GetString("Death")))})");

            if (info.finishedTasks)
            {
                builder.Append(" " + string.Format(GetString("TaskFinishedSeconds"), info.finishTime));
                builder.Append(" " + string.Format(GetString("KillCount"), info.kills));
            }
            else
            {
                var taskColor = info.isAlive ? Color.yellow : Color.gray;
                builder.Append(ColorString(taskColor, $" ({info.completedTasks}/{info.totalTasks})"));
            }

            // 如果不是最后一个玩家，添加换行符
            if (i < playerInfoList.Count - 1)
            {
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    public static void AppendSpeedRunKcount(StringBuilder builder)
    {
        int aliveKillerCount = Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.Runner) && ((Runner)x.GetRoleClass()).BasisChanged);
        int aliveRunnerCount = Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.Runner) && !((Runner)x.GetRoleClass()).BasisChanged);
        int deadKillerCount = Main.AllPlayerControls.Count(x => x.Is(CustomRoles.Runner) && ((Runner)x.GetRoleClass()).BasisChanged && !x.IsAlive());
        int deadRunnerCount = Main.AllPlayerControls.Count(x => x.Is(CustomRoles.Runner) && !((Runner)x.GetRoleClass()).BasisChanged && !x.IsAlive());

        builder.Append(string.Format(GetString("Remaining.SpeedRunAliveKiller"), aliveKillerCount));
        builder.Append(string.Format("\n\r" + GetString("Remaining.SpeedRunAliveRunner"), aliveRunnerCount));
        builder.Append(string.Format("\n\r" + GetString("Remaining.SpeedRunDeadKiller"), deadKillerCount));
        builder.Append(string.Format("\n\r" + GetString("Remaining.SpeedRunDeadRunner"), deadRunnerCount));
    }
}

class SpeedRunGameEndPredicate : GameEndPredicate
{
    public override bool CheckForEndGame(out GameOverReason reason)
    {
        reason = GameOverReason.ImpostorByKill;

        if (Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.Runner)) <= 1)
        {
            CustomWinnerHolder.WinnerIds.Clear();
            Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Runner)).Select(x => x.PlayerId).Do(x => CustomWinnerHolder.WinnerIds.Add(x));
            Main.DoBlockNameChange = true;
            return true;
        }

        if (SpeedRun.StartedAt != 0 && GetTimeStamp() - SpeedRun.StartedAt >= SpeedRun.SpeedRun_MaxTimeForTie.GetInt())
        {
            reason = GameOverReason.HumansByTask;
            CustomWinnerHolder.WinnerIds.Clear();
            Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Runner)).Select(x => x.PlayerId).Do(x => CustomWinnerHolder.WinnerIds.Add(x));
            Main.DoBlockNameChange = true;
            return true;
        }

        return false;
    }
}

public class Runner : RoleBase
{
    public override CustomRoles Role => CustomRoles.Runner;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.None;
    public override bool IsDesyncRole => false;

    public override bool CanUseSabotage(PlayerControl pc)
    {
        return pc.IsAlive() && BasisChanged;
    }

    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute)
    {
        return !BasisChanged;
    }

    public override bool CanUseKillButton(PlayerControl pc) => BasisChanged && pc.IsAlive();

    public override bool CanUseImpostorVentButton(PlayerControl pc) => false;

    public (bool, float) ProtectState = (false, 0f);
    public (bool, float) SpeedBoostState = (false, 0f);
    private (int, int) LastTaskCount = (0, 0);
    public bool BasisChanged = false;

    public override void Add(byte playerId)
    {
        ProtectState = (false, 0f);
        SpeedBoostState = (false, 0f);
        LastTaskCount = (0, 0);
        BasisChanged = false;

        SpeedRun.PlayerTaskCounts[playerId] = LastTaskCount;
        SpeedRun.PlayerTaskFinishedAt[playerId] = 0;
        SpeedRun.PlayerNumKills[playerId] = 0;
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        opt.SetVision(true);

        float speed;

        if (BasisChanged)
        {
            speed = SpeedRun.SpeedRun_RunnerSpeedAfterFinishTasks.GetFloat();
        }
        else
        {
            if (SpeedBoostState.Item1)
            {
                speed = SpeedRun.SpeedRun_SpeedBoostSpeed.GetFloat();
            }
            else
            {
                speed = SpeedRun.SpeedRun_RunnerNormalSpeed.GetFloat();
            }
        }

        Main.AllPlayerSpeed[playerId] = speed;
        AURoleOptions.PlayerSpeedMod = speed;
    }

    public void SendRPC()
    {
        var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, ExtendedPlayerControl.RpcSendOption);
        writer.WriteNetObject(_Player);
        writer.Write(BasisChanged);
        writer.Write(ProtectState.Item1);
        writer.Write(ProtectState.Item2);
        writer.Write(SpeedBoostState.Item1);
        writer.Write(SpeedBoostState.Item2);
        writer.Write((byte)LastTaskCount.Item1);
        writer.Write((byte)LastTaskCount.Item2);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        BasisChanged = reader.ReadBoolean();
        ProtectState = (reader.ReadBoolean(), reader.ReadSingle());
        SpeedBoostState = (reader.ReadBoolean(), reader.ReadSingle());
        LastTaskCount = (reader.ReadByte(), reader.ReadByte());
    }

    public override void SetKillCooldown(byte id)
    {
        var deadnum = Main.AllPlayerControls.Count(x => x.Is(CustomRoles.Runner)) - Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.Runner));
        Main.AllPlayerKillCooldown[id] = SpeedRun.SpeedRun_RunnerKcd.GetFloat() - SpeedRun.SpeedRun_RunnerKcdPerDeadPlayer.GetFloat() * deadnum;
    }

    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody, PlayerControl killer)
    {
        return false;
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        var changed = false;
        if (ProtectState.Item1)
        {
            ProtectState.Item2 -= Time.fixedDeltaTime;

            if (ProtectState.Item2 <= 0)
            {
                ProtectState = (false, 0f);
                changed = true;
            }
        }

        if (SpeedBoostState.Item1)
        {
            SpeedBoostState.Item2 -= Time.fixedDeltaTime;
            if (SpeedBoostState.Item2 <= 0)
            {
                SpeedBoostState = (false, 0f);
                player.MarkDirtySettings();
                changed = true;
            }
        }

        if (changed)
        {
            SendRPC();
            NotifyRoles();
        }
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!BasisChanged)
        {
            return false;
        }

        var targetRoleClass = target.GetRoleClass();

        if (targetRoleClass.Role != CustomRoles.Runner)
        {
            return false;
        }

        Runner targetRole = targetRoleClass as Runner;

        if (targetRole.ProtectState.Item1)
        {
            if (SpeedRun.SpeedRun_ProtectOnlyOnce.GetBool())
            {
                targetRole.ProtectState = (false, 0f);
                targetRole.SendRPC();
            }

            killer.SetKillCooldown(SpeedRun.SpeedRun_ProtectKcd.GetFloat(), target, true);
            killer.ResetKillCooldown();

            target.RpcGuardAndKill(target);
            return false;
        }

        return true;
    }

    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        SpeedBoostState = (false, 0f);
        ProtectState = (false, 0f);
        target.MarkDirtySettings();
        SpeedRun.OnMurderPlayer(killer, target); // Disconnect also handled here.
        SendRPC();

        target.RpcSetRoleType(RoleTypes.CrewmateGhost, true);
    }

    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (!player.IsAlive()) return true;

        LastTaskCount = (completedTaskCount, totalTaskCount);

        if (SpeedRun.SpeedRun_SpeedBoostAfterTask.GetBool() && !BasisChanged)
        {
            SpeedBoostState = (true, SpeedRun.SpeedRun_SpeedBoostDuration.GetFloat());
            player.MarkDirtySettings();
        }

        if (SpeedRun.SpeedRun_ProtectAfterTask.GetBool() && !BasisChanged)
        {
            ProtectState = (true, SpeedRun.SpeedRun_ProtectDuration.GetFloat());
            player.RpcSpecificProtectPlayer(player, player.CurrentOutfit.ColorId);
        }

        if (completedTaskCount >= totalTaskCount && !BasisChanged)
        {
            BasisChanged = true;
            player.RpcSetRoleDesync(RoleTypes.Impostor, player.OwnerId);
            player.MarkDirtySettings();
            SpeedRun.PlayerTaskFinishedAt[player.PlayerId] = GetTimeStamp();
        }

        SpeedRun.PlayerTaskCounts[player.PlayerId] = LastTaskCount;
        SpeedRun.RpcSyncSpeedRunStates(player.PlayerId);

        SendRPC();
        return true;
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (seer.PlayerId == seen.PlayerId)
        {
            if (ProtectState.Item1)
            {
                return ColorString(GetRoleColor(CustomRoles.Medic), "✚");
            }
        }

        return "";
    }

    public override string GetMarkOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (seer.PlayerId == seen.PlayerId) return "";

        if (ProtectState.Item1)
        {
            return ColorString(GetRoleColor(CustomRoles.Medic), "✚");
        }

        return "";
    }

    public override string GetSuffix(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (target != null && seer.PlayerId != target.PlayerId) return "";
        if (!seer.IsAlive() || !SpeedRun.SpeedRun_ArrowPlayers.GetBool()) return "";

        var listing = Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Runner));

        if (listing.Count() > SpeedRun.SpeedRun_ArrowPlayersPlayerLiving.GetInt()) return "";

        var arrows = TargetArrow.GetAllArrows(seer);

        return arrows;
    }

    public override string GetProgressText(byte playerId, bool comms)
    {
        if (!BasisChanged)
        {
            if (_Player.IsAlive())
            {
                return GetTaskCount(playerId, comms);
            }
            else
            {
                return ColorString(Color.gray, $"({LastTaskCount.Item1}/{LastTaskCount.Item2})");
            }
        }
        else
        {
            return ColorString(Color.red, string.Format(GetString("KillCount"), SpeedRun.PlayerNumKills[playerId]));
        }
    }

    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target) => seer.Is(CustomRoles.Runner);

    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target) => Main.roleColors[target.GetCustomRole()];
}
