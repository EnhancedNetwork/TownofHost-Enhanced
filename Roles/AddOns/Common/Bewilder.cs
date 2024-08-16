using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Bewilder : IAddon
{
    private const int Id = 18900;
    public AddonTypes Type => AddonTypes.Helpful;

    private static OptionItem BewilderVision;
    private static OptionItem KillerGetBewilderVision;

    public static bool IsEnable;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Bewilder, canSetNum: true, teamSpawnOptions: true);
        BewilderVision = FloatOptionItem.Create(Id + 10, "BewilderVision", new(0f, 5f, 0.05f), 0.6f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bewilder])
            .SetValueFormat(OptionFormat.Multiplier);
        KillerGetBewilderVision = BooleanOptionItem.Create(Id + 14, "KillerGetBewilderVision", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bewilder]);
    }

    public static void Init()
    {
        IsEnable = false;
    }

    public static void Add()
    {
        IsEnable = true;
    }

    public static void ApplyVisionOptions(IGameOptions opt) 
    { 
        opt.SetVision(false); 
        opt.SetFloat(FloatOptionNames.ImpostorLightMod, BewilderVision.GetFloat()); 
        opt.SetFloat(FloatOptionNames.CrewLightMod, BewilderVision.GetFloat()); 
    }
    public static void ApplyGameOptions(IGameOptions opt, PlayerControl player)
    {
        // If the Bewilder was killed, his killer will receive his vision
        if (Main.AllPlayerControls.Any(x => x.Is(CustomRoles.Bewilder) && !x.IsAlive() && x.GetRealKiller()?.PlayerId == player.PlayerId && KillerGetBewilderVision.GetBool() && !x.Is(CustomRoles.Hangman)))
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, BewilderVision.GetFloat());
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, BewilderVision.GetFloat());
        }
    }
}
