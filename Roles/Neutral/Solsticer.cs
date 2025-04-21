using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using TOHE.Modules;
using TOHE.Roles.Core;
using static TOHE.MeetingHudStartPatch;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Solsticer : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Solsticer;
    private const int Id = 26200;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Solsticer);
    public override CustomRoles ThisRoleBase => SolsticerCanVent.GetBool() ? CustomRoles.Engineer : CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralChaos;
    //==================================================================\\

    private static OptionItem EveryOneKnowSolsticer;
    private static OptionItem SolsticerCanVent;
    private static OptionItem SolsticerKnowKiller;
    public static OptionItem SolsticerCanGuess;
    private static OptionItem SolsticerSpeed;
    private static OptionItem AddTasksPreDeadPlayer;
    private static OptionItem RemainingTasksToBeWarned;

    private static byte playerid = byte.MaxValue;
    private static bool patched = false;
    public static int AddShortTasks = 0;
    private static int Count = 0;
    private static bool warningActived = false;
    private static bool CanGuess = true;
    private static string MurderMessage = string.Empty;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Solsticer, 1);
        EveryOneKnowSolsticer = BooleanOptionItem.Create(Id + 10, "EveryOneKnowSolsticer", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Solsticer]);
        SolsticerKnowKiller = BooleanOptionItem.Create(Id + 11, "SolsticerKnowItsKiller", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Solsticer]);
        SolsticerCanVent = BooleanOptionItem.Create(Id + 12, GeneralOption.CanVent, false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Solsticer]);
        SolsticerCanGuess = BooleanOptionItem.Create(Id + 13, GeneralOption.CanGuess, false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Solsticer]);
        SolsticerSpeed = FloatOptionItem.Create(Id + 14, "SolsticerSpeed", new(0, 5, 0.1f), 1.5f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Solsticer]);
        RemainingTasksToBeWarned = IntegerOptionItem.Create(Id + 15, "SolsticerRemainingTaskWarned", new(0, 10, 1), 1, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Solsticer]);
        AddTasksPreDeadPlayer = FloatOptionItem.Create(Id + 16, "SAddTasksPreDeadPlayer", new(0, 15, 0.1f), 0.5f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Solsticer]);
        OverrideTasksData.Create(Id + 17, TabGroup.NeutralRoles, CustomRoles.Solsticer);
    }
    public override void Init()
    {
        playerid = byte.MaxValue;
        warningActived = false;
        patched = false;
        AddShortTasks = 0;
        Count = 0;
        CanGuess = true;
        MurderMessage = string.Empty;
    }

    public override void Add(byte playerId)
    {
        playerid = playerId;

    }
    public override void ApplyGameOptions(IGameOptions opt, byte id)
    {
        AURoleOptions.EngineerCooldown = 1f;
        AURoleOptions.EngineerInVentMaxTime = 0f;
        AURoleOptions.PlayerSpeedMod = !patched ? SolsticerSpeed.GetFloat() : 0.5f;
    } //Enabled Solsticer can vent

    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute) => !ForRecompute;

    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (player == null) return true;

        // Sycn for modded clients
        SendRPC();

        if (patched)
        {
            ResetTasks(player);
        }
        var taskState = player.GetPlayerTaskState();
        if (taskState.IsTaskFinished)
        {
            CustomSoundsManager.RPCPlayCustomSoundAll("Congrats");
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Solsticer);
            CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
        }
        else if (taskState.AllTasksCount - taskState.CompletedTasksCount <= RemainingTasksToBeWarned.GetInt())
        {
            ActiveWarning(player);
        }

        return true;
    }

    public override string GetSuffixOthers(PlayerControl seer, PlayerControl target, bool IsForMeeting = false)
    {
        if (IsForMeeting || !warningActived || seer.Is(CustomRoles.Solsticer)) return string.Empty;
        if (seer.PlayerId != target.PlayerId && !target.Is(CustomRoles.Solsticer)) return string.Empty;

        var warning = "⚠";
        if (IsSolsticerTarget(seer, onlyKiller: true) && !target.Is(CustomRoles.Solsticer))
            warning += TargetArrow.GetArrows(seer, playerid);

        return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Solsticer), warning);
    }
    private void ActiveWarning(PlayerControl pc)
    {
        foreach (var target in Main.AllAlivePlayerControls.Where(x => IsSolsticerTarget(x, onlyKiller: true)).ToArray())
        {
            TargetArrow.Add(target.PlayerId, pc.PlayerId);
        }
        warningActived = true;
        SendRPC();
        Utils.NotifyRoles(SpecifyTarget: pc);
    }
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        if (!GameStates.IsMeeting)
        {
            if (killer.Is(CustomRoles.Quizmaster))
            {
                return true;
            }
            target.RpcTeleport(ExtendedPlayerControl.GetBlackRoomPosition());
            ReportDeadBodyPatch.CanReport[target.PlayerId] = false;
            target.Notify(string.Format(GetString("SolsticerMurdered"), killer.GetRealName()));
            target.RpcGuardAndKill();
            patched = true;
            target.MarkDirtySettings();
            ResetTasks(target);
            if (EveryOneKnowSolsticer.GetBool())
            {
                killer.Notify(GetString("MurderSolsticer"));
                RPC.PlaySoundRPC(killer.PlayerId, Sounds.TaskComplete);
            }
            killer.SetKillCooldown(time: 10f, forceAnime: EveryOneKnowSolsticer.GetBool());
            killer.MarkDirtySettings();
            if (SolsticerKnowKiller.GetBool())
                MurderMessage = string.Format(GetString("SolsticerMurderMessage"), killer.GetRealName(), GetString(killer.GetCustomRole().ToString()));
            else MurderMessage = "";
        }
        return false; //should be patched before every others
    } //My idea is to encourage everyone to kill Solsticer and won't waste shoots on it, only resets cd.
    public override void AfterMeetingTasks()
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
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (lowLoad) return;
        if (patched)
        {
            Count--;

            if (Count > 0) return;

            Count = 4;

            var pos = ExtendedPlayerControl.GetBlackRoomPosition();
            var dis = Utils.GetDistance(pos, player.GetCustomPosition());
            if (dis < 1f)
                return;

            if (!patched) return;
            player.RpcTeleport(pos);
        }
        else if (GameStates.IsInGame)
        {
            var playerId = player.PlayerId;
            if (Main.AllPlayerSpeed[playerId] != SolsticerSpeed.GetFloat())
            {
                Main.AllPlayerSpeed[playerId] = SolsticerSpeed.GetFloat();
                player.MarkDirtySettings();
            }
        }
    }
    private void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player); //SyncSolsticerNotify
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
        writer.Write(playerid);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        Logger.Info("syncsolsticer", "solsticer");
        int AllCount = reader.ReadInt32();
        int CompletedCount = reader.ReadInt32();
        playerid = reader.ReadByte();

        if (AllCount != byte.MaxValue && CompletedCount != byte.MaxValue)
        {
            var taskState = Utils.GetPlayerById(playerid).GetPlayerTaskState();
            taskState.AllTasksCount = AllCount;
            taskState.CompletedTasksCount = CompletedCount;
        }
    }
    public static bool OtherKnowSolsticer(PlayerControl target)
        => target.Is(CustomRoles.Solsticer) && EveryOneKnowSolsticer.GetBool();
    private static bool IsSolsticerTarget(PlayerControl pc, bool onlyKiller)
    {
        return pc.IsAlive() && (!onlyKiller || pc.HasImpKillButton());
    }
    public void ResetTasks(PlayerControl pc)
    {
        SetShortTasksToAdd();
        pc.RpcResetTasks(); //Let taskassign patch decide the tasks
        pc.RpcGuardAndKill();
        pc.Notify(GetString("SolsticerTasksReset"));
        Main.AllPlayerControls.Do(x => TargetArrow.Remove(x.PlayerId, pc.PlayerId));
        warningActived = false;
        SendRPC();
    }
    public static void SetShortTasksToAdd()
    {
        var TotalPlayer = Main.PlayerStates.Count(x => x.Value.Disconnected == false);
        var AlivePlayer = Main.AllAlivePlayerControls.Length;

        AddShortTasks = (int)((TotalPlayer - AlivePlayer) * AddTasksPreDeadPlayer.GetFloat());
    }
    public override bool CheckMisGuessed(bool isUI, PlayerControl pc, PlayerControl target, CustomRoles role, ref bool guesserSuicide)
    {
        var dp = guesserSuicide ? pc : target;
        if (pc.PlayerId == target.PlayerId)
        {
            CanGuess = false;
            _ = new LateTask(() => { Utils.SendMessage(GetString("SolsticerMisGuessed"), dp.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Solsticer), GetString("GuessKillTitle")), true); }, 0.6f, "Solsticer MisGuess Msg");
            return true;
        }
        return false;
    }
    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl pc, CustomRoles role, ref bool guesserSuicide)
    {
        if (role == CustomRoles.Solsticer)
        {
            pc.ShowInfoMessage(isUI, GetString("GuessSolsticer"));
            return true;
        }
        return false;
    }
    public override bool GuessCheck(bool isUI, PlayerControl pc, PlayerControl target, CustomRoles role, ref bool guesserSuicide)
    {
        if (!CanGuess || !SolsticerCanGuess.GetBool())
        {
            pc.ShowInfoMessage(isUI, GetString("SolsticerGuessMax"));
            return true;
        }
        return false;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        patched = false;
    }
    public override void OnMeetingHudStart(PlayerControl pc)
    {
        if (pc.Is(CustomRoles.Solsticer))
        {
            SetShortTasksToAdd();
            if (MurderMessage == "")
                MurderMessage = string.Format(GetString("SolsticerOnMeeting"), AddShortTasks);
            AddMsg(MurderMessage, pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Solsticer), GetString("SolsticerTitle")));
        }
    }
}
