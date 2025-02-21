using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Antidote : IAddon
{
    public CustomRoles Role => CustomRoles.Antidote;
    private const int Id = 21400;
    public static bool IsEnable = false;
    public AddonTypes Type => AddonTypes.Mixed;


    private static OptionItem AntidoteCDOpt;
    private static OptionItem AntidoteCDReset;

    private static readonly HashSet<byte> playerList = [];
    private static readonly Dictionary<byte, int> KilledAntidote = [];

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Antidote, canSetNum: true, teamSpawnOptions: true);
        AntidoteCDOpt = FloatOptionItem.Create(Id + 13, "AntidoteCDOpt", new(0f, 180f, 1f), 5f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Antidote])
            .SetValueFormat(OptionFormat.Seconds);
        AntidoteCDReset = BooleanOptionItem.Create(Id + 14, "AntidoteCDReset", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Antidote]);
    }

    public void Init()
    {
        IsEnable = false;
        playerList.Clear();
        KilledAntidote.Clear();
    }
    public void Add(byte playerId, bool gameIsLoading = true)
    {
        playerList.Add(playerId);
        IsEnable = true;
    }
    public void Remove(byte playerId)
    {
        playerList.Remove(playerId);

        if (!playerList.Any())
            IsEnable = false;
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
            KilledAntidote.Clear();
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

