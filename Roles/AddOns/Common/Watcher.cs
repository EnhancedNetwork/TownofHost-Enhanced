using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Watcher : IAddon
{
    public CustomRoles Role => CustomRoles.Watcher;
    private const int Id = 20400;
    public AddonTypes Type => AddonTypes.Helpful;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Watcher, canSetNum: true, teamSpawnOptions: true);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
    public static void RevealVotes(IGameOptions opt) => opt.SetBool(BoolOptionNames.AnonymousVotes, false);
}

