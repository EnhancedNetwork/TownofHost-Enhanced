using AmongUs.GameOptions;
using TOHE.Roles.Double;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Werewolf : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Werewolf;
    private const int Id = 18400;

    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem MaulRadius;
    private static OptionItem CanVent;
    private static OptionItem HasImpostorVision;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Werewolf, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 9, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 35f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Werewolf])
            .SetValueFormat(OptionFormat.Seconds);
        MaulRadius = FloatOptionItem.Create(Id + 14, "MaulRadius", new(0.5f, 1.5f, 0.1f), 1.3f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Werewolf])
            .SetValueFormat(OptionFormat.Multiplier);
        CanVent = BooleanOptionItem.Create(Id + 11, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Werewolf]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Werewolf]);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public override void SetAbilityButtonText(HudManager hud, byte playerId) => hud.KillButton.OverrideText(Translator.GetString("WerewolfKillButtonText"));

    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        Logger.Info("Werewolf Kill", "Mauled");
        _ = new LateTask(() =>
        {
            foreach (var player in Main.AllAlivePlayerControls)
            {
                if (player == killer) continue;
                if (player == target) continue;

                if (player.IsTransformedNeutralApocalypse()) continue;
                else if ((player.Is(CustomRoles.NiceMini) || player.Is(CustomRoles.EvilMini)) && Mini.Age < 18) continue;

                if (Utils.GetDistance(killer.transform.position, player.transform.position) <= MaulRadius.GetFloat())
                {
                    player.SetDeathReason(PlayerState.DeathReason.Mauled);
                    player.RpcMurderPlayer(player);
                    player.SetRealKiller(killer);
                }
            }
        }, 0.1f, "Werewolf Maul Bug Fix");
        return true;
    }
}
