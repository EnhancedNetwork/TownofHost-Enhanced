using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Crewmate;

public class Torch : IAddon
{
    public CustomRoles Role => CustomRoles.Torch;
    private const int Id = 20300;
    public AddonTypes Type => AddonTypes.Helpful;
    private static OptionItem TorchVision;
    private static OptionItem TorchAffectedByLights;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Torch, canSetNum: true);
        TorchVision = FloatOptionItem.Create(Id + 10, "TorchVision", new(0.5f, 5f, 0.25f), 1.25f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Torch])
            .SetValueFormat(OptionFormat.Multiplier);
        TorchAffectedByLights = BooleanOptionItem.Create(Id + 11, "TorchAffectedByLights", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Torch]);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
    public static void ApplyGameOptions(IGameOptions opt)
    {
        float forcrew = Utils.IsActive(SystemTypes.Electrical) && !TorchAffectedByLights.GetBool() ? TorchVision.GetFloat() * 5 : TorchVision.GetFloat();
        float forimp = Utils.IsActive(SystemTypes.Electrical) && TorchAffectedByLights.GetBool() ? TorchVision.GetFloat() / 5 : TorchVision.GetFloat();

        opt.SetVision(true);
        opt.SetFloat(FloatOptionNames.CrewLightMod, forcrew);
        opt.SetFloat(FloatOptionNames.ImpostorLightMod, forimp);
    }
}
