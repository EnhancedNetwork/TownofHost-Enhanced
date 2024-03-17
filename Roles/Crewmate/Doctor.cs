using TOHE.Roles.Core;
using AmongUs.GameOptions;
using static TOHE.Translator;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

internal class Doctor : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 6700;
    private static bool On = false;
    public override bool IsEnable => On;
    public static bool HasEnabled => CustomRoles.Doctor.IsClassEnable();
    public override CustomRoles ThisRoleBase => CustomRoles.Scientist;

    //==================================================================\\
    private static OptionItem TaskCompletedBatteryChargeOpt;
    private static OptionItem VisibleToEveryoneOpt;

    public static void SetupCustomOptions()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Doctor);
        TaskCompletedBatteryChargeOpt = FloatOptionItem.Create(Id + 10, "DoctorTaskCompletedBatteryCharge", new(0f, 250f, 1f), 50f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Doctor])
            .SetValueFormat(OptionFormat.Seconds);
        VisibleToEveryoneOpt = BooleanOptionItem.Create(Id + 11, "DoctorVisibleToEveryone", false, TabGroup.CrewmateRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Doctor]);
    }
    public override void Init()
    {
        On = false;
    }
    public override void Add(byte playerId)
    {
        On = true;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ScientistCooldown = 0f;
        AURoleOptions.ScientistBatteryCharge = TaskCompletedBatteryChargeOpt.GetFloat();
    }
    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl pc, CustomRoles role, ref bool guesserSuicide)
    {
        if (target.Is(CustomRoles.Doctor) && VisibleToEveryoneOpt.GetBool() && !target.IsEvilAddons())
        {
            if (!isUI) Utils.SendMessage(GetString("GuessDoctor"), pc.PlayerId);
            else pc.ShowPopUp(GetString("GuessDoctor"));
            return true;
        }
        return false;
    }
    public static bool VisibleToEveryone(PlayerControl target) => target.Is(CustomRoles.Doctor) && VisibleToEveryoneOpt.GetBool();
    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) => VisibleToEveryone(target);
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target) => VisibleToEveryone(target);
}
