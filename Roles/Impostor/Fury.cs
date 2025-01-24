using AmongUs.GameOptions;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Fury : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 31400;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Fury);
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    public override CustomRoles Role => CustomRoles.Fury;
    //==================================================================\\

    public static OptionItem KillCooldown;
    private static OptionItem AbilityCooldown;
    private static OptionItem RageDuration;
    private static OptionItem SpeedInRage;
    private static OptionItem RageKillCooldown;
    private static OptionItem NotifyRageActive;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Fury);
        KillCooldown = FloatOptionItem.Create(Id + 2, GeneralOption.KillCooldown, new(0f, 120f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury])
            .SetValueFormat(OptionFormat.Seconds);
        AbilityCooldown = FloatOptionItem.Create(Id + 3, "FuryAbilityCooldown", new(2.5f, 120f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury])
            .SetValueFormat(OptionFormat.Seconds);
        RageDuration = FloatOptionItem.Create(Id + 4, "FuryRageDuration", new(2.5f, 60f, 2.5f), 15f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury])
                .SetValueFormat(OptionFormat.Seconds);
        SpeedInRage = FloatOptionItem.Create(Id + 5, "FurySpeedInRage", new(0.1f, 3f, 0.1f), 3f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury])
            .SetValueFormat(OptionFormat.Multiplier);
        RageKillCooldown = FloatOptionItem.Create(Id + 6, "FuryRageKillCooldown", new(0f, 120f, 2.5f), 5f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Fury])
            .SetValueFormat(OptionFormat.Seconds);
        NotifyRageActive = BooleanOptionItem.Create(Id + 7, "SeerNotifyRageActive", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fury]);
    }
    public override void Init()
    {
        playerIdList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public override bool IsEnable => playerIdList.Any();

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = AbilityCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }
    public override void UnShapeShiftButton(PlayerControl player)
    {
        player.SetKillCooldown(RageKillCooldown.GetFloat());
        player.Notify(GetString("FuryInRage"), RageDuration.GetFloat());
        foreach (var target in Main.AllPlayerControls)
        {
            if (NotifyRageActive.GetBool()) target.KillFlash();
            if (NotifyRageActive.GetBool()) target.Notify(GetString("SeerFuryInRage"), 5f);
        }
            player.MarkDirtySettings();
            var tmpSpeed = Main.AllPlayerSpeed[player.PlayerId];
            Main.AllPlayerSpeed[player.PlayerId] = SpeedInRage.GetFloat();
            var tmpKillCooldown = Main.AllPlayerKillCooldown[player.PlayerId];
            Main.AllPlayerKillCooldown[player.PlayerId] = RageKillCooldown.GetFloat();

        _ = new LateTask(() =>
        {
            Main.AllPlayerSpeed[player.PlayerId] = Main.AllPlayerSpeed[player.PlayerId] - SpeedInRage.GetFloat() + tmpSpeed;
            Main.AllPlayerKillCooldown[player.PlayerId] = Main.AllPlayerKillCooldown[player.PlayerId] - RageKillCooldown.GetFloat() + tmpKillCooldown;
            player.MarkDirtySettings();
        }, RageDuration.GetFloat());
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(GetString("FuryShapeshiftText"));
    }
}
