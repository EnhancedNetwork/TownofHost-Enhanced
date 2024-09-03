using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Watcher : IAddon
{
    private const int Id = 20400;
    public AddonTypes Type => AddonTypes.Helpful;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Watcher, canSetNum: true, teamSpawnOptions: true);
    }

    public static void RevealVotes(IGameOptions opt) => opt.SetBool(BoolOptionNames.AnonymousVotes, false);
}

