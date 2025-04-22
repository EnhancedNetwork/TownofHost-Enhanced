using static TOHE.MeetingHudStartPatch;

namespace TOHE.Roles.Neutral;

internal class God : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.God;
    private const int Id = 25100;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralChaos;
    //==================================================================\\

    public static OptionItem NotifyGodAlive;
    public static OptionItem CanGuess;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.God);
        NotifyGodAlive = BooleanOptionItem.Create(Id + 3, "NotifyGodAlive", true, TabGroup.NeutralRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.God]);
        CanGuess = BooleanOptionItem.Create(Id + 4, GeneralOption.CanGuess, false, TabGroup.NeutralRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.God]);
    }

    public override bool GuessCheck(bool isUI, PlayerControl guesser, PlayerControl target, CustomRoles role, ref bool guesserSuicide)
    {
        if (!CanGuess.GetBool())
        {
            Logger.Info($"Guess Disabled for this player {guesser.PlayerId}", "GuessManager");
            guesser.ShowInfoMessage(isUI, Translator.GetString("GuessDisabled"));
            return true;
        }
        return false;
    }

    public override void OnMeetingHudStart(PlayerControl pc)
    {
        if (pc.IsAlive() && NotifyGodAlive.GetBool())
            AddMsg(Translator.GetString("GodNoticeAlive"), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.God), Translator.GetString("God").ToUpper()));
    }

    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target) => seer.Is(CustomRoles.God);
    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target) => Main.roleColors[target.GetCustomRole()];
}
