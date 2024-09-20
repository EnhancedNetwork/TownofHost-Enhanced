using AmongUs.GameOptions;

namespace TOHE.Roles.AddOns.Common;

public class Statue : IAddon
{
    private const int Id = 13800;
    public AddonTypes Type => AddonTypes.Harmful;
    public static bool IsEnable = false;

    private static OptionItem SlowDown;
    private static OptionItem PeopleAmount;

    private static bool Active;
    private static readonly HashSet<byte> CountNearplr = [];
    private static readonly Dictionary<byte, float> TempSpeed = [];

    public void SetupCustomOption()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Statue, canSetNum: true, tab: TabGroup.Addons, teamSpawnOptions: true);
        SlowDown = FloatOptionItem.Create(Id + 10, "StatueSlow", new(0f, 1.25f, 0.25f), 0f, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Statue])
             .SetValueFormat(OptionFormat.Multiplier);
        PeopleAmount = IntegerOptionItem.Create(Id + 11, "StatuePeopleToSlow", new(1, 5, 1), 3, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Statue])
             .SetValueFormat(OptionFormat.Times);
    }

    public void Init()
    {
        IsEnable = false;
        CountNearplr.Clear();
        TempSpeed.Clear();
        Active = true;
    }

    public void Add(byte playerId, bool gameIsLoading = true)
    {
        var speed = Main.AllPlayerSpeed[playerId];
        TempSpeed[playerId] = speed;
        IsEnable = true;
    }
    public void Remove(byte playerId)
    {
        Main.AllPlayerSpeed[playerId] = Main.AllPlayerSpeed[playerId] - SlowDown.GetFloat() + TempSpeed[playerId];
        TempSpeed.Remove(playerId);
        playerId.GetPlayer()?.MarkDirtySettings();

        if (!TempSpeed.Any())
            IsEnable = false;
    }

    public static void AfterMeetingTasks()
    {
        foreach (var (statue, speed) in TempSpeed)
        {
            var pc = statue.GetPlayer();
            if (pc == null) continue;

            Main.AllPlayerSpeed[statue] = Main.AllPlayerSpeed[statue] - SlowDown.GetFloat() + speed;
            pc.MarkDirtySettings();
        }
        Active = false;
        CountNearplr.Clear();
        _ = new LateTask(() => 
        {
            Active = true;
        }, 6f);
    }

    public void OnFixedUpdate(PlayerControl victim) 
    {
        if (!victim.Is(CustomRoles.Statue)) return;
        if (!victim.IsAlive() && victim != null)
        {
            var currentSpeed = Main.AllPlayerSpeed[victim.PlayerId];
            var normalSpeed = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod);
            if (currentSpeed != normalSpeed)
            {
                Main.AllPlayerSpeed[victim.PlayerId] = normalSpeed;
                victim?.MarkDirtySettings();
            }
            return;
        }

        foreach (var PVC in Main.AllPlayerControls)
        {
            if (!PVC.IsAlive())
            {
                CountNearplr.Remove(PVC.PlayerId);
            }
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
                float tmpFloat = TempSpeed[victim.PlayerId];
                Main.AllPlayerSpeed[victim.PlayerId] = Main.AllPlayerSpeed[victim.PlayerId] - SlowDown.GetFloat() + tmpFloat;
                victim.MarkDirtySettings();
            }
        }
    }
}
