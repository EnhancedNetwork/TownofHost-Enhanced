
namespace TOHE.Roles.AddOns.Common;

public class Seer : IAddon
{
    public CustomRoles Role => CustomRoles.Seer;
    private const int Id = 20000;
    public AddonTypes Type => AddonTypes.Helpful;

    public void SetupCustomOption()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Seer, canSetNum: true, teamSpawnOptions: true);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
}
