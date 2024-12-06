using AmongUs.GameOptions;
using TOHE.Modules;
using UnityEngine;

namespace TOHE.Roles.Impostor;

internal class SoulCatcher : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 4600;
    private static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();

    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\

    private static OptionItem ShapeSoulCatcherShapeshiftDuration;
    private static OptionItem SoulCatcherShapeshiftCooldown;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.SoulCatcher);
        ShapeSoulCatcherShapeshiftDuration = FloatOptionItem.Create(Id + 2, GeneralOption.ShapeshifterBase_ShapeshiftDuration, new(2.5f, 180f, 2.5f), 300, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.SoulCatcher])
            .SetValueFormat(OptionFormat.Seconds);
        SoulCatcherShapeshiftCooldown = FloatOptionItem.Create(Id + 3, GeneralOption.ShapeshifterBase_ShapeshiftCooldown, new(1f, 180f, 1f), 15f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.SoulCatcher])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        PlayerIds.Clear();
    }
    public override void Add(byte playerId)
    {
        PlayerIds.Add(playerId);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterDuration = ShapeSoulCatcherShapeshiftDuration.GetFloat();
        AURoleOptions.ShapeshifterCooldown = SoulCatcherShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterLeaveSkin = false;
    }

    public override void SetAbilityButtonText(HudManager hud, byte id) => hud.AbilityButton.OverrideText(Translator.GetString("SoulCatcherButtonText"));
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Teleport");

    public override bool OnCheckShapeshift(PlayerControl shapeshifter, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate)
    {
        if (shapeshifter.PlayerId == target.PlayerId) return false;

        if (shapeshifter.CanBeTeleported() && target.CanBeTeleported())
        {
            var originPs = target.GetCustomPosition();
            target.RpcTeleport(shapeshifter.GetCustomPosition());
            shapeshifter.RpcTeleport(originPs);

            shapeshifter.RPCPlayCustomSound("Teleport");
            target.RPCPlayCustomSound("Teleport");
        }
        else
        {
            shapeshifter.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCatcher), Translator.GetString("ErrorTeleport")));
        }
        return false;
    }
}
