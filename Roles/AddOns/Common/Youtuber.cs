using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Youtuber
{
    private static readonly int Id = 25500;

    public static OptionItem ImpCanBeAutopsy;
    public static OptionItem CrewCanBeAutopsy;
    public static OptionItem NeutralCanBeAutopsy;

    public static void SetupCustomOptions()
    {

        SetupAdtRoleOptions(Id, CustomRoles.Youtuber, canSetNum: true, tab: TabGroup.OtherRoles);
    }
}