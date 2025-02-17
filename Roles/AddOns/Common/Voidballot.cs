using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class VoidBallot : IAddon
{
    public CustomRoles Role => CustomRoles.VoidBallot;
    private const int Id = 21100;
    public AddonTypes Type => AddonTypes.Harmful;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.VoidBallot, canSetNum: true, teamSpawnOptions: true);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
}
