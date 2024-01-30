
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

public static class Minion
{
    private static readonly int Id = 19900;

    public static OptionItem AbilityCooldown;

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Minion);
        AbilityCooldown = FloatOptionItem.Create(Id + 10, "AbilityCooldown", new(2.5f, 180f, 2.5f), 40f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Minion])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public static bool OnCheckProtect(PlayerControl killer, PlayerControl target)
    {
        var ImpPVC = CustomRolesHelper.IsImpostor(target.GetCustomRole());
        if (ImpPVC)
        {
            Main.AllPlayerKillCooldown[target.PlayerId] = 1;
            target.Notify(GetString("MinionNotify"));
            killer.RpcResetAbilityCooldown();
        }
        return false;
    }
}

