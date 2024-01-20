using HarmonyLib;
using Hazel;
using System.Linq;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral
{
    public static class Solsticer
    {
        private static readonly int Id = 26200;

        public static OptionItem EveryOneKnowSolsticer;
        public static OptionItem SolsticerCanVent;
        public static OptionItem SolsticerKnowKiller;
        public static OptionItem SolsticerCanGuess;
        public static OptionItem SolsticerSpeed;
        public static OverrideTasksData SolsticerTasks;
        public static OptionItem AddTasksPreDeadPlayer;
        private static OptionItem RemainingTasksToBeWarned;

        public static byte playerid;
        public static bool patched;
        public static int AddShortTasks;
        public static bool warningActived;
        public static bool CanGuess;
        public static string MurderMessage;
        public static void SetupCustomOption()
        {
            SetupSingleRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.Solsticer, 1);
            EveryOneKnowSolsticer = BooleanOptionItem.Create(Id + 10, "EveryOneKnowSolsticer", true, TabGroup.OtherRoles, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Solsticer]);
            SolsticerKnowKiller = BooleanOptionItem.Create(Id + 11, "SolsticerKnowItsKiller", true, TabGroup.OtherRoles, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Solsticer]);
            SolsticerCanVent = BooleanOptionItem.Create(Id + 12, "CanVent", false, TabGroup.OtherRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Solsticer]);
            SolsticerCanGuess = BooleanOptionItem.Create(Id + 13, "CanGuess", false, TabGroup.OtherRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Solsticer]);
            SolsticerSpeed = FloatOptionItem.Create(Id + 14, "SolsticerSpeed", new(0, 5, 0.1f), 1.5f, TabGroup.OtherRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Solsticer]);
            RemainingTasksToBeWarned = IntegerOptionItem.Create(Id + 15, "SolsticerRemainingTaskWarned", new(0, 10, 1), 1, TabGroup.OtherRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Solsticer]);
            AddTasksPreDeadPlayer = FloatOptionItem.Create(Id + 16, "SAddTasksPreDeadPlayer", new(0, 15, 0.1f), 0.5f, TabGroup.OtherRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Solsticer]);
            SolsticerTasks = OverrideTasksData.Create(Id + 17, TabGroup.OtherRoles, CustomRoles.Solsticer);
        }
        public static void Init()
        {
            playerid = byte.MaxValue;
            warningActived = false;
            patched = false;
            AddShortTasks = 0;
            Count = 0;
            CanGuess = true;
            MurderMessage = "";
        }
        
        public static void Add(byte playerId)
        {
            playerid = playerId;
        }
        public static void ApplyGameOptions()
        {
            AURoleOptions.EngineerCooldown = 0f;
            AURoleOptions.EngineerInVentMaxTime = 0f;
            AURoleOptions.PlayerSpeedMod = !patched ? SolsticerSpeed.GetFloat() : 0.5f;
        } //Enabled Solsticer can vent
        public static void OnCompleteTask(PlayerControl player)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (player == null || !player.Is(CustomRoles.Solsticer)) return;
            if (patched)
            {
                ResetTasks(player);
            }
            var taskState = player.GetPlayerTaskState();
            if (taskState.IsTaskFinished)
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Solsticer);
                CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
            }
            else if (taskState.AllTasksCount - taskState.CompletedTasksCount <= RemainingTasksToBeWarned.GetInt())
            {
                ActiveWarning(player);
            }
        }
        public static bool IsSolsticerTarget(this PlayerControl pc, bool onlyKiller)
        {
            return pc.IsAlive() && (!onlyKiller || pc.HasImpKillButton());
        }
        public static string GetWarningArrow(PlayerControl seer, PlayerControl target)
        {
            if (GameStates.IsMeeting || !warningActived) return "";
            if (seer.Is(CustomRoles.Solsticer)) return "";

            var warning = "⚠";
            if (seer.IsSolsticerTarget(onlyKiller: true) && !target.Is(CustomRoles.Solsticer))
                warning += TargetArrow.GetArrows(seer, playerid);

            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Solsticer), warning);
        }
        public static void ActiveWarning(PlayerControl pc)
        {
            foreach (var target in Main.AllAlivePlayerControls.Where(x => x.IsSolsticerTarget(onlyKiller: true)).ToArray())
            {
                TargetArrow.Add(target.PlayerId, pc.PlayerId);
            }
            if (AmongUsClient.Instance.AmHost)
            {
                warningActived = true;
                SendRPC();
                Utils.NotifyRoles(ForceLoop: true);
            }
        }
        public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            if (killer == null || target == null) return false;
            if (!GameStates.IsMeeting)
            {
                if (killer.Is(CustomRoles.Quizmaster))
                {
                    return false;
                }
                Utils.RpcTeleport(target, ExtendedPlayerControl.GetBlackRoomPosition());
                ReportDeadBodyPatch.CanReport[target.PlayerId] = false;
                NameNotifyManager.Notify(target, string.Format(GetString("SolsticerMurdered"), killer.GetRealName()));
                target.RpcGuardAndKill();
                patched = true;
                target.MarkDirtySettings();
                ResetTasks(target);
                if (EveryOneKnowSolsticer.GetBool())
                {
                    NameNotifyManager.Notify(killer, GetString("MurderSolsticer"));
                    RPC.PlaySoundRPC(killer.PlayerId, Sounds.TaskComplete);
                }
                killer.SetKillCooldown(time: 10f, forceAnime: EveryOneKnowSolsticer.GetBool());
                killer.MarkDirtySettings();
                if (SolsticerKnowKiller.GetBool())
                    MurderMessage = string.Format(GetString("SolsticerMurderMessage"), killer.GetRealName(), GetString(killer.GetCustomRole().ToString()));
                else MurderMessage = "";
            }
            return true; //should be patched before every others
        } //My idea is to encourage everyone to kill Solsticer and won't waste shoots on it, only resets cd.
        public static void AfterMeetingTasks()
        {
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Solsticer)).ToArray())
            {
                Main.AllPlayerSpeed[pc.PlayerId] = SolsticerSpeed.GetFloat();
                ReportDeadBodyPatch.CanReport[pc.PlayerId] = true;
                pc.MarkDirtySettings();
                ResetTasks(pc);
            }
            MurderMessage = "";
            patched = false;
        }
        private static int Count;
        public static void OnFixedUpdate(PlayerControl pc)
        {
            if (patched && GameStates.IsInTask)
            {
                Count--;

                if (Count > 0) return;

                Count = 15;

                var pos = ExtendedPlayerControl.GetBlackRoomPosition();
                var dis = Vector2.Distance(pos, pc.GetCustomPosition());
                if (dis < 1f)
                    return;

                if (GameStates.IsMeeting || !patched) return;
                pc.RpcTeleport(pos);
            }
            else if (GameStates.IsInGame)
            {
                if (Main.AllPlayerSpeed[pc.PlayerId] != SolsticerSpeed.GetFloat())
                {
                    Main.AllPlayerSpeed[pc.PlayerId] = SolsticerSpeed.GetFloat();
                    pc.MarkDirtySettings();
                }
            }
        }
        public static void SendRPC()
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncSolsticerNotify, SendOption.Reliable, -1);
            var taskState = Utils.GetPlayerById(playerid).GetPlayerTaskState();
            if (taskState != null)
            {
                writer.Write(taskState.AllTasksCount);
                writer.Write(taskState.CompletedTasksCount);
            }
            else
            {
                writer.Write(0);
                writer.Write(0);
            }
            writer.Write(warningActived);
            writer.Write(playerid);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            Logger.Info("syncsolsticer", "solsticer");
            int AllCount = reader.ReadInt32();
            int CompletedCount = reader.ReadInt32();
            warningActived = reader.ReadBoolean();
            playerid = reader.ReadByte();

            if (AllCount != byte.MaxValue && CompletedCount != byte.MaxValue) 
            {
                var taskState = Utils.GetPlayerById(playerid).GetPlayerTaskState();
                taskState.AllTasksCount = AllCount;
                taskState.CompletedTasksCount = CompletedCount;
            }

            if (warningActived)
            {
                ActiveWarning(Utils.GetPlayerById(playerid));
            }
        }
        public static void ResetTasks(PlayerControl pc)
        {
            SetShortTasksToAdd();
            var taskState = pc.GetPlayerTaskState();
            GameData.Instance.RpcSetTasks(pc.PlayerId, System.Array.Empty<byte>()); //Let taskassign patch decide the tasks
            taskState.CompletedTasksCount = 0;
            pc.RpcGuardAndKill();
            NameNotifyManager.Notify(pc, GetString("SolsticerTasksReset"));
            Main.AllPlayerControls.Do(x => TargetArrow.Remove(x.PlayerId, pc.PlayerId));
            warningActived = false;
            SendRPC();
        }
        public static void SetShortTasksToAdd()
        {
            var TotalPlayer = Main.PlayerStates.Count(x => x.Value.deathReason != PlayerState.DeathReason.Disconnected);
            var AlivePlayer = Main.AllAlivePlayerControls.Length;

            AddShortTasks = (int)((TotalPlayer - AlivePlayer) * AddTasksPreDeadPlayer.GetFloat());
        }
    }
}
