using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Stubborn : IAddon
{
    public CustomRoles Role => CustomRoles.Stubborn;
    private const int Id = 22500;
    public AddonTypes Type => AddonTypes.Mixed;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Stubborn, canSetNum: true, teamSpawnOptions: true);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
}

