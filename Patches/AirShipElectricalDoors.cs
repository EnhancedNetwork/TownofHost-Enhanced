namespace TOHE;

public class AirshipElectricalDoors
{
    private static ElectricalDoors Instance
        => ShipStatus.Instance.Systems[SystemTypes.Decontamination].CastFast<ElectricalDoors>();

    public static void Initialize()
    {
        if (!GameStates.AirshipIsActive) return;
        Instance.Initialize();
    }
    public static byte[] GetClosedDoors()
    {
        List<byte> DoorsArray = [];
        if (Instance.Doors == null || Instance.Doors.Count == 0) return [.. DoorsArray];
        for (byte i = 0; i < Instance.Doors.Count; i++)
        {
            var door = Instance.Doors[i];
            if (door != null && !door.IsOpen)
                DoorsArray.Add(i);
        }
        return DoorsArray?.ToArray();
    }
    // 0: BottomRightHort
    // 1: BottomHort
    // 2: TopRightHort
    // 3: TopCenterHort
    // 4: TopLeftHort
    // 5: LeftVert
    // 6: RightVert
    // 7: TopRightVert
    // 8: TopLeftVert
    // 9: BottomRightVert
    // 10: LeftDoorTop
    // 11: LeftDoorBottom
}
[HarmonyPatch(typeof(ElectricalDoors), nameof(ElectricalDoors.Initialize))]
class ElectricalDoorsInitializePatch
{
    public static void Postfix(/*ElectricalDoors __instance*/)
    {
        if (!GameStates.IsInGame) return;
        var closedoors = "";
        bool isFirst = true;
        foreach (var num in AirshipElectricalDoors.GetClosedDoors())
        {
            if (isFirst)
            {
                isFirst = false;
                closedoors += num.ToString();
            }
            else
                closedoors += $", {num}";
        }
        Logger.Info($"ClosedDoors:{closedoors}", "ElectricalDoors Initialize");
    }
}
