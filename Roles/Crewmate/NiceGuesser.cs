using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

internal class NiceGuesser : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 10900;



    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    private static OptionItem GGCanGuessTime;
    private static OptionItem GGCanGuessCrew;
    private static OptionItem GGCanGuessAdt;
    private static OptionItem GGTryHideMsg;

    public override void SetupCustomOption()
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

    }
    public override void Add(byte playerId)
    {

    }
    public override void Remove(byte playerId)
    {

    }

    public override string PVANameText(PlayerVoteArea pva, PlayerControl seer, PlayerControl target)
            => seer.IsAlive() && target.IsAlive() ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.NiceGuesser), target.PlayerId.ToString()) + " " + pva.NameText.text : string.Empty;

    public static bool NeedHideMsg(PlayerControl pc) => pc.Is(CustomRoles.NiceGuesser) && GGTryHideMsg.GetBool();

    public static bool HideTabInGuesserUI(int TabId)
    {
        if (!GGCanGuessCrew.GetBool() && TabId == 0) return true;
        if (!GGCanGuessAdt.GetBool() && TabId == 3) return true;

        return false;
    }

    public override bool GuessCheck(bool isUI, PlayerControl guesser, PlayerControl target, CustomRoles role, ref bool guesserSuicide)
    {
        // Check limit
        if (GuessManager.GuesserGuessed[guesser.PlayerId] >= GGCanGuessTime.GetInt())
        {
            guesser.ShowInfoMessage(isUI, Translator.GetString("GGGuessMax"));
            return true;
        }

        // Nice Guesser Can't Guess Addons
        if (role.IsAdditionRole() && !GGCanGuessAdt.GetBool())
        {
            guesser.ShowInfoMessage(isUI, Translator.GetString("GuessAdtRole"));
            return true;
        }

        // Nice Guesser Can't Guess Impostors
        if (target.Is(Custom_Team.Crewmate) && !GGCanGuessCrew.GetBool() && !guesser.Is(CustomRoles.Madmate))
        {
            guesser.ShowInfoMessage(isUI, Translator.GetString("GuessCrewRole"));
            return true;
        }

        return false;
    }
}
