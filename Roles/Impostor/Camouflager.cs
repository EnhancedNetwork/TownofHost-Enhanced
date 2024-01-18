
namespace TOHE.Roles.Impostor;

public static class Camouflager
{
    private static readonly int Id = 2900;
    public static bool IsEnable = false;

    private static OptionItem CamouflageCooldown;
    private static OptionItem CamouflageDuration;
    public static OptionItem CanUseCommsSabotage;
    public static OptionItem DisableReportWhenCamouflageIsActive;

    public static bool AbilityActivated;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Camouflager);
        CamouflageCooldown = FloatOptionItem.Create(Id + 2, "CamouflageCooldown", new(1f, 180f, 1f), 25f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Camouflager])
            .SetValueFormat(OptionFormat.Seconds);
        CamouflageDuration = FloatOptionItem.Create(Id + 4, "CamouflageDuration", new(1f, 180f, 1f), 10f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Camouflager])
            .SetValueFormat(OptionFormat.Seconds);
        CanUseCommsSabotage = BooleanOptionItem.Create(Id + 6, "CanUseCommsSabotage", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Camouflager]);
        DisableReportWhenCamouflageIsActive = BooleanOptionItem.Create(Id + 8, "DisableReportWhenCamouflageIsActive", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Camouflager]);

    }
    public static void Init()
    {
        AbilityActivated = false;
        IsEnable = false;
    }
    public static void Add()
    {
        IsEnable = true;
    }

    public static void ApplyGameOptions()
    {
        AURoleOptions.ShapeshifterCooldown = CamouflageCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = CamouflageDuration.GetFloat();
    }
    public static void OnShapeshift()
    {
        AbilityActivated = true;

        _ = new LateTask(() =>
        {
            if (!Main.MeetingIsStarted)
                Camouflage.CheckCamouflage();
        }, 1.2f, "Camouflager Use Shapeshift");
    }
    public static void OnReportDeadBody()
    {
        ClaerCamouflage();
    }
    public static void IsDead()
    {
        if (GameStates.IsMeeting) return;

        ClaerCamouflage();
    }
    private static void ClaerCamouflage()
    {
        AbilityActivated = false;
        Camouflage.CheckCamouflage();
    }
}
