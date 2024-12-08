using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Impostor;

public class Narc : IAddon
{
    private const int Id = 31200;
    public AddonTypes Type => AddonTypes.Misc;

    public static OptionItem MeetingsNeededForWin;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Narc, canSetNum: false, tab: TabGroup.Addons);
        MeetingsNeededForWin = IntegerOptionItem.Create(Id + 3, "MeetingsNeededForWin", new(0, 10, 1), 5, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Narc])
            .SetValueFormat(OptionFormat.Times);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
// yep.It's the end.If you have better ideas on Narc's win condition,please inform me at https://discord.com/channels/1094344790910455908/1309925307163086948
}
