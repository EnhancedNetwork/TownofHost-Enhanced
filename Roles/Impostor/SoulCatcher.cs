using AmongUs.GameOptions;
using TOHE.Modules;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor;

internal class SoulCatcher : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.SoulCatcher;
    private const int Id = 4600;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\

    private static OptionItem SoulCatcherShapeshiftCooldown;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.SoulCatcher);
        SoulCatcherShapeshiftCooldown = FloatOptionItem.Create(Id + 3, GeneralOption.ShapeshifterBase_ShapeshiftCooldown, new(1f, 180f, 1f), 15f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.SoulCatcher])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = SoulCatcherShapeshiftCooldown.GetFloat();
    }

    public override void SetAbilityButtonText(HudManager hud, byte id) => hud.AbilityButton.OverrideText(GetString("SoulCatcherButtonText"));
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
            resetCooldown = true;
        }
        else
        {
            shapeshifter.Notify(ColorString(GetRoleColor(CustomRoles.SoulCatcher), GetString("ErrorTeleport")));
        }
        return false;
    }
}
