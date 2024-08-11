
namespace TOHE.Roles.AddOns.Impostor;

public class LastImpostor : IAddon
{
    private const int Id = 22800;
    public AddonTypes Type => AddonTypes.Impostor;
    public static byte currentId = byte.MaxValue;

    private static OptionItem CooldownReduction;

    public void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.Addons, CustomRoles.LastImpostor, 1);
        CooldownReduction = FloatOptionItem.Create(Id + 15, "OverclockedReduction", new(5f, 95f, 5f), 50f, TabGroup.Addons, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.LastImpostor])
            .SetValueFormat(OptionFormat.Percent);
    }
    public static void Init() => currentId = byte.MaxValue;
    public static void Add(byte id) => currentId = id;
    public static void SetKillCooldown()
    {
        if (currentId == byte.MaxValue) return;
        if (!Main.AllPlayerKillCooldown.TryGetValue(currentId, out var currentCD)) return;
        var removeCooldown = currentCD * (CooldownReduction.GetFloat() / 100);
        Main.AllPlayerKillCooldown[currentId] -= removeCooldown;
    }
    private static bool CanBeLastImpostor(PlayerControl pc)
        => pc.IsAlive() && !pc.Is(CustomRoles.LastImpostor)&& !pc.Is(CustomRoles.Overclocked) && pc.Is(Custom_Team.Impostor);
    
    public static void SetSubRole()
    {
        //ラストインポスターがすでにいれば処理不要
        if (currentId != byte.MaxValue || !AmongUsClient.Instance.AmHost) return;
        if (Options.CurrentGameMode == CustomGameMode.FFA || !CustomRoles.LastImpostor.IsEnable() || Main.AliveImpostorCount != 1) return;

        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (CanBeLastImpostor(pc))
            {
                pc.RpcSetCustomRole(CustomRoles.LastImpostor);
                Add(pc.PlayerId);
                SetKillCooldown();
                pc.SyncSettings();
                Utils.NotifyRoles(SpecifySeer: pc, ForceLoop: false);
                break;
            }
        }
    }
}