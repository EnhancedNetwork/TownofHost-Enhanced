namespace TOHE.Roles.Impostor;

internal class TimeThief : RoleBase
{
    //===========================SETUP================================\\
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
    }
    public override void Init()
    {
        playerIdList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    private static int StolenTime(byte id)
        => playerIdList.Contains(id) && (Utils.GetPlayerById(id).IsAlive() || !ReturnStolenTimeUponDeath.GetBool())
            ? DecreaseMeetingTime.GetInt() * Main.PlayerStates[id].GetKillCount(true)
            : 0;

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