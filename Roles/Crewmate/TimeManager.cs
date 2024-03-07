using System.Collections.Generic;
using TOHE.Roles.Core;

namespace TOHE.Roles.Crewmate;

internal class TimeManager : RoleBase
{
    private static readonly int Id = 9800;
    private static List<byte> playerIdList = [];
    public static bool On = false;
    public override bool IsEnable => On;
    public static bool HasEnabled => CustomRoles.TimeManager.IsClassEnable();
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;

    public static OptionItem IncreaseMeetingTime;
    public static OptionItem MeetingTimeLimit;
    public static OptionItem MadMinMeetingTimeLimit;
    
    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.TimeManager);
        IncreaseMeetingTime = IntegerOptionItem.Create(Id + 10, "TimeManagerIncreaseMeetingTime", new(5, 30, 1), 15, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.TimeManager])
            .SetValueFormat(OptionFormat.Seconds);
        MeetingTimeLimit = IntegerOptionItem.Create(Id + 11, "TimeManagerLimitMeetingTime", new(100, 900, 10), 300, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.TimeManager])
            .SetValueFormat(OptionFormat.Seconds);
        MadMinMeetingTimeLimit = IntegerOptionItem.Create(Id + 12, "MadTimeManagerLimitMeetingTime", new(5, 150, 5), 30, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.TimeManager])
            .SetValueFormat(OptionFormat.Seconds);
        Options.OverrideTasksData.Create(Id + 13, TabGroup.CrewmateRoles, CustomRoles.TimeManager);
    }
    public override void Init()
    {
        playerIdList = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        On = true;
    }
    private static int AdditionalTime(byte id)
    {
        var pc = Utils.GetPlayerById(id);
        return playerIdList.Contains(id) && pc.IsAlive() ? IncreaseMeetingTime.GetInt() * pc.GetPlayerTaskState().CompletedTasksCount : 0;
    }
    public static int TotalIncreasedMeetingTime()
    {
        int sec = 0;
        foreach (var playerId in playerIdList)
        {
            if (Utils.GetPlayerById(playerId).Is(CustomRoles.Madmate)) sec -= AdditionalTime(playerId);
            else sec += AdditionalTime(playerId);
        }
        Logger.Info($"{sec}second", "TimeManager.TotalIncreasedMeetingTime");
        return sec;
    }
}