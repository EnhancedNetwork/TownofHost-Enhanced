using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Flash
{
    private static readonly int Id = 26100;

    private static OptionItem OptionSpeed;

    public static void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Flash, canSetNum: true, tab: TabGroup.Addons);
        OptionSpeed = FloatOptionItem.Create(Id + 10, "FlashSpeed", new(0.25f, 5f, 0.25f), 2.5f, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Flash])
            .SetValueFormat(OptionFormat.Multiplier);
    }
    public static void SetSpeed(byte playerId)
    {
        Main.AllPlayerSpeed[playerId] = OptionSpeed.GetFloat();
    }
}