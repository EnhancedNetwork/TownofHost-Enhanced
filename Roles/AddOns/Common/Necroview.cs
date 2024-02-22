using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Necroview
{
    private static readonly int Id = 19600;

    public static OptionItem ImpCanBeNecroview;
    public static OptionItem CrewCanBeNecroview;
    public static OptionItem NeutralCanBeNecroview;

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Necroview, canSetNum: true, tab: TabGroup.Addons);
        ImpCanBeNecroview = BooleanOptionItem.Create(Id + 10, "ImpCanBeNecroview", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Necroview]);
        CrewCanBeNecroview = BooleanOptionItem.Create(Id + 11, "CrewCanBeNecroview", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Necroview]);
        NeutralCanBeNecroview = BooleanOptionItem.Create(Id + 12, "NeutralCanBeNecroview", true, TabGroup.Addons, false) .SetParent(CustomRoleSpawnChances[CustomRoles.Necroview]);
    }

    public static string NameColorOptions(PlayerControl target)
    {
        var customRole = target.GetCustomRole();

        foreach (var SubRole in target.GetCustomSubRoles())
        {
            if (SubRole.Is(CustomRoles.Charmed)
                || SubRole.Is(CustomRoles.Infected)
                || SubRole.Is(CustomRoles.Contagious)
                || SubRole.Is(CustomRoles.Egoist)
                || SubRole.Is(CustomRoles.Recruit)
                || SubRole.Is(CustomRoles.Soulless))
                return Main.roleColors[CustomRoles.SwordsMan];
        }

        if (customRole.IsImpostorTeamV2() || customRole.IsMadmate())
        {
            return Main.roleColors[CustomRoles.Impostor];
        }

        if (customRole.IsCrewmate())
        {
            return Main.roleColors[CustomRoles.Bait];
        }

        return Main.roleColors[CustomRoles.SwordsMan]; 
    }
}

