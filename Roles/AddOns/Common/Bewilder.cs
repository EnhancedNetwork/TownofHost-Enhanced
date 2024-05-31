using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Bewilder
{
    private const int Id = 18900;

    private static OptionItem BewilderVision;
    public static OptionItem ImpCanBeBewilder;
    public static OptionItem CrewCanBeBewilder;
    public static OptionItem NeutralCanBeBewilder;
    private static OptionItem KillerGetBewilderVision;

    public static bool IsEnable;

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Bewilder, canSetNum: true);
        BewilderVision = FloatOptionItem.Create(Id + 10, "BewilderVision", new(0f, 5f, 0.05f), 0.6f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bewilder])
            .SetValueFormat(OptionFormat.Multiplier);
        ImpCanBeBewilder = BooleanOptionItem.Create("ImpCanBeBewilder", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bewilder]);
        CrewCanBeBewilder = BooleanOptionItem.Create("CrewCanBeBewilder", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bewilder]);
        NeutralCanBeBewilder = BooleanOptionItem.Create("NeutralCanBeBewilder", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bewilder]);
        KillerGetBewilderVision = BooleanOptionItem.Create("KillerGetBewilderVision", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bewilder]);
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
