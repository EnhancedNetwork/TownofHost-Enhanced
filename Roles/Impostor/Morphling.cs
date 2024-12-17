using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

internal class Morphling : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Morphling;
    private const int Id = 3500;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //===========================SETUP================================\\

    private static OptionItem KillCooldown;
    private static OptionItem ShapeshiftCD;
    private static OptionItem ShapeshiftDur;


    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Morphling);
        KillCooldown = FloatOptionItem.Create(Id + 14, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Morphling])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftCD = FloatOptionItem.Create(Id + 15, GeneralOption.ShapeshifterBase_ShapeshiftCooldown, new(1f, 180f, 1f), 10f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Morphling])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftDur = FloatOptionItem.Create(Id + 16, GeneralOption.ShapeshifterBase_ShapeshiftDuration, new(1f, 180f, 1f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Morphling])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override bool CanUseKillButton(PlayerControl player)
    {
        if (player == null || !player.IsAlive()) return false;

        Main.CheckShapeshift.TryGetValue(player.PlayerId, out var IsShapeshift);

        return IsShapeshift;
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCD.GetFloat();
        AURoleOptions.ShapeshifterDuration = ShapeshiftDur.GetFloat();
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
}
