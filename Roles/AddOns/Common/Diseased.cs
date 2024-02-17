using static TOHE.Options;
using System.Collections.Generic;

namespace TOHE.Roles.AddOns.Common;

public static class Diseased
{
    private static readonly int Id = 21800;

    public static OptionItem ImpCanBeDiseased;
    public static OptionItem CrewCanBeDiseased;
    public static OptionItem NeutralCanBeDiseased;
    public static OptionItem DiseasedCDOpt;
    public static OptionItem DiseasedCDReset;

    public static Dictionary<byte, int> KilledDiseased;

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Diseased, canSetNum: true);
        ImpCanBeDiseased = BooleanOptionItem.Create(Id + 10, "ImpCanBeDiseased", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Diseased]);
        CrewCanBeDiseased = BooleanOptionItem.Create(Id + 11, "CrewCanBeDiseased", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Diseased]);
        NeutralCanBeDiseased = BooleanOptionItem.Create(Id + 12, "NeutralCanBeDiseased", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Diseased]);
        DiseasedCDOpt = FloatOptionItem.Create(Id + 13, "DiseasedCDOpt", new(0f, 180f, 1f), 25f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Diseased])
            .SetValueFormat(OptionFormat.Seconds);
        DiseasedCDReset = BooleanOptionItem.Create(Id + 14, "DiseasedCDReset", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Diseased]);
    }

    public static void Init()
    {
        KilledDiseased = [];
    }

    public static void IncreaseKCD(PlayerControl player)
    {
        if (KilledDiseased.ContainsKey(player.PlayerId))
        {
            Main.AllPlayerKillCooldown[player.PlayerId] = Main.AllPlayerKillCooldown[player.PlayerId] + KilledDiseased[player.PlayerId] * DiseasedCDOpt.GetFloat();
            Logger.Info($"kill cd of player set to {Main.AllPlayerKillCooldown[player.PlayerId]}", "Diseased");
        }
    }

    public static void AfterMeetingTasks()
    {
        if (DiseasedCDReset.GetBool())
        {
            foreach (var pid in KilledDiseased.Keys)
            {
                KilledDiseased[pid] = 0;
                var kdpc = Utils.GetPlayerById(pid);
                if (kdpc == null) continue;
                kdpc.ResetKillCooldown();
            }
            KilledDiseased = [];
        }
    }

   public static void CheckMurder(PlayerControl killer)
   {
        if (KilledDiseased.ContainsKey(killer.PlayerId))
        {
            // Key already exists, update the value
            KilledDiseased[killer.PlayerId] += 1;
        }
        else
        {
            // Key doesn't exist, add the key-value pair
            KilledDiseased.Add(killer.PlayerId, 1);
        }
   }
}



