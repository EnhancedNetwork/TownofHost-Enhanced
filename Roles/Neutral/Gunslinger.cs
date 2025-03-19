using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using System.Text.RegularExpressions;
using TOHE.Modules.ChatManager;
using TOHE.Roles.Core;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Translator;
using static TOHE.Utils;


namespace TOHE.Roles.Neutral;
internal class Gunslinger : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 31700;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Gunslinger);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralChaos;
    //==================================================================\\

    private static OptionItem DuelCooldown;
    private static OptionItem OptionTimeBeforeShootout;
    private static OptionItem PointsNeededToWin;
    private static OptionItem TryHideMsg;
    public static OptionItem GunslingerCanVent;
    public static OptionItem GunslingerHasImpostorVision;

    private static readonly Dictionary<byte, bool> DuelFinished = [];
    private static int TimeBeforeShootout = 0;
    private static bool ShootoutActive = false;
    private static bool CanShoot = false;
    private static byte GunslingerTarget;
    private static byte TheGunslinger;
    public static int NumWin = 0;

    public override void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Gunslinger);
        DuelCooldown = FloatOptionItem.Create(Id + 10, "DuelCooldown", new(0f, 300f, 2.5f), 22.5f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gunslinger])
                .SetValueFormat(OptionFormat.Seconds);
        OptionTimeBeforeShootout = FloatOptionItem.Create(Id + 11, "TimeBeforeShootout", new(0f, 90f, 1f), 30f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gunslinger])
                .SetValueFormat(OptionFormat.Seconds);
        TryHideMsg = BooleanOptionItem.Create(Id + 12, "GunslingerTryHideMsg", true, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gunslinger])
            .SetColor(Color.green);
        PointsNeededToWin = IntegerOptionItem.Create(Id + 13, "PointsNeededToWin", new(1, 5, 1), 3, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gunslinger])
            .SetValueFormat(OptionFormat.Times);
        GunslingerCanVent = BooleanOptionItem.Create(Id + 14, "GunslingerCanVent", true, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gunslinger]);
        GunslingerHasImpostorVision = BooleanOptionItem.Create(Id + 15, "GunslingerHasImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gunslinger]);
    }

    public override void Init()
    {
        TheGunslinger = byte.MaxValue;
        GunslingerTarget = byte.MaxValue;
        DuelFinished.Clear();
        NumWin = 0;
        TimeBeforeShootout = -1;
        ShootoutActive = false;
        CanShoot = false;
    }

    public override void Add(byte playerId)
    {
        DuelFinished[playerId] = false;
        playerIdList.Add(playerId);
        TimeBeforeShootout = OptionTimeBeforeShootout.GetInt();
    }

    public override void OnMeetingHudStart(PlayerControl pc)
    {
        if (!HasEnabled || GunslingerTarget == byte.MaxValue) return;

        var tpc = GetPlayerById(GunslingerTarget);
        if (!tpc.IsAlive()) return;

        TimeBeforeShootout = OptionTimeBeforeShootout.GetInt();

        MeetingHudStartPatch.AddMsg(string.Format(GetString("GunslingerMeetingMsg"), (int)TimeBeforeShootout), pc.PlayerId);
        MeetingHudStartPatch.AddMsg(string.Format(GetString("GunslingerTargetMeetingMsg"), (int)TimeBeforeShootout), tpc.PlayerId);
        ShootoutCountdown(TimeBeforeShootout +3, pc);
        CanShoot = true;
    }
//      +3 is there to correct time between countdown and msg

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("GunslingerDuelButtonText"));
    }
    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isMeeting = false)
        => isMeeting && target.PlayerId == GunslingerTarget ? ColorString(GetRoleColor(CustomRoles.Gunslinger), "<size=3> ¤</size>") : string.Empty;

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = DuelCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => GunslingerCanVent.GetBool();
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(Gunslinger.GunslingerHasImpostorVision.GetBool());
    public override string GetProgressText(byte playerId, bool comms)
            => ColorString(GetRoleColor(CustomRoles.Gunslinger).ShadeColor(0.25f), $"({NumWin}/{PointsNeededToWin.GetInt()})");
    
    public void SendRPC(int operate, byte target = byte.MaxValue, int points = -1)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
        writer.Write(operate);
        writer.Write(target);
        if (operate == 1)
        {
            writer.Write(points);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        int operate = reader.ReadInt32();
        byte target = reader.ReadByte();
        byte killer = reader.ReadByte();
        GunslingerTarget = target;
        TheGunslinger = killer;
        if (operate == 1)
        {
            int points = reader.ReadInt32();
            NumWin = points;
        }
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.Is(CustomRoles.NiceMini) && Mini.Age < 18)
        {
            killer.Notify(ColorString(GetRoleColor(CustomRoles.Gangster), GetString("CantDuel")));
            return true;
        }
        if (target.Is(CustomRoles.Pestilence)) return true;
        if (GunslingerTarget != byte.MaxValue)
        {
            killer.Notify(GetString("GunslingerTargetAlreadyChosen"));
            return false;
        }
        Logger.Msg($"{killer.GetNameWithRole()} chose a target {target.GetNameWithRole()}", "Gunslinger");
        GunslingerTarget = target.PlayerId;
        TheGunslinger = killer.PlayerId;
        SendRPC(operate: 0, target: target.PlayerId, points: -1);
        DuelFinished[GunslingerTarget] = false;
        if (!Options.DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(killer);
        else killer.SetKillCooldown();
        return false;
    }

    public override void AfterMeetingTasks()
    {
        if (_Player == null) return;
        var GunslingerId = _state.PlayerId;
        if (NumWin >= PointsNeededToWin.GetInt())
        {
            NumWin = PointsNeededToWin.GetInt();
            if (!CustomWinnerHolder.CheckForConvertedWinner(GunslingerId))
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Gunslinger);
                CustomWinnerHolder.WinnerIds.Add(GunslingerId);
            }
