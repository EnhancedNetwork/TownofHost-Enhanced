using UnityEngine;

namespace TOHE;

class PlayerNameColorPatch
{
    [HarmonyPatch(typeof(PlayerNameColor), nameof(PlayerNameColor.Get))]
    [HarmonyPatch([typeof(NetworkedPlayerInfo)])]
    public class GetPatch()
    {
        public static bool Prefix(ref Color __result)
        {
            __result = Color.white;
            return false;
        }
    }
}