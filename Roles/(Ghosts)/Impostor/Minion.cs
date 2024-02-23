using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

internal class Minion : RoleBase
{
    private const int Id = 27900;

    public static OptionItem AbilityCooldown;
    public static OptionItem AbilityTime;
    public static bool On;
    public override bool IsEnable => On;

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Minion);
        AbilityCooldown = FloatOptionItem.Create(Id + 10, "AbilityCooldown", new(2.5f, 120f, 2.5f), 40f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Minion])
            .SetValueFormat(OptionFormat.Seconds);
        AbilityTime = FloatOptionItem.Create(Id + 11, "MinionAbilityTime", new(1f, 10f, 1f), 5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Minion])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        On = false;
    }
    public override void Add(byte playerId)
    {
        On = true;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte PlayerId)
    {
        AURoleOptions.GuardianAngelCooldown = AbilityCooldown.GetFloat();
        AURoleOptions.ProtectionDurationSeconds = 0f;
    }
    public override bool OnCheckProtect(PlayerControl angel, PlayerControl target)
    {
        var ImpPVC = target.GetCustomRole().IsImpostor();
        if (!ImpPVC)
        {
            Main.PlayerStates[target.PlayerId].IsBlackOut = true;
            target.MarkDirtySettings();
            
            _ = new LateTask(() =>
            {
                Main.PlayerStates[target.PlayerId].IsBlackOut = false;
                target.MarkDirtySettings();
            }, AbilityTime.GetFloat(), "Minion: return vision");
            angel.RpcResetAbilityCooldown();
        }
        return false;
    }
}

