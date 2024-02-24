using System.Collections.Generic;

namespace TOHE.Roles.Impostor;

internal class Cleaner : RoleBase
{
    private const int Id = 3000;
    public static bool On;
    public override bool IsEnable => On;

    private static OptionItem KillCooldown;
    private static OptionItem KillCooldownAfterCleaning;

    private static List<byte> CleanerBodies = [];

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Cleaner);
        KillCooldown = FloatOptionItem.Create(Id + 2, "KillCooldown", new(5f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Cleaner])
            .SetValueFormat(OptionFormat.Seconds);
        KillCooldownAfterCleaning = FloatOptionItem.Create(Id + 3, "KillCooldownAfterCleaning", new(5f, 180f, 2.5f), 60f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Cleaner])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        CleanerBodies = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        On = true;
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public static bool BodyIsCleansed(byte targetId) => CleanerBodies.Contains(targetId);

    public override bool OnPressReportButton(PlayerControl reporter, PlayerControl target)
    {
        CleanerBodies.Remove(target.PlayerId);
        CleanerBodies.Add(target.PlayerId);

        reporter.Notify(Translator.GetString("CleanerCleanBody"));
        reporter.SetKillCooldownV3(KillCooldownAfterCleaning.GetFloat(), forceAnime: true);

        Logger.Info($"Cleaner: {reporter.GetRealName()} clear body: {target.GetRealName()}", "Cleaner");
        return false;
    }
}
