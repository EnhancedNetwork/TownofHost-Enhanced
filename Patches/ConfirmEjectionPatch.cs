using System;

namespace TOHE.Patches;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
class ConfirmEjectionMeetingClose
{
    public static void Prefix()
    {
        var playerData = Main.LastVotedPlayerInfo;
        if (!AmongUsClient.Instance.AmHost || playerData == null) return;

        Logger.Warn("Test", "ExileController.BeginForGameplay");

        if (GameStates.IsInGame)
        {
            try
            {
                playerData.UpdateName("Name For Testing Confirm Ejection Patch <size=0%>", Utils.GetClientById(playerData.ClientId));
                playerData.Object?.RpcSetName("Name For Testing Confirm Ejection Patch <size=0%>");
            }
            catch (Exception error)
            {
                Logger.Error($"Error after change exiled player name: {error}", "ConfirmEjection.MeetingClose");
            }
        }
    }
}

[HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
class ConfirmEjectionReturnPlayerName
{
    public static void Prefix()
    {
        var playerData = Main.LastVotedPlayerInfo;
        if (!AmongUsClient.Instance.AmHost || playerData == null) return;

        var realName = Main.LastVotedPlayer;

        if (GameStates.IsInGame)
        {
            try
            {
                var client = Utils.GetClientById(playerData.ClientId);
                playerData.UpdateName(realName, client);
                
                if (!playerData.Disconnected)
                {
                    playerData.Object?.RpcSetName(realName);
                }
            }
            catch (Exception error)
            {
                Logger.Error($"Error after change exiled player name back: {error}", "ConfirmEjection.ReturnPlayerName");
            }
        }
    }
}
