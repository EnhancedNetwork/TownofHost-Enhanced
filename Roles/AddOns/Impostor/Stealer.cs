using static TOHE.Options;

namespace TOHE.Roles.AddOns.Impostor;

public static class Stealer
{
    private const int Id = 23200;
    
    public static OptionItem TicketsPerKill;
    public static void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.TicketsStealer, canSetNum: true, tab: TabGroup.Addons);
        TicketsPerKill = FloatOptionItem.Create("TicketsPerKill", new(0.1f, 10f, 0.1f), 0.5f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.TicketsStealer]);
    }
}