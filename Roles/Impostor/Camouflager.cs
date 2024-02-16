using System.Collections.Generic;

namespace TOHE.Roles.Impostor;

public static class Camouflager
{
    private static readonly int Id = 2900;
    public static bool IsEnable = false;

    private static OptionItem CamouflageCooldownOpt;
    private static OptionItem CamouflageDurationOpt;
    public static OptionItem CanUseCommsSabotage;
    public static OptionItem DisableReportWhenCamouflageIsActive;

    public static bool AbilityActivated = false;
    public static bool ShapeshiftIsHidden = false;
    public static float CamouflageCooldown;
    public static float CamouflageDuration;

    private static Dictionary<byte, long> Timer = [];

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Camouflager);
        CamouflageCooldownOpt = FloatOptionItem.Create(Id + 2, "CamouflageCooldown", new(1f, 180f, 1f), 25f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Camouflager])
            .SetValueFormat(OptionFormat.Seconds);
        CamouflageDurationOpt = FloatOptionItem.Create(Id + 4, "CamouflageDuration", new(1f, 180f, 1f), 10f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Camouflager])
            .SetValueFormat(OptionFormat.Seconds);
        CanUseCommsSabotage = BooleanOptionItem.Create(Id + 6, "CanUseCommsSabotage", false, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Camouflager]);
        DisableReportWhenCamouflageIsActive = BooleanOptionItem.Create(Id + 8, "DisableReportWhenCamouflageIsActive", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Camouflager]);

    }
    public static void Init()
    {
        AbilityActivated = false;
        IsEnable = false;
    }
    public static void Add()
    {
        CamouflageCooldown = CamouflageCooldownOpt.GetFloat();
        CamouflageDuration = CamouflageDurationOpt.GetFloat();
        ShapeshiftIsHidden = Options.DisableShapeshiftAnimations.GetBool();
        IsEnable = true;
    }

    public static void ApplyGameOptions()
    {
        AURoleOptions.ShapeshifterCooldown = CamouflageCooldown;
        AURoleOptions.ShapeshifterDuration = CamouflageDuration;
    }
    public static void OnShapeshift(PlayerControl camouflager = null, bool shapeshiftIsHidden = false)
    {
        AbilityActivated = true;
        var timer = shapeshiftIsHidden ? 0.1f : 1.2f;

        _ = new LateTask(() =>
        {
            if (!Main.MeetingIsStarted && GameStates.IsInTask)
            {
                Camouflage.CheckCamouflage();

                if (camouflager != null && shapeshiftIsHidden)
                    Timer.Add(camouflager.PlayerId, Utils.GetTimeStamp());
            }
        }, timer, "Camouflager Use Shapeshift");
    }
    public static void OnReportDeadBody()
    {
        ClearCamouflage();
    }
    public static void IsDead()
    {
        if (GameStates.IsMeeting) return;

        ClearCamouflage();
    }
    private static void ClearCamouflage()
    {
        AbilityActivated = false;
        Camouflage.CheckCamouflage();
    }
    public static void OnFixedUpdate(PlayerControl camouflager)
    {
        if (camouflager == null || !camouflager.IsAlive())
        {
            ClearCamouflage();
            return;
        }
        if (!Timer.TryGetValue(camouflager.PlayerId, out var oldTime)) return;

        var nowTime = Utils.GetTimeStamp();
        if (nowTime - oldTime >= CamouflageDuration)
        {
            ClearCamouflage();
            Timer.Remove(camouflager.PlayerId);
        }
    }
}
