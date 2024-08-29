using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Diseased : IAddon
{
    private const int Id = 21800;
    public static bool IsEnable = false;
    public AddonTypes Type => AddonTypes.Mixed;

    private static OptionItem DiseasedCDOpt;
    private static OptionItem DiseasedCDReset;

    private static Dictionary<byte, int> KilledDiseased;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Diseased, canSetNum: true, teamSpawnOptions: true);
        DiseasedCDOpt = FloatOptionItem.Create(Id + 13, "DiseasedCDOpt", new(0f, 180f, 1f), 25f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Diseased])
            .SetValueFormat(OptionFormat.Seconds);
        DiseasedCDReset = BooleanOptionItem.Create(Id + 14, "DiseasedCDReset", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Diseased]);
    }

    public void Init()
    {
        KilledDiseased = [];
        IsEnable = false;
    }
    public static void Add()
    {
        IsEnable = true;
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



