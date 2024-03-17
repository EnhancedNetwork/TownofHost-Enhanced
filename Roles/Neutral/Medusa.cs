using AmongUs.GameOptions;
using System.Collections.Generic;
using static TOHE.Translator;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Medusa : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 17000;
    public static HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem KillCooldownAfterStoneGazing;
    private static OptionItem CanVent;
    private static OptionItem HasImpostorVision;

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Medusa, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 12, "KillCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Medusa])
            .SetValueFormat(OptionFormat.Seconds);
        KillCooldownAfterStoneGazing = FloatOptionItem.Create(Id + 15, "KillCooldownAfterStoneGazing", new(0f, 180f, 2.5f), 40f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Medusa])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, "CanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Medusa]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Medusa]);
    }
    public override void Init()
    {
        playerIdList = [];
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();

    public override bool OnCheckReportDeadBody(PlayerControl __instance, GameData.PlayerInfo target, PlayerControl killer)
    {
        Main.UnreportableBodies.Add(target.PlayerId);
        __instance.Notify(GetString("MedusaStoneBody"));

        __instance.SetKillCooldownV3(KillCooldownAfterStoneGazing.GetFloat(), forceAnime: true);
        Logger.Info($"{__instance.GetRealName()} stoned {target.PlayerName} body", "Medusa");
        return false;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.ReportButton.OverrideText(GetString("MedusaReportButtonText"));
    }
}
