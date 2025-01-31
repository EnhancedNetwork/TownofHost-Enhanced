using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Unlucky : IAddon
{
    public CustomRoles Role => CustomRoles.Unlucky;
    private const int Id = 21000;
    public AddonTypes Type => AddonTypes.Harmful;

    private static OptionItem UnluckyTaskSuicideChance;
    private static OptionItem UnluckyKillSuicideChance;
    private static OptionItem UnluckyVentSuicideChance;
    private static OptionItem UnluckyReportSuicideChance;
    private static OptionItem UnluckyOpenDoorSuicideChance;

    [Obfuscation(Exclude = true)]
    public enum StateSuicide
    {
        TryKill,
        CompleteTask,
        EnterVent,
        ReportDeadBody,
        OpenDoor
    }

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Unlucky, canSetNum: true, teamSpawnOptions: true);
        UnluckyKillSuicideChance = IntegerOptionItem.Create(Id + 10, "UnluckyKillSuicideChance", new(0, 100, 1), 2, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Unlucky])
            .SetValueFormat(OptionFormat.Percent);
        UnluckyTaskSuicideChance = IntegerOptionItem.Create(Id + 11, "UnluckyTaskSuicideChance", new(0, 100, 1), 5, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Unlucky])
            .SetValueFormat(OptionFormat.Percent);
        UnluckyVentSuicideChance = IntegerOptionItem.Create(Id + 12, "UnluckyVentSuicideChance", new(0, 100, 1), 3, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Unlucky])
            .SetValueFormat(OptionFormat.Percent);
        UnluckyReportSuicideChance = IntegerOptionItem.Create(Id + 13, "UnluckyReportSuicideChance", new(0, 100, 1), 1, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Unlucky])
            .SetValueFormat(OptionFormat.Percent);
        UnluckyOpenDoorSuicideChance = IntegerOptionItem.Create(Id + 14, "UnluckyOpenDoorSuicideChance", new(0, 100, 1), 4, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Unlucky])
            .SetValueFormat(OptionFormat.Percent);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
    public static bool SuicideRand(PlayerControl victim, StateSuicide state)
    {
        var shouldBeSuicide = IRandom.Instance.Next(1, 100) <= state switch
        {
            StateSuicide.TryKill => UnluckyKillSuicideChance.GetInt(),
            StateSuicide.CompleteTask => UnluckyTaskSuicideChance.GetInt(),
            StateSuicide.EnterVent => UnluckyVentSuicideChance.GetInt(),
            StateSuicide.ReportDeadBody => UnluckyReportSuicideChance.GetInt(),
            StateSuicide.OpenDoor => UnluckyOpenDoorSuicideChance.GetInt(),

            _ => -1
        };

        if (shouldBeSuicide && !victim.IsTransformedNeutralApocalypse())
        {
            victim.SetDeathReason(PlayerState.DeathReason.Suicide);
            victim.RpcMurderPlayer(victim);
            return true;
        }
        return false;
    }
}
