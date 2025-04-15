using System;
using UnityEngine;

namespace TOHE.Modules;

// https://github.com/tukasa0001/TownOfHost/blob/main/Modules/VersionChecker.cs
public static class VersionChecker
{
    public static bool IsSupported { get; private set; } = true;
    private static bool Ischecked = false;

    public static void Check()
    {
        if (Ischecked) return;

        var amongUsVersion = Constants.GetVersion(Constants.Year, Constants.Month, Constants.Day, Constants.Revision);
        Logger.Info($"AU: {amongUsVersion}", "Among Us Version Check");

        foreach (var version in Main.SupportedVersionAU)
        {
            if (Constants.GetVersion(version.year, version.month, version.day, version.revision) == amongUsVersion)
            {
                IsSupported = true;
                break;
            }
        }

        if (!IsSupported)
        {
            ErrorText.Instance.AddError(ErrorCode.UnsupportedVersion);
        }

        Ischecked = true;
    }
}
