﻿using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Cyber : IAddon
{
    private const int Id = 19100;
    public AddonTypes Type => AddonTypes.Helpful;

    public static List<byte> CyberDead = [];

    public static OptionItem ImpKnowCyberDead;
    public static OptionItem CrewKnowCyberDead;
    public static OptionItem NeutralKnowCyberDead;
    public static OptionItem CyberKnown;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Cyber, canSetNum: true, teamSpawnOptions: true);
        ImpKnowCyberDead = BooleanOptionItem.Create(Id + 13, "ImpKnowCyberDead", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cyber]);
        CrewKnowCyberDead = BooleanOptionItem.Create(Id + 14, "CrewKnowCyberDead", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cyber]);
        NeutralKnowCyberDead = BooleanOptionItem.Create(Id + 15, "NeutralKnowCyberDead", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cyber]);
        CyberKnown = BooleanOptionItem.Create(Id + 16, "CyberKnown", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cyber]);
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
        if (target.IsDisconnected()) return;

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