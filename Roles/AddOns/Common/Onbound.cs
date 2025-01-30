using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Onbound : IAddon
{
    public CustomRoles Role => CustomRoles.Onbound;
    private const int Id = 25800;
    public AddonTypes Type => AddonTypes.Guesser;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Onbound, canSetNum: true, tab: TabGroup.Addons, teamSpawnOptions: true);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
}

