using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

internal class NiceGuesser : RoleBase
{
    private const int Id = 10900;
    private static bool On = false;
    public override bool IsEnable => On;
    public static bool HasEnabled => CustomRoles.NiceGuesser.IsClassEnable();
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public static void SetupCustomOptions()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.NiceGuesser);
        GGCanGuessTime = IntegerOptionItem.Create(Id + 10, "GuesserCanGuessTimes", new(1, 15, 1), 15, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser])
            .SetValueFormat(OptionFormat.Times);
        GGCanGuessCrew = BooleanOptionItem.Create(Id + 11, "GGCanGuessCrew", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser]);
        GGCanGuessAdt = BooleanOptionItem.Create(Id + 12, "GGCanGuessAdt", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser]);
        GGTryHideMsg = BooleanOptionItem.Create(Id + 13, "GuesserTryHideMsg", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser])
            .SetColor(Color.green);
    }
    public override void Init()
    {
        On = false;
    }
    public override void Add(byte playerId)
    {
        On = true;
    }

    public override string PVANameText(PlayerVoteArea pva, PlayerControl target)
            => !Utils.GetPlayerById(pva.TargetPlayerId).Data.IsDead && !target.Data.IsDead ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.NiceGuesser), target.PlayerId.ToString()) + " " + pva.NameText.text : "";
    
}
