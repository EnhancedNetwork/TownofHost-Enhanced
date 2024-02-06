using static TOHE.Options;
using System.Collections.Generic;
using System.Linq;
using MS.Internal.Xml.XPath;

namespace TOHE.Roles.AddOns.Common;

public static class Antidote
{
    private static readonly int Id = 18600;

    public static OptionItem ImpCanBeAntidote;
    public static OptionItem CrewCanBeAntidote;
    public static OptionItem NeutralCanBeAntidote;
    public static OptionItem AntidoteCDOpt;
    public static OptionItem AntidoteCDReset;


    public static Dictionary<byte, int> KilledAntidote = [];

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(21400, CustomRoles.Antidote, canSetNum: true);
        ImpCanBeAntidote = BooleanOptionItem.Create(21403, "ImpCanBeAntidote", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Antidote]);
        CrewCanBeAntidote = BooleanOptionItem.Create(21404, "CrewCanBeAntidote", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Antidote]);
        NeutralCanBeAntidote = BooleanOptionItem.Create(21405, "NeutralCanBeAntidote", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Antidote]);
        AntidoteCDOpt = FloatOptionItem.Create(21406, "AntidoteCDOpt", new(0f, 180f, 1f), 5f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Antidote])
            .SetValueFormat(OptionFormat.Seconds);
        AntidoteCDReset = BooleanOptionItem.Create(21407, "AntidoteCDReset", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Antidote]);
    }

    public static void Init()
    {
        KilledAntidote = [];
    }

    public static void ReduceKCD(PlayerControl player)
    {
        if (KilledAntidote.ContainsKey(player.PlayerId))
        {
            var kcd = Main.AllPlayerKillCooldown[player.PlayerId] - KilledAntidote[player.PlayerId] * AntidoteCDOpt.GetFloat();
            if (kcd < 0) kcd = 0;
            Main.AllPlayerKillCooldown[player.PlayerId] = kcd;
            Logger.Info($"kill cd of player set to {Main.AllPlayerKillCooldown[player.PlayerId]}", "Antidote");
        }
    }

    public static void AfterMeetingTasks()
    {
        if (AntidoteCDReset.GetBool())
        {
            foreach (var pid in KilledAntidote.Keys.ToArray())
            {
                KilledAntidote[pid] = 0;
                var kapc = Utils.GetPlayerById(pid);
                if (kapc == null) continue;
                kapc.ResetKillCooldown();
            }
            KilledAntidote = [];
        }
    }

    public static void Checkmurder(PlayerControl killer)
    {
        if (KilledAntidote.ContainsKey(killer.PlayerId))
        {
            // Key already exists, update the value
            KilledAntidote[killer.PlayerId] += 1;
        }
        else
        {
            // Key doesn't exist, add the key-value pair
            KilledAntidote.Add(killer.PlayerId, 1);
        }
    }
}

