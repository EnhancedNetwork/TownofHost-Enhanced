using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Flash : IAddon
{
    private const int Id = 26100;
    public AddonTypes Type => AddonTypes.Helpful;

    private static OptionItem OptionSpeed;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Flash, canSetNum: true, tab: TabGroup.Addons, teamSpawnOptions: true);
        OptionSpeed = FloatOptionItem.Create(Id + 10, "FlashSpeed", new(0.25f, 5f, 0.25f), 2.5f, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Flash])
            .SetValueFormat(OptionFormat.Multiplier);
    }
    public static void SetSpeed(byte playerId, bool clearAddOn)
    {
        if (!clearAddOn)
            Main.AllPlayerSpeed[playerId] = OptionSpeed.GetFloat();
        else
            Main.AllPlayerSpeed[playerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod);
    }
}