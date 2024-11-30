using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Impostor;

public class Narc : IAddon
{
    private const int Id = 23400;
    public AddonTypes Type => AddonTypes.Misc;

    public static OptionItem MeetingsNeededForWin;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Narc, canSetNum: false, tab: TabGroup.Addons);
        MeetingsNeededForWin = IntegerOptionItem.Create(Id + 3, "MeetingsNeededForWin", new(0, 5, 1), 1, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Narc])
            .SetValueFormat(OptionFormat.Times);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
    public static void ApplyGameOptions(IGameOptions opt)
    {
        if (!Utils.IsActive(SystemTypes.Electrical))
            opt.SetVision(true);
        opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultCrewmateVision);
        opt.SetFloat(FloatOptionNames.ImpostorLightMod, Main.DefaultCrewmateVision);

        if (Utils.IsActive(SystemTypes.Electrical))
            opt.SetVision(true);
        opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultCrewmateVision / 5);
        opt.SetFloat(FloatOptionNames.ImpostorLightMod, Main.DefaultCrewmateVision / 5);
    }
}