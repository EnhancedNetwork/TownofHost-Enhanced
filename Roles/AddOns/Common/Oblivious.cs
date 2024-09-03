using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Oblivious : IAddon
{
    private const int Id = 20700;
    public AddonTypes Type => AddonTypes.Harmful;

    public static OptionItem ObliviousBaitImmune;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Oblivious, canSetNum: true, teamSpawnOptions: true);
        ObliviousBaitImmune = BooleanOptionItem.Create(Id + 13, "ObliviousBaitImmune", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Oblivious]);
    }

}