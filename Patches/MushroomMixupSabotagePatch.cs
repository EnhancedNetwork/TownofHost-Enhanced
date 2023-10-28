namespace TOHE;

public static class MushroomMixupSabotagePatch
{
    private static bool IsMushroomMixup;

    public static void Init()
    {
        IsMushroomMixup = false;
    }
    public static void CheckMushroomMixup()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        var oldIsMushroomMixup = IsMushroomMixup;

        IsMushroomMixup = Utils.IsActive(SystemTypes.MushroomMixupSabotage);

        if (oldIsMushroomMixup != IsMushroomMixup)
        {
            Utils.NotifyRoles(NoCache: true);
        }
    }
}
