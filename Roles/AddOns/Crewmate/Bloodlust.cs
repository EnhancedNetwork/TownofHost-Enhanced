using TOHE.Roles.Crewmate;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Crewmate;

public static class Bloodlust
{
    private static readonly int Id = 21700;

    public static OptionItem ImpCanBeAutopsy;
    public static OptionItem CrewCanBeAutopsy;
    public static OptionItem NeutralCanBeAutopsy;

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Bloodlust, canSetNum: true);
    }

    public static void OnTaskComplete(PlayerControl player)
    {
        if (Alchemist.BloodlustList.ContainsKey(player.PlayerId)) return;

        Alchemist.BloodlustList[player.PlayerId] = player.PlayerId;
        player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Bloodlust), Translator.GetString("BloodlustAdded")));
    }
}