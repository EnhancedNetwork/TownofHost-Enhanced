//using System.Linq;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Impostor;

public class Mare : IAddon
{
    public CustomRoles Role => CustomRoles.Mare;
    private const int Id = 23000;
    public AddonTypes Type => AddonTypes.Impostor;
    public static readonly HashSet<byte> playerIdList = [];

    public static OptionItem KillCooldownInLightsOut;
    private static OptionItem SpeedInLightsOut;
    private static bool idAccelerated = false;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Mare, canSetNum: true, tab: TabGroup.Addons);
        SpeedInLightsOut = FloatOptionItem.Create(Id + 10, "MareAddSpeedInLightsOut", new(0.1f, 0.5f, 0.1f), 0.3f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mare])
            .SetValueFormat(OptionFormat.Multiplier);
        KillCooldownInLightsOut = FloatOptionItem.Create(Id + 11, "MareKillCooldownInLightsOut", new(0f, 180f, 2.5f), 7.5f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mare])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public void Init()
    {
        playerIdList.Clear();
    }
    public void Add(byte playerId, bool gameIsLoading = true)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);
    }
    public void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
    }
    public static bool IsEnable => playerIdList.Any();

    public static float GetKillCooldown => Utils.IsActive(SystemTypes.Electrical) ? KillCooldownInLightsOut.GetFloat() : DefaultKillCooldown;

    public static void ApplyGameOptions(byte playerId)
    {
        if (Utils.IsActive(SystemTypes.Electrical) && !idAccelerated)
        {
            idAccelerated = true;
            Main.AllPlayerSpeed[playerId] += SpeedInLightsOut.GetFloat();
        }
        else if (!Utils.IsActive(SystemTypes.Electrical) && idAccelerated)
        {
            idAccelerated = false;
            Main.AllPlayerSpeed[playerId] -= SpeedInLightsOut.GetFloat();
        }
    }
    public static bool IsLightsOut => !Utils.IsActive(SystemTypes.Electrical);

    public static bool KnowTargetRoleColor(PlayerControl target, bool isMeeting)
        => !isMeeting && playerIdList.Contains(target.PlayerId) && Utils.IsActive(SystemTypes.Electrical);
}
