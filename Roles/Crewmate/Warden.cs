using Hazel;
using System.Collections.Generic;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

public class Warden
{
    private static readonly int Id = 27900;
    public static OptionItem AbilityCooldown;

    public static void SetupCustomOptions()
    {
        SetupGhostRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Warden);
        AbilityCooldown = FloatOptionItem.Create(Id + 10, "AbilityCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Warden])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public static bool OnCheckProtect(PlayerControl killer, PlayerControl target)
    {
        killer.Notify($"You've marked target");
        target.Notify($"DANGER! RUN RUN RUN!");

        killer.RpcResetAbilityCooldown();

        return false;
    }
}