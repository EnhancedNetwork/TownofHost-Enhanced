using HarmonyLib;
using TOHE.Modules;

namespace TOHE.Patches;

[HarmonyPatch(typeof(VoteBanSystem), nameof(VoteBanSystem.AddVote))]
internal class VoteBanSystemPatch
{
    public static bool Prefix(/*VoteBanSystem __instance, */[HarmonyArgument(0)] int srcClient, [HarmonyArgument(1)] int clientId)
    {
        if (!AmongUsClient.Instance.AmHost) return true;

        var VoterClient = AmongUsClient.Instance.GetClient(srcClient);
        var ClientData = AmongUsClient.Instance.GetClient(clientId);
        var VoterPc = VoterClient.Character;
        var ClientPc = ClientData.Character;

        if (Options.DisableVoteBan.GetBool())
        {
        return false;
        }

        if (VoterPc == null || ClientPc == null)
        {
            Logger.Info($"Client {srcClient} add kick vote for Target {clientId}, but one of them has null PlayerControl", "VoteBanSystem");
            return false;
        }

        Logger.Info($"Client {srcClient} - Player {VoterPc.GetNameWithRole()} add kick vote for Client {clientId} - TargetPc {ClientPc.GetNameWithRole()}", "VoteBanSystem");
        if (ClientPc.Data.Disconnected)
        {
            Logger.Info($"Canceled because target disconnected", "VoteBanSystem");
            return false;
        }

        if (DevManager.GetDevUser(ClientPc.FriendCode).DeBug && ClientPc.AmOwner)
        {
            Logger.Info($"Canceled because target is dev host", "VoteBanSystem");
            return false; //If the host has debug permission, Prevent host from being vote kicked
        } //wont function if target is debug but not host for security reasons

        return true;
    }
}
