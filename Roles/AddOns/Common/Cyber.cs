using Hazel;
using System.Collections.Generic;
using System.Linq;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.AddOns.Crewmate;

public class Cyber
{
    private static readonly int Id = 19100;

    public static List<byte> CyberDead = [];

    public static OptionItem ImpCanBeCyber;
    public static OptionItem CrewCanBeCyber;
    public static OptionItem NeutralCanBeCyber;
    public static OptionItem ImpKnowCyberDead;
    public static OptionItem CrewKnowCyberDead;
    public static OptionItem NeutralKnowCyberDead;
    public static OptionItem CyberKnown;
    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Cyber, canSetNum: true);
        ImpCanBeCyber = BooleanOptionItem.Create(Id + 10, "ImpCanBeCyber", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cyber]);
        CrewCanBeCyber = BooleanOptionItem.Create(Id + 11, "CrewCanBeCyber", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cyber]);
        NeutralCanBeCyber = BooleanOptionItem.Create(Id + 12, "NeutralCanBeCyber", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cyber]);
        ImpKnowCyberDead = BooleanOptionItem.Create(Id + 13, "ImpKnowCyberDead", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cyber]);
        CrewKnowCyberDead = BooleanOptionItem.Create(Id + 14, "CrewKnowCyberDead", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cyber]);
        NeutralKnowCyberDead = BooleanOptionItem.Create(Id + 15, "NeutralKnowCyberDead", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cyber]);
        CyberKnown = BooleanOptionItem.Create(Id + 16, "CyberKnown", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cyber]);
    }

    public static void Init()
    {
        CyberDead = [];
    }
}