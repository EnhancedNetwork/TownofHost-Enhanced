using AmongUs.GameOptions;
using System.Collections.Generic;
using System.Linq;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

public static class Huntsman
{
    private static readonly int Id = 12870;
    public static List<byte> playerIdList = new();

    private static OptionItem KillCooldown;
    public static OptionItem CanVent;
    private static OptionItem HasImpostorVision;
    private static OptionItem SuccessKillCooldown;
    private static OptionItem FailureKillCooldown;
    private static OptionItem NumOfTargets;
    private static OptionItem MinKCD;
    private static OptionItem MaxKCD;

    public static List<byte> Targets = new();
    public static float KCD = 25;

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Huntsman, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Huntsman])
            .SetValueFormat(OptionFormat.Seconds);
        SuccessKillCooldown = FloatOptionItem.Create(Id + 11, "HHSuccessKCDDecrease", new(0f, 180f, 0.5f), 5f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Huntsman])
            .SetValueFormat(OptionFormat.Seconds);
        FailureKillCooldown = FloatOptionItem.Create(Id + 12, "HHFailureKCDIncrease", new(0f, 180f, 0.5f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Huntsman])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 13, "CanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Huntsman]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 14, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Huntsman]);
        NumOfTargets = IntegerOptionItem.Create(Id + 15, "HHNumOfTargets", new(0, 10, 1), 3, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Huntsman])
            .SetValueFormat(OptionFormat.Times);
        MaxKCD = FloatOptionItem.Create(Id + 16, "HHMaxKCD", new(0f, 180f, 2.5f), 60f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Huntsman])
            .SetValueFormat(OptionFormat.Seconds);
        MinKCD = FloatOptionItem.Create(Id + 17, "HHMinKCD", new(0f, 180f, 2.5f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Huntsman])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public static void Init()
    {
        playerIdList = new();
        Targets = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        _ = new LateTask(ResetTargets, 8f);
        KCD = KillCooldown.GetFloat();

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Any();
    public static void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision.GetBool());
    public static void OnReportDeadBody()
    {
        ResetTargets();
    }
    public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        float tempkcd = KCD;
        if (Targets.Contains(target.PlayerId)) System.Math.Clamp(KCD -= SuccessKillCooldown.GetFloat(), MinKCD.GetFloat(), MaxKCD.GetFloat());
        else System.Math.Clamp(KCD += FailureKillCooldown.GetFloat(), MinKCD.GetFloat(), MaxKCD.GetFloat());
        if (KCD != tempkcd)
        {
            killer.ResetKillCooldown();
            killer.SyncSettings();
        }
    }
    public static string GetHudText(PlayerControl player)
    {
        var targetId = player.PlayerId;
        string output = string.Empty;
        for (int i = 0; i < Targets.Count; i++) { byte playerId = Targets[i]; if (i != 0) output += ", "; output += Utils.GetPlayerById(playerId).GetRealName(); }
        return targetId != 0xff ? GetString("Targets") + $"<b><color=#ff1919>{output}</color></b>" : string.Empty;
    }
    public static void ResetTargets()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        Targets.Clear();
        for (var i = 0; i < NumOfTargets.GetInt(); i++)
        {
            try
            {
                var cTargets = new List<PlayerControl>(Main.AllAlivePlayerControls.Where(pc => !Targets.Contains(pc.PlayerId) && pc.GetCustomRole() != CustomRoles.Huntsman));
                var rand = IRandom.Instance;
                var target = cTargets[rand.Next(0, cTargets.Count)];
                var targetId = target.PlayerId;
                Targets.Add(targetId);
            }
            catch (System.Exception ex)
            {
                Logger.Warn($"Not enough targets for Head Hunter could be assigned. This may be due to a low player count or the following error:\n\n{ex}", "HuntsmanAssignTargets");
                break;
            }
        }

        Utils.NotifyRoles(SpecifySeer: Utils.GetPlayerById(playerIdList[0]));
    }
}
