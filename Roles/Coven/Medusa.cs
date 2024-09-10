using AmongUs.GameOptions;
using static TOHE.Translator;
using static TOHE.Options;

namespace TOHE.Roles.Coven;

internal class Medusa : CovenManager
{
    //===========================SETUP================================\\
    private const int Id = 17000;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenUtility;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem KillCooldownAfterStoneGazing;
    private static OptionItem CanVent;
    private static OptionItem HasImpostorVision;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, CustomRoles.Medusa, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 12, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Medusa])
            .SetValueFormat(OptionFormat.Seconds);
        KillCooldownAfterStoneGazing = FloatOptionItem.Create(Id + 15, "KillCooldownAfterStoneGazing", new(0f, 180f, 2.5f), 40f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Medusa])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, GeneralOption.CanVent, true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Medusa]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, GeneralOption.ImpostorVision, true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Medusa]);
    }
    public override void Init()
    {
        playerIdList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();


    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target, PlayerControl killer)
    {
        if (reporter.Is(CustomRoles.Medusa))
        {
            Main.UnreportableBodies.Add(target.PlayerId);
            reporter.Notify(GetString("MedusaStoneBody"));

            reporter.SetKillCooldownV3(KillCooldownAfterStoneGazing.GetFloat(), forceAnime: true);
            Logger.Info($"{reporter.GetRealName()} stoned {target.PlayerName} body", "Medusa");
            return false;
        }
        return true;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.ReportButton.OverrideText(GetString("MedusaReportButtonText"));
    }
}
