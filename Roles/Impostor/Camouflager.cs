using AmongUs.GameOptions;
using System.Collections.Generic;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Camouflager : RoleBase
{
    private static readonly int Id = 2900;
    public static bool On;
    public override bool IsEnable => On;

    private static OptionItem CamouflageCooldownOpt;
    private static OptionItem CamouflageDurationOpt;
    private static OptionItem CanUseCommsSabotagOpt;
    private static OptionItem DisableReportWhenCamouflageIsActiveOpt;

    public static bool AbilityActivated = false;
    public static bool ShapeshiftIsHidden = false;
    private static float CamouflageCooldown;
    private static float CamouflageDuration;
    private static bool CanUseCommsSabotage;
    private static bool DisableReportWhenCamouflageIsActive;

    private static Dictionary<byte, long> Timer = [];

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Camouflager);
        CamouflageCooldownOpt = FloatOptionItem.Create(Id + 2, "CamouflageCooldown", new(1f, 180f, 1f), 25f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Camouflager])
            .SetValueFormat(OptionFormat.Seconds);
        CamouflageDurationOpt = FloatOptionItem.Create(Id + 4, "CamouflageDuration", new(1f, 180f, 1f), 10f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Camouflager])
            .SetValueFormat(OptionFormat.Seconds);
        CanUseCommsSabotagOpt = BooleanOptionItem.Create(Id + 6, "CanUseCommsSabotage", false, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Camouflager]);
        DisableReportWhenCamouflageIsActiveOpt = BooleanOptionItem.Create(Id + 8, "DisableReportWhenCamouflageIsActive", false, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Camouflager]);

    }
    public override void Init()
    {
        Timer = [];
        AbilityActivated = false;
        On = false;
    }
    public override void Add(byte playerId)
    {
        CamouflageCooldown = CamouflageCooldownOpt.GetFloat();
        CamouflageDuration = CamouflageDurationOpt.GetFloat();
        CanUseCommsSabotage = CanUseCommsSabotagOpt.GetBool();
        DisableReportWhenCamouflageIsActive = DisableReportWhenCamouflageIsActiveOpt.GetBool();
        
        ShapeshiftIsHidden = Options.DisableShapeshiftAnimations.GetBool();
        On = true;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftIsHidden && AbilityActivated ? CamouflageDuration : CamouflageCooldown;
        AURoleOptions.ShapeshifterDuration = CamouflageDuration;
    }
    public override void SetAbilityButtonText(HudManager __instance, byte id)
    {
        if (AbilityActivated)
            __instance.AbilityButton.OverrideText(GetString("CamouflagerShapeshiftTextAfterDisguise"));
        else
            __instance.AbilityButton.OverrideText(GetString("CamouflagerShapeshiftTextBeforeDisguise"));


        __instance.ReportButton.OverrideText(GetString("ReportButtonText"));
    }
    public override void OnShapeshift(PlayerControl camouflager, PlayerControl target, bool shapeshifting, bool shapeshiftIsHidden)
    {
        if (AbilityActivated && shapeshiftIsHidden)
        {
            Logger.Info("Rejected bcz the ss button is used to display skill timer", "Check ShapeShift");
            camouflager.RejectShapeshiftAndReset(reset: false);
            return;
        }
        if (!shapeshifting && !shapeshiftIsHidden)
        {
            ClearCamouflage();
            Timer = [];
            return;
        }

        AbilityActivated = true;
        
        var timer = 1.2f;
        if (shapeshiftIsHidden)
        {
            timer = 0.1f;
            camouflager.SyncSettings();
        }

        _ = new LateTask(() =>
        {
            if (!Main.MeetingIsStarted && GameStates.IsInTask)
            {
                Camouflage.CheckCamouflage();

                if (camouflager != null && shapeshiftIsHidden)
                {
                    Timer.Add(camouflager.PlayerId, Utils.GetTimeStamp());
                }
            }
        }, timer, "Camouflager Use Shapeshift");
    }
    public override void OnReportDeadBody(PlayerControl reporter, PlayerControl target)
    {
        ClearCamouflage();
        Timer = [];
    }
    public override void OnPlayerDead(PlayerControl killer, PlayerControl target)
    {
        if (GameStates.IsMeeting || !Camouflager.AbilityActivated) return;

        ClearCamouflage();
    }

    public static bool CantPressCommsSabotageButton(PlayerControl player) => player.Is(CustomRoles.Camouflager) && !CanUseCommsSabotage;
    public static bool CantPressOnReportButton() => DisableReportWhenCamouflageIsActive && AbilityActivated && !(Utils.IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool());

    private static void ClearCamouflage()
    {
        AbilityActivated = false;
        Camouflage.CheckCamouflage();
    }
    public override void OnFixedUpdate(PlayerControl camouflager)
    {
        if (!ShapeshiftIsHidden && !AbilityActivated) return;

        if (camouflager == null || !camouflager.IsAlive())
        {
            Timer.Remove(camouflager.PlayerId);
            ClearCamouflage();
            camouflager.SyncSettings();
            camouflager.RpcResetAbilityCooldown();
            return;
        }
        if (!Timer.TryGetValue(camouflager.PlayerId, out var oldTime)) return;

        var nowTime = Utils.GetTimeStamp();
        if (nowTime - oldTime >= CamouflageDuration)
        {
            Timer.Remove(camouflager.PlayerId);
            ClearCamouflage();
            camouflager.SyncSettings();
            camouflager.RpcResetAbilityCooldown();
        }
    }
}
