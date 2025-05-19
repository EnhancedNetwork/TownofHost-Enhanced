using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

internal class TimeManager : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.TimeManager;
    private const int Id = 9800;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();

    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    public static OptionItem IncreaseMeetingTime;
    public static OptionItem MeetingTimeLimit;
    public static OptionItem MadMinMeetingTimeLimit;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.TimeManager);
        IncreaseMeetingTime = IntegerOptionItem.Create(Id + 10, "TimeManagerIncreaseMeetingTime", new(5, 30, 1), 15, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.TimeManager])
            .SetValueFormat(OptionFormat.Seconds);
        MeetingTimeLimit = IntegerOptionItem.Create(Id + 11, "TimeManagerLimitMeetingTime", new(100, 900, 10), 300, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.TimeManager])
            .SetValueFormat(OptionFormat.Seconds);
        MadMinMeetingTimeLimit = IntegerOptionItem.Create(Id + 12, "MadTimeManagerLimitMeetingTime", new(5, 150, 5), 30, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.TimeManager])
            .SetValueFormat(OptionFormat.Seconds);
        OverrideTasksData.Create(Id + 13, TabGroup.CrewmateRoles, CustomRoles.TimeManager);
    }
    public override void Init()
    {
        playerIdList.Clear();
    }
    public override void Add(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
    }

    private static int AdditionalTime(byte id)
    {
        var timemanager = id.GetPlayer();
        bool isalive = playerIdList.Contains(id) && timemanager.IsAlive();
        int increased = IncreaseMeetingTime.GetInt() * timemanager.GetPlayerTaskState().CompletedTasksCount;
        int decreased = 0 - increased;
        return isalive ? (!timemanager.IsPlayerCrewmateTeam() ? decreased : increased) : 0;
    }
    public static int TotalIncreasedMeetingTime()
    {
        int sec = 0;
        foreach (var playerId in playerIdList)
            sec += AdditionalTime(playerId);

        Logger.Info($"{sec}second", "TimeManager.TotalIncreasedMeetingTime");
        return sec;
    }
}
