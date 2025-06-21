namespace TOHE.Roles.Impostor;

internal class TimeThief : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.TimeThief;
    private const int Id = 3700;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();

    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem DecreaseMeetingTime;
    public static OptionItem LowerLimitVotingTime;
    private static OptionItem ReturnStolenTimeUponDeath;
    public static OptionItem MaxMeetingTimeOnAdmired;


    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.TimeThief);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.TimeThief])
            .SetValueFormat(OptionFormat.Seconds);
        DecreaseMeetingTime = IntegerOptionItem.Create(Id + 11, "TimeThiefDecreaseMeetingTime", new(0, 100, 1), 25, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.TimeThief])
            .SetValueFormat(OptionFormat.Seconds);
        LowerLimitVotingTime = IntegerOptionItem.Create(Id + 12, "TimeThiefLowerLimitVotingTime", new(1, 300, 1), 10, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.TimeThief])
            .SetValueFormat(OptionFormat.Seconds);
        ReturnStolenTimeUponDeath = BooleanOptionItem.Create(Id + 13, "TimeThiefReturnStolenTimeUponDeath", true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.TimeThief]);
        MaxMeetingTimeOnAdmired = IntegerOptionItem.Create(Id + 14, "TimeThiefMaxTimeOnAdmired", new(100, 900, 10), 300, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.TimeThief])
            .SetValueFormat(OptionFormat.Seconds);
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

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    private static int StolenTime(byte id)
    {
        var timethief = Utils.GetPlayerById(id);
        bool isalive = playerIdList.Contains(id) && (timethief.IsAlive() || !ReturnStolenTimeUponDeath.GetBool());

        int decreased = DecreaseMeetingTime.GetInt() * Main.PlayerStates[id].GetKillCount(true);
        int increased = 0 - decreased;
        return isalive ? (timethief.IsPlayerCrewmateTeam() ? increased : decreased) : 0;
    }

    public static int TotalDecreasedMeetingTime()
    {
        int sec = 0;
        foreach (var playerId in playerIdList)
            sec -= StolenTime(playerId);

        Logger.Info($"{sec}second", "TimeThief.TotalDecreasedMeetingTime");
        return sec;
    }

    public override string GetProgressText(byte playerId, bool cooms)
        => StolenTime(playerId) > 0 ? Utils.ColorString(Palette.ImpostorRed.ShadeColor(0.5f), $"{-StolenTime(playerId)}s") : string.Empty;
}
