using TOHE.Roles.Crewmate;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Crewmate;

public static class Bloodlust
{
    private static readonly int Id = 18600;

    public static OptionItem ImpCanBeAutopsy;
    public static OptionItem CrewCanBeAutopsy;
    public static OptionItem NeutralCanBeAutopsy;

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(21700, CustomRoles.Bloodlust, canSetNum: true);
    }

    // Bloodlust uses == Alchemist.OnFixedUpdate(); 
}

