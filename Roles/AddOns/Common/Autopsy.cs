using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Autopsy : IAddon
{
    public CustomRoles Role => CustomRoles.Autopsy;
    private const int Id = 18600;
    public AddonTypes Type => AddonTypes.Helpful;
    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Autopsy, canSetNum: true, teamSpawnOptions: true);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
}
