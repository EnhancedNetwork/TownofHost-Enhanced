using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Antidote : IAddon
{
    private const int Id = 21400;
    public static bool IsEnable = false;
    public AddonTypes Type => AddonTypes.Mixed;

    public static OptionItem ImpCanBeAntidote;
    public static OptionItem CrewCanBeAntidote;
    public static OptionItem NeutralCanBeAntidote;
    private static OptionItem AntidoteCDOpt;
    private static OptionItem AntidoteCDReset;

    private static Dictionary<byte, int> KilledAntidote = [];

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Antidote, canSetNum: true, teamSpawnOptions: true);
        AntidoteCDOpt = FloatOptionItem.Create(Id + 13, "AntidoteCDOpt", new(0f, 180f, 1f), 5f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Antidote])
            .SetValueFormat(OptionFormat.Seconds);
        AntidoteCDReset = BooleanOptionItem.Create(Id + 14, "AntidoteCDReset", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Antidote]);
    }

    public void Init()
    {
        KilledAntidote = [];
        IsEnable = false;
    }
    public static void Add()
    {
        IsEnable = true;
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

    public static void CheckMurder(PlayerControl killer)
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

