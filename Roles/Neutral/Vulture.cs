using AmongUs.GameOptions;
using Hazel;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Neutral;

internal class Vulture : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Vulture;
    private const int Id = 15600;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();

    public override CustomRoles ThisRoleBase => CanVent.GetBool() ? CustomRoles.Engineer : CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralChaos;
    //==================================================================\\

    private static OptionItem ArrowsPointingToDeadBody;
    private static OptionItem NumberOfReportsToWin;
    private static OptionItem CanVent;
    private static OptionItem VultureReportCD;
    private static OptionItem MaxEaten;
    private static OptionItem HasImpVision;

    private static readonly Dictionary<byte, int> BodyReportCount = [];
    private static readonly Dictionary<byte, int> AbilityLeftInRound = [];
    private static readonly Dictionary<byte, long> LastReport = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Vulture);
        ArrowsPointingToDeadBody = BooleanOptionItem.Create(Id + 10, "VultureArrowsPointingToDeadBody", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Vulture]);
        NumberOfReportsToWin = IntegerOptionItem.Create(Id + 11, "VultureNumberOfReportsToWin", new(1, 14, 1), 5, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Vulture]);
        CanVent = BooleanOptionItem.Create(Id + 12, GeneralOption.CanVent, true, TabGroup.NeutralRoles, true).SetParent(CustomRoleSpawnChances[CustomRoles.Vulture]);
        VultureReportCD = FloatOptionItem.Create(Id + 13, "VultureReportCooldown", new(0f, 180f, 2.5f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Vulture])
                .SetValueFormat(OptionFormat.Seconds);
        MaxEaten = IntegerOptionItem.Create(Id + 14, "VultureMaxEatenInOneRound", new(1, 14, 1), 1, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Vulture]);
        HasImpVision = BooleanOptionItem.Create(Id + 15, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Vulture]);
    }
    public override void Init()
    {
        playerIdList.Clear();
        BodyReportCount.Clear();
        AbilityLeftInRound.Clear();
        LastReport.Clear();
    }
    public override void Add(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);

        BodyReportCount[playerId] = 0;
        AbilityLeftInRound[playerId] = MaxEaten.GetInt();
        LastReport[playerId] = GetTimeStamp();

        CustomRoleManager.CheckDeadBodyOthers.Add(CheckDeadBody);

        if (AmongUsClient.Instance.AmHost)
        {
            _ = new LateTask(() =>
            {
                if (GameStates.IsInTask)
                {
                    if (!DisableShieldAnimations.GetBool()) GetPlayerById(playerId).RpcGuardAndKill(GetPlayerById(playerId));
                    GetPlayerById(playerId).Notify(GetString("VultureCooldownUp"));
                }
                return;
            }, VultureReportCD.GetFloat() + 8f, "Vulture Cooldown Up In Start");  //for some reason that idk vulture cd completes 8s faster when the game starts, so I added 8f for now 
        }
    }

    public override void ApplyGameOptions(IGameOptions opt, byte id)
    {
        opt.SetVision(HasImpVision.GetBool());
        AURoleOptions.EngineerCooldown = 1f;
        AURoleOptions.EngineerInVentMaxTime = 0f;
    }

    private static void SendBodyRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncVultureBodyAmount, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(BodyReportCount[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveBodyRPC(MessageReader reader)
    {
        byte playerId = reader.ReadByte();
        int body = reader.ReadInt32();

        if (!BodyReportCount.ContainsKey(playerId))
        {
            BodyReportCount.Add(playerId, body);
        }
        else
            BodyReportCount[playerId] = body;
    }
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (lowLoad || !player.IsAlive()) return;

        if (BodyReportCount[player.PlayerId] >= NumberOfReportsToWin.GetInt())
        {
            BodyReportCount[player.PlayerId] = NumberOfReportsToWin.GetInt();
            if (!CustomWinnerHolder.CheckForConvertedWinner(player.PlayerId))
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Vulture);
                CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
            }
        }
    }
    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody, PlayerControl killer)
    {
        if (Main.UnreportableBodies.Contains(deadBody.PlayerId)) return false;

        if (reporter.Is(CustomRoles.Vulture))
        {
            if (deadBody.Object.Is(CustomRoles.Stubborn))
            {
                reporter.Notify(Translator.GetString("StubbornNotify"));
                return false;
            }
            var reporterId = reporter.PlayerId;
            long now = GetTimeStamp();
            if ((AbilityLeftInRound[reporterId] > 0) && (now - LastReport[reporterId] > (long)VultureReportCD.GetFloat()))
            {
                LastReport[reporterId] = now;

                OnEatDeadBody(reporter, deadBody);
                reporter.RpcGuardAndKill(reporter);
                reporter.Notify(GetString("VultureReportBody"));
                if (AbilityLeftInRound[reporterId] > 0)
                {
                    _ = new LateTask(() =>
                    {
                        if (GameStates.IsInTask)
                        {
                            if (!DisableShieldAnimations.GetBool()) reporter.RpcGuardAndKill(reporter);
                            reporter.Notify(GetString("VultureCooldownUp"));
                        }
                        return;
                    }, VultureReportCD.GetFloat(), "Vulture CD");
                }

                Logger.Info($"{reporter.GetRealName()} ate {deadBody.PlayerName} corpse", "Vulture");
                return false;
            }
        }
        return true;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        foreach (var apc in playerIdList)
        {
            LocateArrow.RemoveAllTarget(apc);
        }
    }
    private static void OnEatDeadBody(PlayerControl pc, NetworkedPlayerInfo target)
    {
        BodyReportCount[pc.PlayerId]++;
        AbilityLeftInRound[pc.PlayerId]--;
        Logger.Msg($"target is null? {target == null}", "VultureNull");
        if (target != null)
        {
            foreach (var apc in playerIdList)
            {
                LocateArrow.Remove(apc, target.GetDeadBody().transform.position);
            }
        }
        SendBodyRPC(pc.PlayerId);
        pc.Notify(GetString("VultureBodyReported"));
        Main.UnreportableBodies.Remove(target.PlayerId);
        Main.UnreportableBodies.Add(target.PlayerId);
    }
    public override void AfterMeetingTasks()
    {
        foreach (var apc in playerIdList)
        {
            var player = GetPlayerById(apc);
            if (player == null) continue;

            if (player.IsAlive())
            {
                AbilityLeftInRound[apc] = MaxEaten.GetInt();
                LastReport[apc] = GetTimeStamp();
            }
            SendBodyRPC(player.PlayerId);
        }
    }
    public override void NotifyAfterMeeting()
    {
        foreach (var apc in playerIdList)
        {
            var player = GetPlayerById(apc);
            if (player == null) continue;

            _ = new LateTask(() =>
            {
                if (GameStates.IsInTask && player.IsAlive())
                {
                    if (!DisableShieldAnimations.GetBool()) player.RpcGuardAndKill(GetPlayerById(apc));
                    player.Notify(GetString("VultureCooldownUp"));
                }
                return;
            }, VultureReportCD.GetFloat(), "Vulture Cooldown Up After Meeting");
            SendBodyRPC(player.PlayerId);
        }
    }
    private void CheckDeadBody(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        if (inMeeting || target.IsDisconnected()) return;
        if (!ArrowsPointingToDeadBody.GetBool()) return;

        foreach (var pc in playerIdList.ToArray())
        {
            var player = pc.GetPlayer();
            if (player == null || !player.IsAlive()) continue;
            LocateArrow.Add(pc, target.Data.GetDeadBody().transform.position);
        }
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false)
    {
        if (isForMeeting || seer.PlayerId != target.PlayerId) return string.Empty;

        return ColorString(Color.white, LocateArrow.GetArrows(seer));
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.ReportButton.OverrideText(GetString("VultureEatButtonText"));
    }
    public override Sprite ReportButtonSprite => CustomButton.Get("Eat");
    public override string GetProgressText(byte playerId, bool comms)
        => ColorString(GetRoleColor(CustomRoles.Vulture).ShadeColor(0.25f), $"({(BodyReportCount.TryGetValue(playerId, out var count1) ? count1 : 0)}/{NumberOfReportsToWin.GetInt()})");
}
