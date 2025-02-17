using UnityEngine;

namespace TOHE.Roles.Impostor;

internal class Cleaner : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Cleaner;
    private const int Id = 3000;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    public override Sprite ReportButtonSprite => CustomButton.Get("Clean");

    private static OptionItem KillCooldown;
    private static OptionItem KillCooldownAfterCleaning;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Cleaner);
        KillCooldown = FloatOptionItem.Create(Id + 2, GeneralOption.KillCooldown, new(5f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Cleaner])
            .SetValueFormat(OptionFormat.Seconds);
        KillCooldownAfterCleaning = FloatOptionItem.Create(Id + 3, "KillCooldownAfterCleaning", new(5f, 180f, 2.5f), 60f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Cleaner])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody, PlayerControl killer)
    {
        if (Main.UnreportableBodies.Contains(deadBody.PlayerId)) return false;

        if (reporter.Is(CustomRoles.Cleaner))
        {
            Main.UnreportableBodies.Add(deadBody.PlayerId);

            reporter.Notify(Translator.GetString("CleanerCleanBody"));
            reporter.SetKillCooldownV3(KillCooldownAfterCleaning.GetFloat(), forceAnime: true);

            Logger.Info($"Cleaner: {reporter.GetRealName()} clear body: {deadBody.PlayerName}", "Cleaner");
            return false;
        }

        return true;
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.ReportButton.OverrideText(Translator.GetString("CleanerReportButtonText"));
    }
}
