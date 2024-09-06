using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Sloth : IAddon
{
    private const int Id = 29700;
    public AddonTypes Type => AddonTypes.Harmful;

    private static OptionItem OptionSpeed;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Sloth, canSetNum: true, tab: TabGroup.Addons, teamSpawnOptions: true);
        OptionSpeed = FloatOptionItem.Create(Id + 10, "SlothSpeed", new(25f, 75f, 5f), 50f, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Sloth])
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