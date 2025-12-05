using System;
using TOHE.Modules;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.AddOns.Common;

public class Bait : IAddon
{
    public CustomRoles Role => CustomRoles.Bait;
    private const int Id = 18700;
    public AddonTypes Type => AddonTypes.Helpful;

    public static OptionItem BaitDelayMin;
    public static OptionItem BaitDelayMax;
    public static OptionItem BaitDelayNotify;
    public static OptionItem BaitNotification;
    public static OptionItem BaitCanBeReportedUnderAllConditions;

    public static readonly HashSet<byte> BaitAlive = [];

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

    public void Init()
    {
        BaitAlive.Clear();
    }
    public void Add(byte playerId, bool gameIsLoading = true)
    {
        BaitAlive.Add(playerId);
    }
    public void Remove(byte playerId)
    {
        BaitAlive.Remove(playerId);
    }
    public static void SendNotify()
    {
        if (MeetingStates.FirstMeeting && CustomRoles.Bait.RoleExist() && BaitNotification.GetBool())
        {
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Bait) && !BaitAlive.Contains(x.PlayerId)).ToArray())
            {
                BaitAlive.Add(pc.PlayerId);
            }
            HashSet<string> baitAliveList = [];
            foreach (var whId in BaitAlive.ToArray())
            {
                PlayerControl whpc = whId.GetPlayer();
                if (whpc == null) continue;
                baitAliveList.Add(whpc.GetRealName());
            }
            string separator = TranslationController.Instance.currentLanguage.languageID is SupportedLangs.English or SupportedLangs.Russian ? "], [" : "】, 【";
            MeetingHudStartPatch.AddMsg(string.Format(GetString("BaitAdviceAlive"), string.Join(separator, baitAliveList)), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Bait), GetString("Bait").ToUpper()));
        }
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
            || killer.Is(CustomRoles.Cleaner)
            || killer.Is(CustomRoles.Swift)
            || (DisableReportWhenCC.GetBool() && Utils.IsActive(SystemTypes.Comms) && Camouflage.IsActive && !BaitCanBeReportedUnderAllConditions.GetBool())
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

