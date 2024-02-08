using MS.Internal.Xml.XPath;
using System.Collections.Generic;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using static TOHE.Options;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.AddOns.Impostor;
public static class Madmate
{
    private static readonly int Id1 = 60002;
    private static readonly int Id2 = 22900;

    public static OptionItem MadmateSpawnMode;
    public static OptionItem MadmateCountMode;
    public static OptionItem SheriffCanBeMadmate;
    public static OptionItem MayorCanBeMadmate;
    public static OptionItem NGuesserCanBeMadmate;
    public static OptionItem MarshallCanBeMadmate;
    public static OptionItem FarseerCanBeMadmate;
    public static OptionItem RetributionistCanBeMadmate;
    public static OptionItem SnitchCanBeMadmate;
    public static OptionItem MadSnitchTasks;
    public static OptionItem JudgeCanBeMadmate;

    public static OptionItem ImpKnowAlliesRole;
    public static OptionItem ImpKnowWhosMadmate;
    public static OptionItem ImpCanKillMadmate;
    public static OptionItem MadmateKnowWhosMadmate;
    public static OptionItem MadmateKnowWhosImp;
    public static OptionItem MadmateCanKillImp;
    public static OptionItem MadmateHasImpostorVision;
    public static void SetupMenuOptions()
    {

        ImpKnowAlliesRole = BooleanOptionItem.Create(Id1 + 0, "ImpKnowAlliesRole", true, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true);
        ImpKnowWhosMadmate = BooleanOptionItem.Create(Id1 + 1, "ImpKnowWhosMadmate", true, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);
        ImpCanKillMadmate = BooleanOptionItem.Create(Id1 + 2, "ImpCanKillMadmate", true, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);

        MadmateKnowWhosMadmate = BooleanOptionItem.Create(Id1 + 3, "MadmateKnowWhosMadmate", true, TabGroup.ImpostorRoles, false)
            .SetHeader(true)
            .SetGameMode(CustomGameMode.Standard);
        MadmateKnowWhosImp = BooleanOptionItem.Create(Id1 + 4, "MadmateKnowWhosImp", true, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);
        MadmateCanKillImp = BooleanOptionItem.Create(Id1 + 5, "MadmateCanKillImp", true, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);
        MadmateHasImpostorVision = BooleanOptionItem.Create(Id1 + 6, "MadmateHasImpostorVision", false, TabGroup.ImpostorRoles, false)
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
        RetributionistCanBeMadmate = BooleanOptionItem.Create(Id2 + 10, "RetributionistCanBeMadmate", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        SnitchCanBeMadmate = BooleanOptionItem.Create(Id2 + 11, "SnitchCanBeMadmate", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        MadSnitchTasks = IntegerOptionItem.Create(Id2 + 12, "MadSnitchTasks", new(1, 30, 1), 3, TabGroup.Addons, false).SetParent(SnitchCanBeMadmate)
            .SetValueFormat(OptionFormat.Pieces);
        JudgeCanBeMadmate = BooleanOptionItem.Create(Id2 + 13, "JudgeCanBeMadmate", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
    }

    public static readonly string[] madmateSpawnMode =
    [
        "MadmateSpawnMode.Assign",
        "MadmateSpawnMode.FirstKill",
        "MadmateSpawnMode.SelfVote",
    ];
    public static readonly string[] madmateCountMode =
    [
        "MadmateCountMode.None",
        "MadmateCountMode.Imp",
        "MadmateCountMode.Original",
    ];

    public static bool CanBeMadmate(this PlayerControl pc, bool inGame = false)
    {
        return pc != null && (pc.GetCustomRole().IsCrewmate() || (pc.GetCustomRole().IsNeutral() && inGame)) && !pc.Is(CustomRoles.Madmate)
        && !(
            (pc.Is(CustomRoles.Sheriff) && !Madmate.SheriffCanBeMadmate.GetBool()) ||
            (pc.Is(CustomRoles.Mayor) && !Madmate.MayorCanBeMadmate.GetBool()) ||
            (pc.Is(CustomRoles.NiceGuesser) && !Madmate.NGuesserCanBeMadmate.GetBool()) ||
            (pc.Is(CustomRoles.Snitch) && !Madmate.SnitchCanBeMadmate.GetBool()) ||
            (pc.Is(CustomRoles.Judge) && !Madmate.JudgeCanBeMadmate.GetBool()) ||
            (pc.Is(CustomRoles.Marshall) && !Madmate.MarshallCanBeMadmate.GetBool()) ||
            (pc.Is(CustomRoles.Farseer) && !Madmate.FarseerCanBeMadmate.GetBool()) ||
            (pc.Is(CustomRoles.Retributionist) && !Madmate.RetributionistCanBeMadmate.GetBool()) ||
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