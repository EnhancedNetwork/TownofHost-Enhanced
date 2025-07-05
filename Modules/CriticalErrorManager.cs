using Hazel;
using TOHE.Modules.Rpc;
using static TOHE.Translator;

namespace TOHE.Modules;

public static class CriticalErrorManager
{
    private static bool IsError = false;
    private static bool ErrorFromRpc = false;
    private static byte ModdedPlayerId = byte.MinValue;

    public static void Initialize()
    {
        IsError = false;
        ErrorFromRpc = false;
        ModdedPlayerId = byte.MinValue;
    }
    public static void SetCriticalError(string reason, bool whileLoading, string sourseError = "")
    {
        Logger.Fatal($"Error: {reason} - triggered critical error", "Anti-black");
        IsError = true;

        if (!whileLoading) return;

        if (AmongUsClient.Instance.AmHost)
        {
            ChatUpdatePatch.DoBlockChat = true;
            Main.OverrideWelcomeMsg = GetString("AntiBlackOutNotifyInLobby");
        }
        else
        {
            var msg = new RpcAntiBlackout(PlayerControl.LocalPlayer.NetId, PlayerControl.LocalPlayer.PlayerId, reason, sourseError);
            RpcUtils.LateBroadcastReliableMessage(msg);
        }
    }
    public static void ReadRpc(PlayerControl player, MessageReader reader)
    {
        IsError = true;
        ErrorFromRpc = true;
        ModdedPlayerId = reader.ReadByte();

        Logger.Fatal($"Modded client: {player?.Data?.PlayerName}({ModdedPlayerId}): triggered critical error: {reader.ReadString()}", "Anti-black");
        Logger.Error($"Error: {reader.ReadString()}", "CriticalErrorManager");

        ChatUpdatePatch.DoBlockChat = true;
        Main.OverrideWelcomeMsg = string.Format(GetString("RpcAntiBlackOutNotifyInLobby"), player?.Data?.PlayerName, GetString("EndWhenPlayerBug"));
    }
    public static void CheckEndGame()
    {
        if (!IsError) return;

        if (AmongUsClient.Instance.AmHost)
        {
            if (ErrorFromRpc)
            {
                var player = ModdedPlayerId.GetPlayer();

                if (GameStates.IsShip || !GameStates.IsLobby || GameStates.IsCoStartGame)
                {
                    if (Options.EndWhenPlayerBug.GetBool())
                    {
                        ChatUpdatePatch.DoBlockChat = true;
                        Main.OverrideWelcomeMsg = string.Format(GetString("RpcAntiBlackOutNotifyInLobby"), player?.Data?.PlayerName, GetString("EndWhenPlayerBug"));

                        Logger.SendInGame(string.Format(GetString("RpcAntiBlackOutEndGame"), player?.Data?.PlayerName));

                        if (GameStates.IsInGame && !GameStates.IsCoStartGame)
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Error);
                            GameManager.Instance.LogicFlow.CheckEndCriteria();
                            RPC.ForceEndGame(CustomWinner.Error);
                        }
                        else
                        {
                            _ = new LateTask(() =>
                            {
                                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Error);
                                GameManager.Instance.LogicFlow.CheckEndCriteria();
                                RPC.ForceEndGame(CustomWinner.Error);
                            }, 1.5f, "RPC Anti-Black End Game As Critical Error");
                        }
                    }
                    else
                    {
                        Logger.SendInGame(string.Format(GetString("RpcAntiBlackOutIgnored"), player?.Data?.PlayerName));

                        if (player != null)
                        {
                            if (GameStates.IsInGame && !GameStates.IsCoStartGame)
                            {
                                AmongUsClient.Instance.KickPlayer(player.GetClientId(), false);
                                Logger.SendInGame(string.Format(GetString("RpcAntiBlackOutKicked"), player?.Data?.PlayerName));
                            }
                            else
                            {
                                _ = new LateTask(() =>
                                {
                                    if (player == null) return;
                                    AmongUsClient.Instance.KickPlayer(player.GetClientId(), false);
                                    Logger.SendInGame(string.Format(GetString("RpcAntiBlackOutKicked"), player?.Data?.PlayerName));
                                }, 0.5f, "RPC Anti-Black Kicked As Critical Error");
                            }

                            ChatUpdatePatch.DoBlockChat = false;
                        }
                    }
                }
                else if (GameStartManager.Instance != null)
                {
                    // We imagine rpc is received when starting game in lobby, not fucked yet
                    if (AmongUsClient.Instance.AmHost)
                    {
                        GameStartManager.Instance.ResetStartState();
                        if (player != null)
                        {
                            AmongUsClient.Instance.KickPlayer(player.GetClientId(), false);
                        }
                    }
                    Logger.SendInGame(string.Format(GetString("RpcAntiBlackOutKicked"), player?.Data?.PlayerName));
                }
                else
                {
                    Logger.SendInGame("[Critical Error] Your client is in a unknow state while receiving AntiBlackOut rpcs from others");
                    Logger.Fatal($"Client is in a unknow state while receiving AntiBlackOut rpcs from others", "Anti-black");
                }
            }
            else
            {
                Logger.SendInGame(GetString("AntiBlackOutLoggerSendInGame"));

                if (GameStates.IsShip || !GameStates.IsLobby || GameStates.IsCoStartGame)
                {
                    _ = new LateTask(() =>
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Error);
                        GameManager.Instance.LogicFlow.CheckEndCriteria();
                        RPC.ForceEndGame(CustomWinner.Error);
                    }, 0.5f, "Anti-Black End Game As Critical Error");
                }
                else if (GameStartManager.Instance != null)
                {
                    GameStartManager.Instance.ResetStartState();
                    AmongUsClient.Instance.RemoveUnownedObjects();
                    Logger.SendInGame(GetString("AntiBlackOutLoggerSendInGame"));
                }
                else
                {
                    Logger.SendInGame("Host in a unknow antiblack bugged state");
                    Logger.Fatal($"Host in a unknow antiblack bugged state", "Anti-black");
                }
            }
        }
        else
        {
            if (Options.EndWhenPlayerBug.GetBool())
            {
                Logger.SendInGame(GetString("AntiBlackOutRequestHostToForceEnd"));
            }
            else
            {
                Logger.SendInGame(GetString("AntiBlackOutHostRejectForceEnd"));

                _ = new LateTask(() =>
                {
                    if (AmongUsClient.Instance.AmConnected)
                    {
                        AmongUsClient.Instance.ExitGame(DisconnectReasons.Custom);
                        Logger.Fatal($"Disconnected from the game due critical error", "Anti-black");
                    }
                }, 1.5f, "Anti-Black Exit Game Due Critical Error");
            }
        }
        IsError = false;
        ErrorFromRpc = false;
        ModdedPlayerId = byte.MinValue;
    }
}
