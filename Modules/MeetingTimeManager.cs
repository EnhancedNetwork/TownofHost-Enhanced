using AmongUs.GameOptions;
using System;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;

namespace TOHE.Modules;

public class MeetingTimeManager
{
    private static int DiscussionTime;
    private static int VotingTime;
    private static int DefaultDiscussionTime;
    private static int DefaultVotingTime;

    public static void Init()
    {
        DefaultDiscussionTime = Main.RealOptionsData.GetInt(Int32OptionNames.DiscussionTime);
        DefaultVotingTime = Main.RealOptionsData.GetInt(Int32OptionNames.VotingTime);
        Logger.Info($"DefaultDiscussionTime:{DefaultDiscussionTime}, DefaultVotingTime{DefaultVotingTime}", "MeetingTimeManager.Init");
        ResetMeetingTime();
    }
    public static void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetInt(Int32OptionNames.DiscussionTime, DiscussionTime);
        opt.SetInt(Int32OptionNames.VotingTime, VotingTime);
    }
    private static void ResetMeetingTime()
    {
        DiscussionTime = DefaultDiscussionTime;
        VotingTime = DefaultVotingTime;
    }
    public static void OnReportDeadBody()
    {
        if (Options.AllAliveMeeting.GetBool() && Utils.IsAllAlive)
        {
            DiscussionTime = 0;
            VotingTime = Options.AllAliveMeetingTime.GetInt();
            Logger.Info($"DiscussionTime:{DiscussionTime}, VotingTime{VotingTime}", "MeetingTimeManager.OnReportDeadBody");
            return;
        }

        ResetMeetingTime();
        int BonusMeetingTime = 0;
        int MeetingTimeMinTimeThief = 0;
        int MeetingTimeMinTimeManager = 0;
        int MeetingTimeMax = 300;

        if (TimeThief.HasEnabled)
        {
            MeetingTimeMinTimeThief = TimeThief.LowerLimitVotingTime.GetInt();
            MeetingTimeMax = TimeThief.MaxMeetingTimeOnAdmired.GetInt();
            BonusMeetingTime += TimeThief.TotalDecreasedMeetingTime();
        }
        if (TimeManager.HasEnabled)
        {
            MeetingTimeMinTimeManager = TimeManager.MadMinMeetingTimeLimit.GetInt();
            MeetingTimeMax = TimeManager.MeetingTimeLimit.GetInt();
            BonusMeetingTime += TimeManager.TotalIncreasedMeetingTime();
        }
        if (CustomRoles.Death.RoleExist())
        {
            BonusMeetingTime += SoulCollector.DeathMeetingTimeIncrease.GetInt();
        }
        int TotalMeetingTime = DiscussionTime + VotingTime;

        if (TimeManager.HasEnabled) BonusMeetingTime = Math.Clamp(TotalMeetingTime + BonusMeetingTime, MeetingTimeMinTimeManager, MeetingTimeMax) - TotalMeetingTime;
        if (TimeThief.HasEnabled) BonusMeetingTime = Math.Clamp(TotalMeetingTime + BonusMeetingTime, MeetingTimeMinTimeThief, MeetingTimeMax) - TotalMeetingTime;
        if (!TimeManager.HasEnabled && !TimeThief.HasEnabled) BonusMeetingTime = Math.Clamp(TotalMeetingTime + BonusMeetingTime, MeetingTimeMinTimeThief, MeetingTimeMax) - TotalMeetingTime;

        if (BonusMeetingTime >= 0)
            VotingTime += BonusMeetingTime; // Extended voting hours
        else
        {
            DiscussionTime += BonusMeetingTime; // Prioritize meeting time reduction
            if (DiscussionTime < 0) // If meeting time alone is not sufficient to cover
            {
                VotingTime += DiscussionTime; // Shorten voting time for missing
                DiscussionTime = 0;
            }
        }
        Logger.Info($"DiscussionTime:{DiscussionTime}, VotingTime{VotingTime}", "MeetingTimeManager.OnReportDeadBody");
    }
}
