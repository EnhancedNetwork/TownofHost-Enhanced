using AmongUs.GameOptions;
using TOHE.Modules;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Miner : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Miner;
    private const int Id = 4200;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\

    private static OptionItem MinerSSCD;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Miner);
        MinerSSCD = FloatOptionItem.Create(Id + 3, GeneralOption.ShapeshifterBase_ShapeshiftCooldown, new(1f, 180f, 1f), 15f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Miner])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = MinerSSCD.GetFloat();
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(GetString("MinerTeleButtonText"));
    }

    public override void UnShapeShiftButton(PlayerControl shapeshifter)
    {
        if (Main.LastEnteredVent.ContainsKey(shapeshifter.PlayerId))
        {
            var lastVentPosition = Main.LastEnteredVentLocation[shapeshifter.PlayerId];
            Logger.Info($"Miner - {shapeshifter.GetNameWithRole()}:{lastVentPosition}", "MinerTeleport");
            shapeshifter.RpcTeleport(lastVentPosition);
            shapeshifter.RPCPlayCustomSound("Teleport");
        }
    }
}
