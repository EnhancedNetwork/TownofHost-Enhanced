using AmongUs.GameOptions;
using TOHE.Modules;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor;

internal class Disperser : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Disperser;
    private const int Id = 24400;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorHindering;
    //==================================================================\\

    private static OptionItem DisperserShapeshiftCooldown;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Disperser);
        DisperserShapeshiftCooldown = FloatOptionItem.Create(Id + 5, GeneralOption.ShapeshifterBase_ShapeshiftCooldown, new(1f, 180f, 1f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Disperser])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = DisperserShapeshiftCooldown.GetFloat();
    }
    public override void UnShapeShiftButton(PlayerControl shapeshifter)
    {
        if (shapeshifter.PlayerId == target.PlayerId) return false;

        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (!pc.CanBeTeleported())
            {
                pc.Notify(ColorString(GetRoleColor(CustomRoles.Disperser), GetString("ErrorTeleport")));
                continue;
            }

            pc.RPCPlayCustomSound("Teleport");
            pc.RpcRandomVentTeleport();
            pc.Notify(ColorString(GetRoleColor(CustomRoles.Disperser), GetString("TeleportedInRndVentByDisperser")));
        }
    }
}
