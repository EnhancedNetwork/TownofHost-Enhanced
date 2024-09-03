using AmongUs.GameOptions;
using static TOHE.Options;
using UnityEngine;

namespace TOHE.Roles.AddOns.Common;

public static class Sloth
{
    private const int Id = 29700;

    private static OptionItem OptionSpeed;

    public static void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Sloth, canSetNum: true, tab: TabGroup.Addons);
        OptionSpeed = FloatOptionItem.Create(Id + 10, "SlothSpeed", new(25f, 75f, 5f), 50f, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Sloth])
            .SetValueFormat(OptionFormat.Multiplier);
    }
    public static void SetSpeed(byte playerId, bool clearAddOn)
    {
        if (!clearAddOn)
        {
            float reductionFactor = Mathf.Clamp(OptionSpeed.GetFloat(), 0f, 75f) / 75f;
            Main.AllPlayerSpeed[playerId] *= Mathf.Clamp(1f - reductionFactor, 0.25f, 1f);
        }
        else
            Main.AllPlayerSpeed[playerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod);
    }
}