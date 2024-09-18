namespace TOHE.Roles.AddOns.Common;

public class Statue : IAddon
{
    private const int Id = 13800;
    public AddonTypes Type => AddonTypes.Harmful;
    public static bool IsEnable = false;

    private static OptionItem SlowDown;
    private static OptionItem PeopleAmount;

    private static bool Active;
    private static HashSet<byte> CountNearplr;
    private static Dictionary<byte, float> tempSpeed;

    public void SetupCustomOption()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Statue, canSetNum: true, tab: TabGroup.Addons, teamSpawnOptions: true);
        SlowDown = FloatOptionItem.Create(Id + 10, "StatueSlow", new(0f, 1.25f, 0.25f), 0f, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Statue])
             .SetValueFormat(OptionFormat.Multiplier);
        PeopleAmount = IntegerOptionItem.Create(Id + 11, "StatuePeopleToSlow", new(1, 5, 1), 3, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Statue])
             .SetValueFormat(OptionFormat.Times);
    }

    public static void Init()
    {
        CountNearplr = [];
        tempSpeed = [];
        Active = true;
        IsEnable = false;
    }

    public static void Add(byte playerId)
    {
        tempSpeed.Add(playerId, Main.AllPlayerSpeed[playerId]);
        IsEnable = true;
    }

    public static void Remove(byte playerId)
    {
        if (Main.AllPlayerSpeed[playerId] == SlowDown.GetFloat())
        {
            Main.AllPlayerSpeed[playerId] = Main.AllPlayerSpeed[playerId] - SlowDown.GetFloat() + tempSpeed[playerId];
            Utils.GetPlayerById(playerId)?.MarkDirtySettings();
        }
        tempSpeed.Remove(playerId);
    }

    public static void AfterMeetingTasks()
    {
        foreach (var Statue in tempSpeed.Keys)
        {
            var pc = Utils.GetPlayerById(Statue);
            if (pc == null) continue;
            float tmpFloat = tempSpeed[Statue];
            Main.AllPlayerSpeed[Statue] = Main.AllPlayerSpeed[Statue] - Main.AllPlayerSpeed[Statue] + tmpFloat;
            pc.MarkDirtySettings();
        }
        Active = false;
        CountNearplr = [];
        _ = new LateTask(() => 
        {
            Active = true;
        }, 6f);
    }

    public void OnFixedUpdate(PlayerControl victim) 
    {
        if (!victim.Is(CustomRoles.Statue) || !victim.IsAlive()) return;

        foreach (var PVC in Main.AllAlivePlayerControls)
        {
            if (CountNearplr.Contains(PVC.PlayerId) && Utils.GetDistance(PVC.transform.position, victim.transform.position) > 2f)
            {
                CountNearplr.Remove(PVC.PlayerId);
            }
        }

        if (Active)
        {
            foreach (var plr in Main.AllAlivePlayerControls)
            {
                if (Utils.GetDistance(plr.transform.position, victim.transform.position) < 2f && plr != victim)
                {
                    if (!CountNearplr.Contains(plr.PlayerId)) CountNearplr.Add(plr.PlayerId);
                }
            }

            if (CountNearplr.Count >= PeopleAmount.GetInt())
            {
                if (Main.AllPlayerSpeed[victim.PlayerId] != SlowDown.GetFloat()) 
                { 
                    Main.AllPlayerSpeed[victim.PlayerId] = SlowDown.GetFloat();
                    victim.MarkDirtySettings();
                }
                return;
            }
            else if (Main.AllPlayerSpeed[victim.PlayerId] == SlowDown.GetFloat())
            {
                float tmpFloat = tempSpeed[victim.PlayerId];
                Main.AllPlayerSpeed[victim.PlayerId] = Main.AllPlayerSpeed[victim.PlayerId] - SlowDown.GetFloat() + tmpFloat;
                victim.MarkDirtySettings();
            }
        }
    }
}
