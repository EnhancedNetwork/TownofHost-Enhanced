using System.Linq;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Marshall : RoleBase
{
    private const int Id = 11900;
    public static bool On = false;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;

    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Marshall);

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Marshall);
        OverrideTasksData.Create(Id + 10, TabGroup.CrewmateRoles, CustomRoles.Marshall);
    }
    public override void Init()
    {
        On = false;
    }

    public override void Add(byte playerId)
    {
        On = true;
    }
    private static bool GetExpose(PlayerControl pc)
    {
        if (!pc.IsAlive() || pc.Is(CustomRoles.Madmate)) return false;

        return (pc.Is(CustomRoles.Marshall) && pc.GetPlayerTaskState().IsTaskFinished);
    }
    private static bool IsMarshallTarget(PlayerControl seer) => CustomRoles.Marshall.IsClassEnable() && seer.Is(CustomRoleTypes.Crewmate);
    public override string GetMark(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false)
        => IsMarshallTarget(seer) && GetExpose(target) ? Utils.ColorString(RoleColor, "★") : string.Empty;

    public override bool KnowRoletarget(PlayerControl seer, PlayerControl target) => target.GetPlayerTaskState().IsTaskFinished && seer.Is(CustomRoleTypes.Crewmate) && target.Is(CustomRoles.Marshall);
    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) => (seer.Is(CustomRoleTypes.Crewmate) && target.Is(CustomRoles.Marshall) && target.GetPlayerTaskState().IsTaskFinished);
    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl pc)
    {
        if (!isUI) Utils.SendMessage(GetString("GuessMarshallTask"), pc.PlayerId);
        else pc.ShowPopUp(GetString("GuessMarshallTask"));
        return true;
    }
}
