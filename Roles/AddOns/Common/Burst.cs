using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.AddOns.Common;

public class Burst : IAddon
{
    private const int Id = 19000;
    public static bool IsEnable = false;
    public AddonTypes Type => AddonTypes.Helpful;

    private static OptionItem BurstKillDelay;

    private static readonly List<byte> BurstBodies = [];
    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Burst, canSetNum: true, teamSpawnOptions: true);
        BurstKillDelay = FloatOptionItem.Create(Id + 13, "BurstKillDelay", new(1f, 180f, 1f), 5f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Burst])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public static void Init()
    {
        BurstBodies.Clear();
        IsEnable = false;
    }
    public static void Add()
    {
        IsEnable = true;
    }

    public static void AfterMeetingTasks()
    {
        BurstBodies.Clear();
    }

    public static void AfterBurstDeadTasks(PlayerControl killer, PlayerControl target)
    {
        target.SetRealKiller(killer);
        BurstBodies.Add(target.PlayerId);
        if (killer.PlayerId != target.PlayerId && !killer.IsTransformedNeutralApocalypse())
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Burst), GetString("BurstNotify")));
            _ = new LateTask(() =>
            {
                if (!killer.inVent && killer.IsAlive() && !GameStates.IsMeeting && GameStates.IsInGame)
                {
                    killer.SetDeathReason(PlayerState.DeathReason.Bombed);
                    target.RpcMurderPlayer(killer);
                    killer.SetRealKiller(target);
                }
                else if (GameStates.IsInGame)
                {
                    RPC.PlaySoundRPC(killer.PlayerId, Sounds.TaskComplete);
                    killer.SetKillCooldown(time: Main.AllPlayerKillCooldown[killer.PlayerId] - BurstKillDelay.GetFloat(), forceAnime: true);
                    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Burst), GetString("BurstFailed")));
                }
                BurstBodies.Remove(target.PlayerId);
            }, BurstKillDelay.GetFloat(), "Burst Suicide");
        }
    }
}