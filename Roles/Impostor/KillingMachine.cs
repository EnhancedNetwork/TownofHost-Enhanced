using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

internal class KillingMachine : RoleBase
{
    private const int Id = 23800;

    public static bool On;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    private static OptionItem MNKillCooldown;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.KillingMachine);
        MNKillCooldown = FloatOptionItem.Create(Id + 5, "KillCooldown", new(2.5f, 180f, 2.5f), 10f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.KillingMachine])
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

    public override bool CanUseImpostorVentButton(PlayerControl pc) => false;
    public override bool CanUseSabotage(PlayerControl pc) => false;
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = MNKillCooldown.GetFloat();

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0.2f);
    }

    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        killer.RpcMurderPlayerV3(target);
        killer.ResetKillCooldown();
        return false;
    }
}
