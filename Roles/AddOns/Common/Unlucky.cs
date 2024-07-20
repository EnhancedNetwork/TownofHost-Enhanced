using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Unlucky
{
    private const int Id = 21000;

    private static OptionItem UnluckyTaskSuicideChance;
    private static OptionItem UnluckyKillSuicideChance;
    private static OptionItem UnluckyVentSuicideChance;
    private static OptionItem UnluckyReportSuicideChance;
    private static OptionItem UnluckyOpenDoorSuicideChance;
    public static OptionItem ImpCanBeUnlucky;
    public static OptionItem CrewCanBeUnlucky;
    public static OptionItem NeutralCanBeUnlucky;

    public enum StateSuicide
    {
        TryKill,
        CompleteTask,
        EnterVent,
        ReportDeadBody,
        OpenDoor
    }

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Unlucky, canSetNum: true);
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
        ImpCanBeUnlucky = BooleanOptionItem.Create(Id + 15, "ImpCanBeUnlucky", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Unlucky]);
        CrewCanBeUnlucky = BooleanOptionItem.Create(Id + 16, "CrewCanBeUnlucky", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Unlucky]);
        NeutralCanBeUnlucky = BooleanOptionItem.Create(Id + 17, "NeutralCanBeUnlucky", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Unlucky]);
    }
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

        if (shouldBeSuicide)
        {
            victim.SetDeathReason(PlayerState.DeathReason.Suicide);
            victim.RpcMurderPlayer(victim);
            return true;
        }
        return false;
    }
}