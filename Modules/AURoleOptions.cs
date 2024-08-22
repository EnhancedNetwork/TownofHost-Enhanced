using AmongUs.GameOptions;

namespace TOHE;

public static class AURoleOptions
{
    private static IGameOptions Opt;
    public static void SetOpt(IGameOptions opt) => Opt = opt;
    public static int EmergencyMeetings
    {
        get => Opt.GetInt(Int32OptionNames.NumEmergencyMeetings);
        set => Opt.SetInt(Int32OptionNames.NumEmergencyMeetings, value);
    }
    public static float KillCooldown
    {
        get => Opt.GetFloat(FloatOptionNames.KillCooldown);
        set => Opt.SetFloat(FloatOptionNames.KillCooldown, value);
    }
    public static float PlayerSpeedMod
    {
        get => Opt.GetFloat(FloatOptionNames.PlayerSpeedMod);
        set => Opt.SetFloat(FloatOptionNames.PlayerSpeedMod, value);
    }
    public static float ScientistCooldown
    {
        get => Opt.GetFloat(FloatOptionNames.ScientistCooldown);
        set => Opt.SetFloat(FloatOptionNames.ScientistCooldown, value);
    }
    public static float ScientistBatteryCharge
    {
        get => Opt.GetFloat(FloatOptionNames.ScientistBatteryCharge);
        set => Opt.SetFloat(FloatOptionNames.ScientistBatteryCharge, value);
    }
    public static float EngineerCooldown
    {
        get => Opt.GetFloat(FloatOptionNames.EngineerCooldown);
        set => Opt.SetFloat(FloatOptionNames.EngineerCooldown, value);
    }
    public static float EngineerInVentMaxTime
    {
        get => Opt.GetFloat(FloatOptionNames.EngineerInVentMaxTime);
        set => Opt.SetFloat(FloatOptionNames.EngineerInVentMaxTime, value);
    }
    public static float GuardianAngelCooldown
    {
        get => Opt.GetFloat(FloatOptionNames.GuardianAngelCooldown);
        set => Opt.SetFloat(FloatOptionNames.GuardianAngelCooldown, value);
    }
    public static float ProtectionDurationSeconds
    {
        get => Opt.GetFloat(FloatOptionNames.ProtectionDurationSeconds);
        set => Opt.SetFloat(FloatOptionNames.ProtectionDurationSeconds, value);
    }
    public static bool ImpostorsCanSeeProtect
    {
        get => Opt.GetBool(BoolOptionNames.ImpostorsCanSeeProtect);
        set => Opt.SetBool(BoolOptionNames.ImpostorsCanSeeProtect, value);
    }
    public static float ShapeshifterDuration
    {
        get => Opt.GetFloat(FloatOptionNames.ShapeshifterDuration);
        set => Opt.SetFloat(FloatOptionNames.ShapeshifterDuration, value);
    }
    public static float ShapeshifterCooldown
    {
        get => Opt.GetFloat(FloatOptionNames.ShapeshifterCooldown);
        set => Opt.SetFloat(FloatOptionNames.ShapeshifterCooldown, value);
    }
    public static bool ShapeshifterLeaveSkin
    {
        get => Opt.GetBool(BoolOptionNames.ShapeshifterLeaveSkin);
        set => Opt.SetBool(BoolOptionNames.ShapeshifterLeaveSkin, value);
    }
    public static bool NoisemakerImpostorAlert
    {
        get => Opt.GetBool(BoolOptionNames.NoisemakerImpostorAlert);
        set => Opt.SetBool(BoolOptionNames.NoisemakerImpostorAlert, value);
    }
    public static float NoisemakerAlertDuration
    {
        get => Opt.GetFloat(FloatOptionNames.NoisemakerAlertDuration);
        set => Opt.SetFloat(FloatOptionNames.NoisemakerAlertDuration, value);
    }
    public static float PhantomCooldown
    {
        get => Opt.GetFloat(FloatOptionNames.PhantomCooldown);
        set => Opt.SetFloat(FloatOptionNames.PhantomCooldown, value);
    }
    public static float PhantomDuration
    {
        get => Opt.GetFloat(FloatOptionNames.PhantomDuration);
        set => Opt.SetFloat(FloatOptionNames.PhantomDuration, value);
    }
    public static float TrackerCooldown
    {
        get => Opt.GetFloat(FloatOptionNames.TrackerCooldown);
        set => Opt.SetFloat(FloatOptionNames.TrackerCooldown, value);
    }
    public static float TrackerDuration
    {
        get => Opt.GetFloat(FloatOptionNames.TrackerDuration);
        set => Opt.SetFloat(FloatOptionNames.TrackerDuration, value);
    }
    public static float TrackerDelay
    {
        get => Opt.GetFloat(FloatOptionNames.TrackerDelay);
        set => Opt.SetFloat(FloatOptionNames.TrackerDelay, value);
    }
}