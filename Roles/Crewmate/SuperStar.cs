using System.Linq;
using UnityEngine;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class SuperStar : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 7150;
    private static bool On = false;
    public override bool IsEnable => On;
    public static bool HasEnabled => CustomRoles.SuperStar.IsClassEnable();
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    //==================================================================\\

    private static OptionItem EveryOneKnowSuperStar; // You should always have this enabled TBHHH 💀💀

    public static void SetupCustomOptions()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.SuperStar);
        EveryOneKnowSuperStar = BooleanOptionItem.Create(7152, "EveryOneKnowSuperStar", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.SuperStar]);
    }
    public override void Init()
    {
        On = false;
    }
    public override void Add(byte playerId)
    {
        On = true;
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
            => seer.Is(CustomRoles.SuperStar) && EveryOneKnowSuperStar.GetBool() ? ColorString(GetRoleColor(CustomRoles.SuperStar), "★") : "";
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
            => GetSuffix(seen, seer);
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        return !Main.AllAlivePlayerControls.Any(x =>
                x.PlayerId != killer.PlayerId &&
                x.PlayerId != target.PlayerId &&
                Vector2.Distance(x.transform.position, target.transform.position) < 2f);
    }
    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl pc, CustomRoles role)
    {
        if (target.Is(CustomRoles.SuperStar))
        {
            if (!isUI) SendMessage(GetString("GuessSuperStar"), pc.PlayerId);
            else pc.ShowPopUp(GetString("GuessSuperStar"));
            return true;
        }
        return false;
    }
    public static bool VisibleToEveryone(PlayerControl target) => target.Is(CustomRoles.SuperStar) && EveryOneKnowSuperStar.GetBool();
    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) => VisibleToEveryone(target);
}
