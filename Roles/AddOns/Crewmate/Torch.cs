﻿using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Crewmate;

public class Torch : IAddon
{
    private const int Id = 20300;
    public AddonTypes Type => AddonTypes.Helpful;
    private static OptionItem TorchVision;
    private static OptionItem TorchAffectedByLights;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id , CustomRoles.Torch, canSetNum: true);
        TorchVision = FloatOptionItem.Create(Id +10, "TorchVision", new(0.5f, 5f, 0.25f), 1.25f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Torch])
            .SetValueFormat(OptionFormat.Multiplier);
        TorchAffectedByLights = BooleanOptionItem.Create(Id +11, "TorchAffectedByLights", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Torch]);
    }

    public static void ApplyGameOptions(IGameOptions opt)
    {
        if (!Utils.IsActive(SystemTypes.Electrical))
            opt.SetVision(true);
        opt.SetFloat(FloatOptionNames.CrewLightMod, TorchVision.GetFloat());
        opt.SetFloat(FloatOptionNames.ImpostorLightMod, TorchVision.GetFloat());

        if (Utils.IsActive(SystemTypes.Electrical) && !TorchAffectedByLights.GetBool())
            opt.SetVision(true);
        opt.SetFloat(FloatOptionNames.CrewLightMod, TorchVision.GetFloat() * 5);
        opt.SetFloat(FloatOptionNames.ImpostorLightMod, TorchVision.GetFloat() * 5);
    }
}