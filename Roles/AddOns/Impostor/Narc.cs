using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Impostor;

public class Narc : IAddon
{
    private const int Id = 31200;
    public AddonTypes Type => AddonTypes.Misc;

    public static OptionItem MeetingsNeededForWin;
    public static OptionItem NarcCanKillMadmates;
    public static OptionItem NarcCanSeeTeammates;
    public static OptionItem NarcCanUseSabotage;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Narc, canSetNum: false, tab: TabGroup.Addons);
        MeetingsNeededForWin = IntegerOptionItem.Create(Id + 3, "MeetingsNeededForWin", new(0, 10, 1), 5, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Narc])
            .SetValueFormat(OptionFormat.Times);
        NarcCanSeeTeammates = BooleanOptionItem.Create(Id + 4, "NarcCanSeeTeammates", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Narc]);
        NarcCanKillMadmates = BooleanOptionItem.Create(Id + 5, "NarcCanKillMadmates", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Narc]);
        NarcCanUseSabotage = BooleanOptionItem.Create(Id + 6, "NarcCanUseSabotage", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Narc]);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
    public static bool CantUseSabotage(PlayerControl pc) => pc.Is(CustomRoles.Narc) && !NarcCanUseSabotage.GetBool();
// Note:Narc Parasite and Narc Crewpostor are still shown as neutral to some roles
}
