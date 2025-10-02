using AmongUs.GameOptions;
using System.Text;
using TOHE.Modules;
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
        AbilityLeftInRound.Clear();
        LastReport.Clear();
    }
    public override void Add(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);

        playerId.SetAbilityUseLimit(0);
        AbilityLeftInRound[playerId] = MaxEaten.GetInt();
        LastReport[playerId] = GetTimeStamp();

        CustomRoleManager.CheckDeadBodyOthers.Add(CheckDeadBody);

        if (AmongUsClient.Instance.AmHost)
        {
            _ = new LateTask(() =>
            {
                if (GameStates.IsInTask)
                {
                    var player = playerId.GetPlayer();
                    if (player == null) return;

                    if (!DisableShieldAnimations.GetBool()) player.RpcGuardAndKill(player);
                    player.Notify(GetString("VultureCooldownUp"));
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

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (lowLoad || !player.IsAlive()) return;

        if (player.GetAbilityUseLimit() >= NumberOfReportsToWin.GetInt())
        {
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
        pc.RPCPlayCustomSound("Eat");
        pc.RpcIncreaseAbilityUseLimitBy(1);
        AbilityLeftInRound[pc.PlayerId]--;
        Logger.Msg($"target is null? {target == null}", "VultureNull");

        if (target != null)
        {
            foreach (var apc in playerIdList)
            {
                LocateArrow.Remove(apc, target.GetDeadBody().transform.position);
            }
        }
        pc.Notify(GetString("VultureBodyReported"));
        Main.UnreportableBodies.Remove(target.PlayerId);
        Main.UnreportableBodies.Add(target.PlayerId);
    }
    public override void AfterMeetingTasks()
    {
        foreach (var apc in playerIdList)
        {
            var player = GetPlayerById(apc);
            if (player == null || !player.IsAlive()) continue;

            AbilityLeftInRound[apc] = MaxEaten.GetInt();
            LastReport[apc] = GetTimeStamp();
        }
    }
    public override void NotifyAfterMeeting()
    {
        foreach (var apc in playerIdList)
        {
            var player = apc.GetPlayer();
            if (player == null) continue;

            LocateArrow.RemoveAllTarget(apc);

            _ = new LateTask(() =>
            {
                if (GameStates.IsInTask && player.IsAlive())
                {
                    if (!DisableShieldAnimations.GetBool()) player.RpcGuardAndKill(GetPlayerById(apc));
                    player.Notify(GetString("VultureCooldownUp"));
                }
                return;
            }, VultureReportCD.GetFloat(), "Vulture Cooldown Up After Meeting");
        }
    }
    private void CheckDeadBody(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        var vulture = _Player;
        if (!vulture.IsAlive() || inMeeting || target.IsDisconnected()) return;
        if (!ArrowsPointingToDeadBody.GetBool()) return;

        LocateArrow.Add(vulture.PlayerId, target.Data.GetDeadBody().transform.position);
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
    {
        var ProgressText = new StringBuilder();
        Color TextColor = GetRoleColor(CustomRoles.Vulture).ShadeColor(0.25f);

        ProgressText.Append(ColorString(TextColor, ColorString(Color.white, " - ") + $"({playerId.GetAbilityUseLimit()}/{NumberOfReportsToWin.GetInt()})"));
        return ProgressText.ToString();
    }
}
