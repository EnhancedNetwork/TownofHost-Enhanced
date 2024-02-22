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
    public static OptionItem FarseerCanBeMadmate;
    public static OptionItem SnitchCanBeMadmate;
    public static OptionItem MadSnitchTasks;
    public static OptionItem JudgeCanBeMadmate;

    public static OptionItem ImpKnowWhosMadmate;
    public static OptionItem ImpCanKillMadmate;
    public static OptionItem MadmateKnowWhosMadmate;
    public static OptionItem MadmateKnowWhosImp;
    public static OptionItem MadmateCanKillImp;
    private static OptionItem MadmateHasImpostorVision;

    public static void SetupMenuOptions()
    {
        ImpKnowWhosMadmate = BooleanOptionItem.Create(Id1, "ImpKnowWhosMadmate", true, TabGroup.ImpostorRoles, false)
            .SetHeader(true)
            .SetGameMode(CustomGameMode.Standard);
        ImpCanKillMadmate = BooleanOptionItem.Create(Id1 + 1, "ImpCanKillMadmate", true, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);

        MadmateKnowWhosMadmate = BooleanOptionItem.Create(Id1 + 2, "MadmateKnowWhosMadmate", true, TabGroup.ImpostorRoles, false)
            .SetHeader(true)
            .SetGameMode(CustomGameMode.Standard);
        MadmateKnowWhosImp = BooleanOptionItem.Create(Id1 + 3, "MadmateKnowWhosImp", true, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);
        MadmateCanKillImp = BooleanOptionItem.Create(Id1 + 4, "MadmateCanKillImp", true, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);
        MadmateHasImpostorVision = BooleanOptionItem.Create(Id1 + 7, "MadmateHasImpostorVision", false, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);
    }

    public static void SetupCustomMenuOptions()
    {
        SetupAdtRoleOptions(Id2, CustomRoles.Madmate, canSetNum: true, canSetChance: false);
        MadmateSpawnMode = StringOptionItem.Create(Id2 + 3, "MadmateSpawnMode", madmateSpawnMode, 0, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        MadmateCountMode = StringOptionItem.Create(Id2 + 4, "MadmateCountMode", madmateCountMode, 1, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        SheriffCanBeMadmate = BooleanOptionItem.Create(Id2 + 5, "SheriffCanBeMadmate", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        MayorCanBeMadmate = BooleanOptionItem.Create(Id2 + 6, "MayorCanBeMadmate", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        NGuesserCanBeMadmate = BooleanOptionItem.Create(Id2 + 7, "NGuesserCanBeMadmate", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        MarshallCanBeMadmate = BooleanOptionItem.Create(Id2 + 8, "MarshallCanBeMadmate", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        FarseerCanBeMadmate = BooleanOptionItem.Create(Id2 + 9, "FarseerCanBeMadmate", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        SnitchCanBeMadmate = BooleanOptionItem.Create(Id2 + 11, "SnitchCanBeMadmate", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        MadSnitchTasks = IntegerOptionItem.Create(Id2 + 12, "MadSnitchTasks", new(1, 30, 1), 3, TabGroup.Addons, false).SetParent(SnitchCanBeMadmate)
            .SetValueFormat(OptionFormat.Pieces);
        JudgeCanBeMadmate = BooleanOptionItem.Create(Id2 + 13, "JudgeCanBeMadmate", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
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
            (pc.Is(CustomRoles.Farseer) && (!forGangster ? !FarseerCanBeMadmate.GetBool() : !Gangster.FarseerCanBeMadmate.GetBool())) ||
            pc.Is(CustomRoles.Needy) ||
            pc.Is(CustomRoles.Lazy) ||
            pc.Is(CustomRoles.Loyal) ||
            pc.Is(CustomRoles.SuperStar) ||
            pc.Is(CustomRoles.CyberStar) ||
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