using System.Collections.Generic;

namespace TOHE.Roles.Neutral;

internal class God : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 25100;
    public static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    //==================================================================\\

    public static OptionItem NotifyGodAlive;
    public static OptionItem CanGuess;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.God);
        NotifyGodAlive = BooleanOptionItem.Create(Id + 3, "NotifyGodAlive", true, TabGroup.OtherRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.God]);
        CanGuess = BooleanOptionItem.Create(Id + 4, "CanGuess", false, TabGroup.OtherRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.God]);
    }

    public override void Init()
    {
        playerIdList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }

    public override bool GuessCheck(bool isUI, PlayerControl guesser, PlayerControl target, CustomRoles role, ref bool guesserSuicide)
    {
        if (guesser.Is(CustomRoles.God) && !CanGuess.GetBool())
        {
            if (!isUI) Utils.SendMessage(Translator.GetString("GuessDisabled"), guesser.PlayerId);
            else guesser.ShowPopUp(Translator.GetString("GuessDisabled"));
            return true;
        }

        return false;
    }

    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target) => true;
    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target) => Main.roleColors[target.GetCustomRole()];
}
