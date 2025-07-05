namespace TOHE;

//参考元:https://github.com/yukieiji/ExtremeRoles/blob/master/ExtremeRoles/Patches/AirshipStatusPatch.cs
[HarmonyPatch(typeof(AirshipStatus), nameof(AirshipStatus.PrespawnStep))]
public static class AirshipStatusPrespawnStepPatch
{
    public static bool Prefix()
    {
        Logger.Info("AirshipStatus.PrespawnStep called", "AirshipStatus");

        if (PlayerControl.LocalPlayer.Is(CustomRoles.GM))
        {
            RandomSpawn.AirshipSpawn(PlayerControl.LocalPlayer);
            // GM skips gushing screen
            return false;
        }
        return true;
    }
}
