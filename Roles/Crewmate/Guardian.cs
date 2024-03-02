using static TOHE.Options;
using static TOHE.Translator;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.Crewmate;

internal class Guardian : RoleBase
{
    public const int Id = 11700;
    public static bool On = false;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;

    public static OverrideTasksData GuardianTasks;

    public static void SetupCustomOptions()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Guardian);
        GuardianTasks = OverrideTasksData.Create(Id + 10, TabGroup.CrewmateRoles, CustomRoles.Guardian);
    }

    public override void Init()
    {
        On = false;
    }

    public override void Add(byte playerId)
    {
        On = true;
    }
    public static bool CannotBeKilled(PlayerControl Guardian) => Guardian.Is(CustomRoles.Guardian) && Guardian.AllTasksCompleted();
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (CannotBeKilled(target))
            return false;

        return true;
    }

    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl guesser, CustomRoles role)
    {
        if (target.Is(CustomRoles.Guardian) && target.GetPlayerTaskState().IsTaskFinished)
        {
            if (!isUI) Utils.SendMessage(GetString("GuessGuardianTask"), guesser.PlayerId);
            else guesser.ShowPopUp(GetString("GuessGuardianTask"));
            return true;
        }
        return false;
    }
}
