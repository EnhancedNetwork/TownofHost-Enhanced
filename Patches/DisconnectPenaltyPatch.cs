using AmongUs.Data.Player;

namespace TOHE.Patches
{
    [HarmonyPatch(typeof(PlayerBanData), nameof(PlayerBanData.IsBanned), MethodType.Getter)]
    public static class DisconnectPenaltyPatch
    {
        public static bool Prefix(PlayerBanData __instance, ref bool __result)
        {
            __instance.BanPoints = 0f;
            __result = false;
            return false;
        }
    }
}
