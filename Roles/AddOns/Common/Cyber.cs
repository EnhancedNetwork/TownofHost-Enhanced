using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Cyber
{
    private const int Id = 19100;

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
        ImpCanBeCyber = BooleanOptionItem.Create("ImpCanBeCyber", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cyber]);
        CrewCanBeCyber = BooleanOptionItem.Create("CrewCanBeCyber", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cyber]);
        NeutralCanBeCyber = BooleanOptionItem.Create("NeutralCanBeCyber", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cyber]);
        ImpKnowCyberDead = BooleanOptionItem.Create("ImpKnowCyberDead", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cyber]);
        CrewKnowCyberDead = BooleanOptionItem.Create("CrewKnowCyberDead", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cyber]);
        NeutralKnowCyberDead = BooleanOptionItem.Create("NeutralKnowCyberDead", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cyber]);
        CyberKnown = BooleanOptionItem.Create("CyberKnown", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cyber]);
    }

    public static void Init()
    {
        CyberDead = [];
    }
    public static void Clear()
    {
        CyberDead.Clear();
    }

    public static void AfterCyberDeadTask(PlayerControl target, bool inMeeting)
    {
        foreach (var pc in Main.AllPlayerControls)
        {
            if (!ImpKnowCyberDead.GetBool() && pc.GetCustomRole().IsImpostor()) continue;
            if (!NeutralKnowCyberDead.GetBool() && pc.GetCustomRole().IsNeutral()) continue;
            if (!CrewKnowCyberDead.GetBool() && pc.GetCustomRole().IsCrewmate()) continue;

            if (inMeeting)
            {
                Utils.SendMessage(string.Format(Translator.GetString("CyberDead"), target.GetRealName()), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cyber), Translator.GetString("CyberNewsTitle")));
            }
            else
            {
                pc.KillFlash();
                pc.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cyber), Translator.GetString("OnCyberDead")));
            }
        }

        if (!inMeeting && !CyberDead.Contains(target.PlayerId))
            CyberDead.Add(target.PlayerId);
    }
}