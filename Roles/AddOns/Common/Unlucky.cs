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

    public static readonly Dictionary<byte, bool> UnluckCheck = [];

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
        ImpCanBeUnlucky = BooleanOptionItem.Create("ImpCanBeUnlucky", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Unlucky]);
        CrewCanBeUnlucky = BooleanOptionItem.Create("CrewCanBeUnlucky", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Unlucky]);
        NeutralCanBeUnlucky = BooleanOptionItem.Create("NeutralCanBeUnlucky", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Unlucky]);
    }
    
    public static void Init()
    {
        UnluckCheck.Clear();
    }
    public static void Add(byte PlayerId)
    {
        UnluckCheck.Add(PlayerId, false);
    }
    public static void Remove(byte player)
    {
        UnluckCheck.Remove(player);
    }

    public static void SuicideRand(PlayerControl victim, StateSuicide state)
    {
        var random = IRandom.Instance;
        var shouldBeSuicide = false;

        switch (state)
        {
            case StateSuicide.TryKill:
                shouldBeSuicide = random.Next(1, 101) <= UnluckyKillSuicideChance.GetInt();
                break;
            case StateSuicide.CompleteTask:
                shouldBeSuicide = random.Next(1, 101) <= UnluckyTaskSuicideChance.GetInt();
                break;
            case StateSuicide.EnterVent:
                shouldBeSuicide = random.Next(1, 101) <= UnluckyVentSuicideChance.GetInt();
                break;
            case StateSuicide.ReportDeadBody:
                shouldBeSuicide = random.Next(1, 101) <= UnluckyReportSuicideChance.GetInt();
                break;
            case StateSuicide.OpenDoor:
                shouldBeSuicide = random.Next(1, 101) <= UnluckyOpenDoorSuicideChance.GetInt();
                break;
        }

        if (shouldBeSuicide)
        {
            Main.PlayerStates[victim.PlayerId].deathReason = PlayerState.DeathReason.Suicide;
            victim.RpcMurderPlayer(victim);
            UnluckCheck[victim.PlayerId] = true;
        }
    }
}