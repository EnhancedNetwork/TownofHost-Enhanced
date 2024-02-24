using AmongUs.GameOptions;
using TOHE.Modules;

namespace TOHE.Roles.Impostor;

internal class SoulCatcher : RoleBase
{
    private const int Id = 4600;
    public static bool On;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;

    private static OptionItem ShapeSoulCatcherShapeshiftDuration;
    private static OptionItem SoulCatcherShapeshiftCooldown;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.SoulCatcher);
        ShapeSoulCatcherShapeshiftDuration = FloatOptionItem.Create(Id + 2, "ShapeshiftDuration", new(2.5f, 180f, 2.5f), 300, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.SoulCatcher])
            .SetValueFormat(OptionFormat.Seconds);
        SoulCatcherShapeshiftCooldown = FloatOptionItem.Create(Id + 3, "ShapeshiftCooldown", new(1f, 180f, 1f), 15f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.SoulCatcher])
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
        AURoleOptions.ShapeshifterDuration = ShapeSoulCatcherShapeshiftDuration.GetFloat();
        AURoleOptions.ShapeshifterCooldown = SoulCatcherShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterLeaveSkin = false;
    }
    public override void SetAbilityButtonText(HudManager hud, byte id) => hud.AbilityButton.OverrideText(Translator.GetString("SoulCatcherButtonText"));

    public override void OnShapeshift(PlayerControl shapeshifter, PlayerControl target, bool shapeshifting, bool shapeshiftIsHidden)
    {
        if (!shapeshifting && !shapeshiftIsHidden) return;

        var timer = shapeshiftIsHidden ? 0.2f : 1.5f;
        _ = new LateTask(() =>
        {
            if (shapeshifter.CanBeTeleported() && target.CanBeTeleported())
            {
                var originPs = target.GetCustomPosition();
                target.RpcTeleport(shapeshifter.GetCustomPosition());
                shapeshifter.RpcTeleport(originPs);

                shapeshifter.RPCPlayCustomSound("Teleport");
                target.RPCPlayCustomSound("Teleport");
            }
        }, timer, "Soul Catcher teleport");
    }
}
