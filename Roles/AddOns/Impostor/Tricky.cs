using static TOHE.Options;

namespace TOHE.Roles.AddOns.Impostor;
public class Tricky : IAddon
{
    public CustomRoles Role => CustomRoles.Tricky;
    private const int Id = 19900;
    public AddonTypes Type => AddonTypes.Impostor;
    private static OptionItem EnabledDeathReasons;
    //private static Dictionary<byte, PlayerState.DeathReason> randomReason = [];

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Tricky, canSetNum: true, tab: TabGroup.Addons);
        EnabledDeathReasons = BooleanOptionItem.Create(Id + 11, "OnlyEnabledDeathReasons", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Tricky]);
    }
    public void Init()
    {
        //randomReason = [];
    }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
    private static PlayerState.DeathReason ChangeRandomDeath()
    {
        PlayerState.DeathReason[] deathReasons = EnumHelper.GetAllValues<PlayerState.DeathReason>().Where(IsReasonEnabled).ToArray();
        if (deathReasons.Length == 0 || !deathReasons.Contains(PlayerState.DeathReason.Kill)) deathReasons.AddItem(PlayerState.DeathReason.Kill);
        var random = IRandom.Instance;
        int randomIndex = random.Next(deathReasons.Length);
        return deathReasons[randomIndex];
    }
    private static bool IsReasonEnabled(PlayerState.DeathReason reason)
    {
        if (reason is PlayerState.DeathReason.etc) return false;
        if (!EnabledDeathReasons.GetBool()) return true;
        return reason.DeathReasonIsEnable();
    }
    public static void AfterPlayerDeathTasks(PlayerControl target)
    {
        if (target == null) return;
        _ = new LateTask(() =>
        {
            var killer = target.GetRealKiller();
            if (killer == null || !killer.Is(CustomRoles.Tricky)) return;

            var randomDeathReason = ChangeRandomDeath();
            Main.PlayerStates[target.PlayerId].deathReason = randomDeathReason;
            Main.PlayerStates[target.PlayerId].SetDead();

            Utils.NotifyRoles(SpecifySeer: target);
            Logger.Info($"Set death reason: {randomDeathReason}", "Tricky");
        }, 0.3f, "Tricky random death reason");
    }
}
