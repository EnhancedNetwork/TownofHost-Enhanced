using AmongUs.GameOptions;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Doctor : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Doctor;
    private const int Id = 6700;
    public override CustomRoles ThisRoleBase => CustomRoles.Scientist;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateBasic;
    //==================================================================\\

    private static OptionItem TaskCompletedBatteryChargeOpt;
    public static OptionItem VisibleToEveryoneOpt;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Doctor);
        TaskCompletedBatteryChargeOpt = FloatOptionItem.Create(Id + 10, "DoctorTaskCompletedBatteryCharge", new(0f, 250f, 1f), 50f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Doctor])
            .SetValueFormat(OptionFormat.Seconds);
        VisibleToEveryoneOpt = BooleanOptionItem.Create(Id + 11, "DoctorVisibleToEveryone", false, TabGroup.CrewmateRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Doctor]);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ScientistCooldown = 0f;
        AURoleOptions.ScientistBatteryCharge = TaskCompletedBatteryChargeOpt.GetFloat();
    }
    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {

        return true;
    }

    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl pc, CustomRoles role, ref bool guesserSuicide)
    {
        if (role != CustomRoles.Doctor) return false;
        if (VisibleToEveryoneOpt.GetBool() && !target.GetCustomSubRoles().Any(sub => sub.IsBetrayalAddon()))
        {
            pc.ShowInfoMessage(isUI, GetString("GuessDoctor"));
            return true;
        }
        return false;
    }
    public static bool VisibleToEveryone(PlayerControl target) => target.Is(CustomRoles.Doctor) && VisibleToEveryoneOpt.GetBool();
    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) => VisibleToEveryone(target);
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target) => VisibleToEveryone(target);
}
