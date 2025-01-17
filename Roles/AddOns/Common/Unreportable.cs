using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Unreportable : IAddon
{
    public CustomRoles Role => CustomRoles.Unreportable;
    private const int Id = 20500;
    public AddonTypes Type => AddonTypes.Harmful;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Unreportable, canSetNum: true, teamSpawnOptions: true);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
}
