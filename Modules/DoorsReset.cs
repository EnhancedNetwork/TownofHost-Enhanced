namespace TOHE.Modules;

public static class DoorsReset
{
    private static bool isEnabled = false;
    private static ResetMode mode;
    private static DoorsSystemType DoorsSystem => ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Doors, out var system) ? system.TryCast<DoorsSystemType>() : null;
    private static readonly LogHandler logger = Logger.Handler(nameof(DoorsReset));

    public static void Initialize()
    {
        if (GameStates.IsHideNSeek)
        {
            isEnabled = false;
            return;
        }
        // Not supported except Airship, Polus and Fungle
        if ((Utils.GetActiveMapName() is not (MapNames.Airship or MapNames.Polus or MapNames.Fungle)) || Options.DisableCloseDoor.GetBool())
        {
            isEnabled = false;
            return;
        }
        isEnabled = Options.ResetDoorsEveryTurns.GetBool();
        mode = (ResetMode)Options.DoorsResetMode.GetValue();
        Logger.Info($"initialization: [ {isEnabled}, {mode} ]", "Reset Doors");
    }

    /// <summary>Reset door status according to settings</summary>
    public static void ResetDoors()
    {
        if (!isEnabled || DoorsSystem == null)
        {
            return;
        }
        Logger.Info("Reset", "Reset Doors");

        switch (mode)
        {
            case ResetMode.AllOpen: OpenAllDoors(); break;
            case ResetMode.AllClosed: CloseAllDoors(); break;
            case ResetMode.RandomByDoor: OpenOrCloseAllDoorsRandomly(); break;
            default: Logger.Warn($"Invalid Reset Doors Mode: {mode}", "Reset Doors"); break;
        }
    }
    /// <summary>Open all doors on the map</summary>
    private static void OpenAllDoors()
    {
        foreach (var door in ShipStatus.Instance.AllDoors)
        {
            SetDoorOpenState(door, true);
        }
        DoorsSystem.IsDirty = true;
    }
    /// <summary>Close all doors on the map</summary>
    private static void CloseAllDoors()
    {
        foreach (var door in ShipStatus.Instance.AllDoors)
        {
            SetDoorOpenState(door, false);
        }
        DoorsSystem.IsDirty = true;
    }
    /// <summary>Randomly opens and closes all doors on the map</summary>
    private static void OpenOrCloseAllDoorsRandomly()
    {
        foreach (var door in ShipStatus.Instance.AllDoors)
        {
            var isOpen = IRandom.Instance.Next(2) > 0;
            SetDoorOpenState(door, isOpen);
        }
        DoorsSystem.IsDirty = true;
    }

    /// <summary>Sets the open/close status of the door. Do nothing for doors that cannot be closed by sabotage</summary>
    /// <param name="door">Target door</param>
    /// <param name="isOpen">true for open, false for close</param>
    private static void SetDoorOpenState(OpenableDoor door, bool isOpen)
    {
        if (IsValidDoor(door))
        {
            door.SetDoorway(isOpen);
        }
    }
    /// <summary>Determine if the door is subject to reset</summary>
    /// <returns>true if it is subject to reset</returns>
    private static bool IsValidDoor(OpenableDoor door)
    {
        // Airship lounge toilets and Polus decontamination room doors are not closed
        return door.Room is not (SystemTypes.Lounge or SystemTypes.Decontamination);
    }

    public enum ResetMode { AllOpen, AllClosed, RandomByDoor, }
}
