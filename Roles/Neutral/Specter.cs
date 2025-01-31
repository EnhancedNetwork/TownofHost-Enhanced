using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Specter : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Specter;
    private const int Id = 14900;
    public override CustomRoles ThisRoleBase => CanVent.GetBool() ? CustomRoles.Engineer : CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralChaos;
    //==================================================================\\
    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute) => !ForRecompute;

    private static OptionItem CanVent;
    public static OptionItem SnatchesWin;
    public static OptionItem CanGuess;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(14900, TabGroup.NeutralRoles, CustomRoles.Specter);
        CanVent = BooleanOptionItem.Create(14902, GeneralOption.CanVent, false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Specter]);
        SnatchesWin = BooleanOptionItem.Create(14903, GeneralOption.SnatchesWin, false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Specter]);
        CanGuess = BooleanOptionItem.Create(14904, GeneralOption.CanGuess, false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Specter]);
        OverrideTasksData.Create(14905, TabGroup.NeutralRoles, CustomRoles.Specter);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = 1f;
        AURoleOptions.EngineerInVentMaxTime = 0f;
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

    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl guesser, CustomRoles role, ref bool guesserSuicide)
    {
        if (role == CustomRoles.Specter)
        {
            guesser.ShowInfoMessage(isUI, Translator.GetString("GuessSpecter"));
            return true;
        }
        return false;
    }
}
