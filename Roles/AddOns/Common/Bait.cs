using System;
using TOHE.Modules;
using static TOHE.Translator;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Bait : IAddon
{
    private const int Id = 18700;
    public AddonTypes Type => AddonTypes.Helpful;

    public static OptionItem BaitDelayMin;
    public static OptionItem BaitDelayMax;
    public static OptionItem BaitDelayNotify;
    public static OptionItem BaitNotification;
    public static OptionItem BaitCanBeReportedUnderAllConditions;
    
    public static List<byte> BaitAlive = [];

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Bait, canSetNum: true, teamSpawnOptions: true);
        BaitDelayMin = FloatOptionItem.Create(Id + 13, "BaitDelayMin", new(0f, 5f, 1f), 0f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bait])
            .SetValueFormat(OptionFormat.Seconds);
        BaitDelayMax = FloatOptionItem.Create(Id + 14, "BaitDelayMax", new(0f, 10f, 1f), 0f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bait])
            .SetValueFormat(OptionFormat.Seconds);
        BaitDelayNotify = BooleanOptionItem.Create(Id + 15, "BaitDelayNotify", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bait]);
        BaitNotification = BooleanOptionItem.Create(Id + 16, "BaitNotification", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bait]);
        BaitCanBeReportedUnderAllConditions = BooleanOptionItem.Create(Id + 17, "BaitCanBeReportedUnderAllConditions", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bait]);
    }

    public static void Init()
    {
        BaitAlive = [];
    }
    public static void BaitAfterDeathTasks(PlayerControl killer, PlayerControl target)
    {        
        if (killer.PlayerId == target.PlayerId)
        {
            if (target.GetRealKiller() != null)
            {
                if (!target.GetRealKiller().IsAlive()) return;
                killer = target.GetRealKiller();
            }
        }

        if (killer.PlayerId == target.PlayerId) return;

        if (killer.Is(CustomRoles.KillingMachine)
            || killer.Is(CustomRoles.Swooper)
            || killer.Is(CustomRoles.Wraith)
            || (killer.Is(CustomRoles.Oblivious) && Oblivious.ObliviousBaitImmune.GetBool()))
            return;

        {
            killer.RPCPlayCustomSound("Congrats");
            target.RPCPlayCustomSound("Congrats");
            float delay;
            if (BaitDelayMax.GetFloat() < BaitDelayMin.GetFloat()) delay = 0f;
            else delay = IRandom.Instance.Next((int)BaitDelayMin.GetFloat(), (int)BaitDelayMax.GetFloat() + 1);
            delay = Math.Max(delay, 0.15f);
            if (delay > 0.15f && BaitDelayNotify.GetBool()) killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Bait), string.Format(GetString("KillBaitNotify"), (int)delay)), delay);
            Logger.Info($"{killer.GetNameWithRole()} 击杀诱饵 => {target.GetNameWithRole()}", "MurderPlayer");
            _ = new LateTask(() => { if (GameStates.IsInTask && GameStates.IsInGame) killer?.CmdReportDeadBody(target.Data); }, delay, "Bait Self Report");
        }
    }
}

