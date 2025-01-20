namespace TOHE.Patches;

[HarmonyPatch(typeof(VoteBanSystem), nameof(VoteBanSystem.AddVote))]
internal class VoteBanSystemPatch
{
    public static bool Prefix(/*VoteBanSystem __instance, */[HarmonyArgument(0)] int srcClient, [HarmonyArgument(1)] int clientId)
    {
        if (!AmongUsClient.Instance.AmHost) return true;

        return false;
    }
}
