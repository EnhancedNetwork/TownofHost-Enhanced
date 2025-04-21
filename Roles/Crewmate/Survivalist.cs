using AmongUs.GameOptions;
using TOHE.Modules;
using static TOHE.Options;
using static TOHE.Translator;
using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;
using TOHE.Roles.AddOns.Common;

namespace TOHE.Roles.Crewmate;

internal class Survivalist : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Survivalist;
    private const int Id = 35500;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem ShowdownDuration;
    private static long ShowdownStartTime;
    private static bool InShowdown = false;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Survivalist, 1);
        ShowdownDuration = FloatOptionItem.Create(Id + 10, "SurvivalistShowdownDuration", new(10f, 120f, 5f), 70f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Survivalist])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Init()
    {
        ShowdownStartTime = -1;
        InShowdown = false;
    }

    public override void Add(byte playerId)
    {
        // No special initialization needed
    }

    private static long Time;

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (!AmongUsClient.Instance.AmHost || lowLoad || !InShowdown) return;

        var remainingTime = ShowdownStartTime + (long)ShowdownDuration.GetFloat() - nowTime;
        Time = remainingTime;

        if (remainingTime <= 0)
        {
            EndShowdown(true); // Survivalist wins
        }
        else
        {
            // Notify all players about remaining time
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (pc.Is(CustomRoles.Survivalist)) continue;
                pc.Notify(string.Format(GetString("SurvivalistShowdownCountdown"), remainingTime), sendInLog: false);
            }
        }
    }

    private static void StartShowdown()
    {
        if (InShowdown) return;

        InShowdown = true;
        ShowdownStartTime = Utils.GetTimeStamp();

        // Disable all abilities except for Survivalist and killers
        foreach (var pc in Main.AllPlayerControls)
        {
            if (!pc.Is(CustomRoles.Survivalist) && !IsThreat(pc))
            {
                pc.RpcGuardAndKill(pc); // Prevent movement
            }
        }
    }

    private static void EndShowdown(bool survivalistWins)
    {
        if (!InShowdown) return;

        InShowdown = false;
        ShowdownStartTime = -1;

        if (survivalistWins)
        {
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
            foreach (var pc in Main.AllPlayerControls)
            {
                if (pc.Is(Custom_Team.Crewmate) && !pc.Is(CustomRoles.Survivalist))
                {
                    CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                }
            }
            CustomWinnerHolder.WinnerIds.Add(Main.AllPlayerControls.FirstOrDefault(x => x.Is(CustomRoles.Survivalist))?.PlayerId ?? byte.MaxValue);
        }
        else
        {
            // Original winning team wins
        }

        // Re-enable movement for all players
        foreach (var pc in Main.AllPlayerControls)
        {
            pc.RpcGuardAndKill(null);
        }
    }

    private static bool IsThreat(PlayerControl pc)
    {
        return pc.GetCustomRole().IsImpostor();
    }

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (!InShowdown || !target.Is(CustomRoles.Survivalist)) return true;

        // Survivalist was killed during showdown
        EndShowdown(false);
        return true;
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (seer == null || !seer.IsAlive() || isForMeeting || !isForHud) return string.Empty;

        if (InShowdown && seer.Is(CustomRoles.Survivalist))
        {
            var remainingTime = ShowdownStartTime + (long)ShowdownDuration.GetFloat() - Utils.GetTimeStamp();
            return string.Format(GetString("SurvivalistShowdownStatus"), remainingTime + 1);
        }

        return string.Empty;
    }

    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody, PlayerControl killer)
    {
        if (InShowdown)
        {
            return false;
        }
        else return base.OnCheckReportDeadBody(reporter, deadBody, killer);
    }
    public override bool OnCheckStartMeeting(PlayerControl reporter)
    {
        if (InShowdown)
        {
            return false;
        }
        else return base.OnCheckStartMeeting(reporter);
    }


    // This method should be called from CheckGameEndPatch when game is about to end
    public static bool CheckForShowdown()
    {
        if (InShowdown) return true;

        var survivalist = Main.AllAlivePlayerControls.FirstOrDefault(p => p.Is(CustomRoles.Survivalist));
        if (survivalist == null) return false;

        // Check if game is ending with non-crew win
        if (CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor)
        {
            StartShowdown();
            CustomWinnerHolder.Reset();
            return true;
        }

        return false;
    }
}
