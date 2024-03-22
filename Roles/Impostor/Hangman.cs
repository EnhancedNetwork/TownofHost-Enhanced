using AmongUs.GameOptions;
using TOHE.Roles.AddOns.Impostor;
using static TOHE.Options;
namespace TOHE.Roles.Impostor;

internal class Hangman : RoleBase
{
    private const int Id = 24500;
    public static bool On;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;

    private static OptionItem ShapeshiftCooldown;
    private static OptionItem ShapeshiftDuration;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Hangman);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 2, "ShapeshiftCooldown", new(1f, 180f, 1f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hangman])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftDuration = FloatOptionItem.Create(Id + 4, "ShapeshiftDuration", new(1f, 60f, 1f), 10f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hangman])
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

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = ShapeshiftDuration.GetFloat();
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.Is(CustomRoles.Pestilence))
            return true;

        if (target.Is(CustomRoles.Madmate) && !Madmate.ImpCanKillMadmate.GetBool())
            return false;

        if (Main.CheckShapeshift.TryGetValue(killer.PlayerId, out var isShapeshift) && isShapeshift)
        {
            Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.LossOfHead;
            target.RpcExileV2();
            Main.PlayerStates[target.PlayerId].SetDead();
            target.Data.IsDead = true;
            target.SetRealKiller(killer);

            killer.SetKillCooldown();
            MurderPlayerPatch.AfterPlayerDeathTasks(killer, target, false);
            return false;
        }
        return true;
    }
}