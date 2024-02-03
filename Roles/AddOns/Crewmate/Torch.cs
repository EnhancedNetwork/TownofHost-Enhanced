using static TOHE.Options;

namespace TOHE.Roles.AddOns.Crewmate;

public class Torch
{
    private static readonly int Id = 20300;
    public static OptionItem TorchVision;
    public static OptionItem TorchAffectedByLights;

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id , CustomRoles.Torch, canSetNum: true);
        TorchVision = FloatOptionItem.Create(Id +10, "TorchVision", new(0.5f, 5f, 0.25f), 1.25f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Torch])
            .SetValueFormat(OptionFormat.Multiplier);
        TorchAffectedByLights = BooleanOptionItem.Create(Id +11, "TorchAffectedByLights", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Torch]);
    }
}