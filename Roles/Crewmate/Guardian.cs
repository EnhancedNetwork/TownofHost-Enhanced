using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Guardian : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 11700;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Guardian);
        OverrideTasksData.Create(Id + 10, TabGroup.CrewmateRoles, CustomRoles.Guardian);
    }

    public override void Init()
    {
        playerIdList.Clear();
    }

    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static bool CannotBeKilled(PlayerControl Guardian) => Guardian.Is(CustomRoles.Guardian) && Guardian.GetPlayerTaskState().IsTaskFinished;
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (CannotBeKilled(target))
            return false;

        return true;
    }

    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl guesser, CustomRoles role, ref bool guesserSuicide)
    {
        if (role == CustomRoles.Guardian && target.GetPlayerTaskState().IsTaskFinished)
        {
            guesser.ShowInfoMessage(isUI, GetString("GuessGuardianTask"));
            return true;
        }
        return false;
    }
}
