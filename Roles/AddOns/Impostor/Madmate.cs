using AmongUs.GameOptions;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Impostor;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Impostor;

public static class Madmate
{
    private static readonly int Id1 = 60003;
    private static readonly int Id2 = 22900;

    public static OptionItem MadmateSpawnMode;
    public static OptionItem MadmateCountMode;
    public static OptionItem SheriffCanBeMadmate;
    public static OptionItem MayorCanBeMadmate;
    public static OptionItem NGuesserCanBeMadmate;
    public static OptionItem MarshallCanBeMadmate;
    public static OptionItem OverseerCanBeMadmate;
    public static OptionItem SnitchCanBeMadmate;
    public static OptionItem MadSnitchTasks;
    public static OptionItem RetributionistCanBeMadmate;
    public static OptionItem JudgeCanBeMadmate;

    public static OptionItem ImpKnowWhosMadmate;
    public static OptionItem ImpCanKillMadmate;
    public static OptionItem MadmateKnowWhosMadmate;
    public static OptionItem MadmateKnowWhosImp;
    public static OptionItem MadmateCanKillImp;
    private static OptionItem MadmateHasImpostorVision;

    public static void SetupMenuOptions()
    {
        ImpKnowWhosMadmate = BooleanOptionItem.Create("ImpKnowWhosMadmate", true, TabGroup.ImpostorRoles, false)
            .SetHeader(true)
            .SetGameMode(CustomGameMode.Standard);
        ImpCanKillMadmate = BooleanOptionItem.Create("ImpCanKillMadmate", true, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);

        MadmateKnowWhosMadmate = BooleanOptionItem.Create("MadmateKnowWhosMadmate", true, TabGroup.ImpostorRoles, false)
            .SetHeader(true)
            .SetGameMode(CustomGameMode.Standard);
        MadmateKnowWhosImp = BooleanOptionItem.Create("MadmateKnowWhosImp", true, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);
        MadmateCanKillImp = BooleanOptionItem.Create("MadmateCanKillImp", true, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);
        MadmateHasImpostorVision = BooleanOptionItem.Create("MadmateHasImpostorVision", false, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);
    }

    public static void SetupCustomMenuOptions()
    {
        SetupAdtRoleOptions(Id2, CustomRoles.Madmate, canSetNum: true, canSetChance: false);
        MadmateSpawnMode = StringOptionItem.Create(Id2 + 3, "MadmateSpawnMode", madmateSpawnMode, 0, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        MadmateCountMode = StringOptionItem.Create(Id2 + 4, "MadmateCountMode", madmateCountMode, 1, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        SheriffCanBeMadmate = BooleanOptionItem.Create("SheriffCanBeMadmate", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        MayorCanBeMadmate = BooleanOptionItem.Create("MayorCanBeMadmate", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        NGuesserCanBeMadmate = BooleanOptionItem.Create("NGuesserCanBeMadmate", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        MarshallCanBeMadmate = BooleanOptionItem.Create("MarshallCanBeMadmate", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        RetributionistCanBeMadmate = BooleanOptionItem.Create("RetributionistCanBeMadmate", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        OverseerCanBeMadmate = BooleanOptionItem.Create("OverseerCanBeMadmate", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        SnitchCanBeMadmate = BooleanOptionItem.Create("SnitchCanBeMadmate", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        MadSnitchTasks = IntegerOptionItem.Create(Id2 + 12, "MadSnitchTasks", new(1, 30, 1), 3, TabGroup.Addons, false).SetParent(SnitchCanBeMadmate)
            .SetValueFormat(OptionFormat.Pieces);
        JudgeCanBeMadmate = BooleanOptionItem.Create("JudgeCanBeMadmate", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
    }

    public static void ApplyGameOptions(IGameOptions opt) => opt.SetVision(MadmateHasImpostorVision.GetBool());

    private static readonly string[] madmateSpawnMode =
    [
        "MadmateSpawnMode.Assign",
        "MadmateSpawnMode.FirstKill",
        "MadmateSpawnMode.SelfVote",
    ];
    private static readonly string[] madmateCountMode =
    [
        "MadmateCountMode.None",
        "MadmateCountMode.Imp",
        "MadmateCountMode.Original",
    ];

    public static bool CanBeMadmate(this PlayerControl pc, bool inGame = false, bool forGangster = false)
    {
        return pc != null && (pc.GetCustomRole().IsCrewmate() || (pc.GetCustomRole().IsNeutral() && inGame)) && !pc.Is(CustomRoles.Madmate)
        && !(
            (pc.Is(CustomRoles.Sheriff) && (!forGangster ? !SheriffCanBeMadmate.GetBool() : !Gangster.SheriffCanBeMadmate.GetBool())) ||
            (pc.Is(CustomRoles.Mayor) && (!forGangster ? !MayorCanBeMadmate.GetBool() : !Gangster.MayorCanBeMadmate.GetBool())) ||
            (pc.Is(CustomRoles.NiceGuesser) && (!forGangster ? !NGuesserCanBeMadmate.GetBool() : !Gangster.NGuesserCanBeMadmate.GetBool())) ||
            (pc.Is(CustomRoles.Snitch) && !SnitchCanBeMadmate.GetBool()) ||
            (pc.Is(CustomRoles.Judge) && (!forGangster ? !JudgeCanBeMadmate.GetBool() : !Gangster.JudgeCanBeMadmate.GetBool())) ||
            (pc.Is(CustomRoles.Marshall) && (!forGangster ? !MarshallCanBeMadmate.GetBool() : !Gangster.MarshallCanBeMadmate.GetBool())) ||
            (pc.Is(CustomRoles.Retributionist) && (!forGangster ? !RetributionistCanBeMadmate.GetBool() : !Gangster.RetributionistCanBeMadmate.GetBool())) ||
            (pc.Is(CustomRoles.Overseer) && (!forGangster ? !OverseerCanBeMadmate.GetBool() : !Gangster.OverseerCanBeMadmate.GetBool())) ||
            pc.Is(CustomRoles.LazyGuy) ||
            pc.Is(CustomRoles.Lazy) ||
            pc.Is(CustomRoles.Loyal) ||
            pc.Is(CustomRoles.SuperStar) ||
            pc.Is(CustomRoles.Celebrity) ||
            pc.Is(CustomRoles.TaskManager) ||
            //   pc.Is(CustomRoles.Cyber) ||
            pc.Is(CustomRoles.Egoist) ||
            pc.Is(CustomRoles.Schizophrenic) ||
            pc.Is(CustomRoles.Vigilante) ||
            (pc.Is(CustomRoles.NiceMini) && Mini.Age >= 18) ||
            (pc.Is(CustomRoles.Hurried) && !Hurried.CanBeOnMadMate.GetBool())
            );
    }
}