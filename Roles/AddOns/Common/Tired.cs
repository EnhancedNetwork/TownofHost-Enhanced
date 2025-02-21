using AmongUs.GameOptions;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.AddOns.Common;

public class Tired : IAddon
{
    public CustomRoles Role => CustomRoles.Tired;
    private const int Id = 27300;
    public AddonTypes Type => AddonTypes.Harmful;
    private static readonly Dictionary<byte, bool> playerIdList = []; // Target Action player for Vision

    private static OptionItem SetVision;
    private static OptionItem SetSpeed;
    private static OptionItem TiredDuration;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Tired, canSetNum: true, tab: TabGroup.Addons, teamSpawnOptions: true);
        SetVision = FloatOptionItem.Create(Id + 10, "TiredVision", new(0f, 2f, 0.25f), 0.25f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Tired])
             .SetValueFormat(OptionFormat.Multiplier);
        SetSpeed = FloatOptionItem.Create(Id + 11, "TiredSpeed", new(0.25f, 3f, 0.25f), 0.75f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Tired])
           .SetValueFormat(OptionFormat.Multiplier);
        TiredDuration = FloatOptionItem.Create(Id + 12, "TiredDur", new(2f, 15f, 0.5f), 5f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Tired])
                .SetValueFormat(OptionFormat.Seconds);
    }

    public void Init()
    {
        playerIdList.Clear();
    }
    public void Add(byte playerId, bool gameIsLoading = true)
    {
        playerIdList[playerId] = false;
    }
    public void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        playerId.GetPlayer()?.MarkDirtySettings();
    }
    public static void RemoveMidGame(byte playerId)
    {
        playerIdList.Remove(playerId);
    }

    public static void ApplyGameOptions(IGameOptions opt, PlayerControl player)
    {
        if (!playerIdList.TryGetValue(player.PlayerId, out var isTired)) return;

        if (isTired)
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, SetVision.GetFloat());
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, SetVision.GetFloat());
        }
        else
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultCrewmateVision);
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, Main.DefaultImpostorVision);
        }
    }

    public static void AfterActionTasks(PlayerControl player)
    {
        // Speed
        player.Notify(GetString("TiredNotify"));
        var tmpSpeed = Main.AllPlayerSpeed[player.PlayerId];
        Main.AllPlayerSpeed[player.PlayerId] = SetSpeed.GetFloat();

        // Vision
        playerIdList[player.PlayerId] = true;
        player.MarkDirtySettings();

        _ = new LateTask(() =>
        {
            Main.AllPlayerSpeed[player.PlayerId] = Main.AllPlayerSpeed[player.PlayerId] - SetSpeed.GetFloat() + tmpSpeed;
            player.MarkDirtySettings();
            playerIdList[player.PlayerId] = false;
        }, TiredDuration.GetFloat());
    }
}