//          this winner check is probably not needed, but keeping it just in case
        }
        ShootoutActive = false;
        CanShoot = false;
        DuelFinished.Clear();
        GunslingerTarget = byte.MaxValue;

        SendRPC(operate: 1, target: byte.MaxValue, points: NumWin);
        foreach (byte playerId in Main.PlayerStates.Values.Where(x => x.MainRole == CustomRoles.Gunslinger).Select(x => x.PlayerId)) { DuelFinished[playerId] = false; }
    }

    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        GunslingerTarget = byte.MaxValue;
        SendRPC(operate: 1, target: byte.MaxValue, points: NumWin);
    }

    public static bool GunslingerDuelCheckMsg(PlayerControl pc, string msg, bool isUI = false)
    {
        var tpc = GetPlayerById(GunslingerTarget);
        var gpc = GetPlayerById(TheGunslinger);
        var originMsg = msg;
        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsMeeting || pc == null || GameStates.IsExilling || !CanShoot) return false;
        if (!pc.Is(CustomRoles.Gunslinger) && GunslingerTarget != pc.PlayerId) return false;

        msg = msg.ToLower().TrimStart().TrimEnd();
        bool operate = false;

        if (CheckCommond(ref msg, "shoot") && pc.IsAlive())
        {
            operate = true;
            if (!ShootoutActive && !pc.Is(CustomRoles.Gunslinger))
            {
                NumWin++;
                 
                if (NumWin == PointsNeededToWin.GetInt() - 1)
                {
                pc.ShowInfoMessage(isUI, GetString("GunslingerShotTooEarly"));
                pc.RpcMurderPlayer(pc);   
                }
                
            }
            if (pc.Is(CustomRoles.Gunslinger) && ShootoutActive)
            {
                NumWin++;
                if (NumWin == PointsNeededToWin.GetInt() - 1)
                {
                tpc.ShowInfoMessage(isUI, GetString("GunslingerLostDuelBySpeed"));
                tpc.RpcMurderPlayer(tpc);
                }
            }
            if (!pc.Is(CustomRoles.Gunslinger) && ShootoutActive)
            {
                gpc.ShowInfoMessage(isUI, GetString("GunslingerLostDuelBySpeed"));
                gpc.RpcMurderPlayer(gpc);
            }
            if (NumWin >= PointsNeededToWin.GetInt())
            {
                NumWin = PointsNeededToWin.GetInt();
                if (!CustomWinnerHolder.CheckForConvertedWinner(gpc.PlayerId))
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Gunslinger);
                    CustomWinnerHolder.WinnerIds.Add(gpc.PlayerId);
                }
            }
            GunslingerTarget = byte.MaxValue;
            ShootoutActive = false;
            CanShoot = false;
        }
        else return false;

        if (operate)
        {

            if (TryHideMsg.GetBool())
            {
                TryHideMsgForGunslingerDuel();
                ChatManager.SendPreviousMessagesToAll();
            }
            else if (pc.AmOwner) SendMessage(originMsg, 255, pc.GetRealName());

        }
        return true;
    }

    public static void TryHideMsgForGunslingerDuel()
    {
        ChatUpdatePatch.DoBlockChat = true;
        List<CustomRoles> roles = CustomRolesHelper.AllRoles.Where(x => x is not CustomRoles.NotAssigned).ToList();
        var rd = IRandom.Instance;
        string msg;
        string[] command = ["shoot"];
        for (int i = 0; i < 20; i++)
        {
            msg = "/";
            if (rd.Next(1, 100) < 20)
            {
                msg += "id";
            }
            else
            {
                msg += command[rd.Next(0, command.Length - 1)];
                msg += " ";
                msg += rd.Next(0, 9).ToString();
            }
            var player = Main.AllAlivePlayerControls.RandomElement();
            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg);
            var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
            writer.StartMessage(-1);
            writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
                .Write(msg)
                .EndRpc();
            writer.EndMessage();
            writer.SendMessage();
        }
        ChatUpdatePatch.DoBlockChat = false;
    }

    public static bool CheckCommond(ref string msg, string command)
    {
        var comList = command.Split('|');
        for (int i = 0; i < comList.Length; i++)
        {
            if (msg.StartsWith("/" + comList[i]))
            {
                msg = msg.Replace("/" + comList[i], string.Empty);
                return true;
            }
        }
        return false;
    }

    private static void ShootoutCountdown(int seconds, PlayerControl pc, bool isUI = false)
    {
        var tpc = GetPlayerById(GunslingerTarget);
        var gpc = GetPlayerById(TheGunslinger);

        if (seconds <= 0) 
        { 
            ShootoutActive = true;
            tpc.ShowInfoMessage(isUI, GetString("GunslingerShoot"));
            gpc.ShowInfoMessage(isUI, GetString("GunslingerShoot"));
            return; 
        }
        TimeBeforeShootout = seconds;

        _ = new LateTask(() => { ShootoutCountdown(seconds - 1, pc); }, 1.01f, "Shootout Countdown");
    }
}
