using InnerNet;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Modules;

public class TimeOutStatus
{
    public bool Timing = false;
    public bool RpcReady = false;
    public bool Ready = false;
    public bool Kicking = false;
    public bool Disconnected = false;
    public float timer = -1f;
}

[HarmonyPatch]
public static class PlayerTimeOutManager
{
    public static Dictionary<int, TimeOutStatus> PlayerTimer = [];
    private static float CustomTimeOut => Main.PlayerSpawnTimeOutCooldown.Value;
    public static TimeOutStatus GetTimeOutStatus(this ClientData client)
    {
        if (PlayerTimer.TryGetValue(client.Id, out var status))
        {
            return status;
        }
        return null;
    }

    public static void Init()
    {
        PlayerTimer = [];
    }
    public static bool IsAllReady()
    {
        foreach (var pt in PlayerTimer)
        {
            if (!pt.Value.Ready && !pt.Value.Disconnected)
            {
                return false;
            }
        }

        return true;
    }

    public static void KickAllNotReady()
    {
        foreach (var pt in PlayerTimer)
        {
            if (!pt.Value.Ready && !pt.Value.Disconnected)
            {
                var client = AmongUsClient.Instance.GetClient(pt.Key);

                if (client != null)
                {
                    AmongUsClient.Instance.KickPlayer(pt.Key, false);
                    Logger.SendInGame(GetString("Error.InvalidColor") + $" {client.Id}/{client.PlayerName}");
                }
            }
        }
    }

    public static TimeOutStatus GetTimeOutStatus(this PlayerControl player)
    {
        if (player.GetClient() != null)
        {
            return GetTimeOutStatus(player.GetClient());
        }

        return null;
    }

    public static void OnPlayerJoined(int playerId)
    {
        if (!PlayerTimer.ContainsKey(playerId))
        {
            PlayerTimer.Add(playerId, new TimeOutStatus());
        }
    }

    public static void OnPlayerCreated([HarmonyArgument(0)] ClientData client)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (client.Id == AmongUsClient.Instance.ClientId) return;

        if (PlayerTimer.TryGetValue(client.Id, out var status))
        {
            status.timer = 0f;
            status.Timing = true;
        }
        else
        {
            PlayerTimer.Add(client.Id, new TimeOutStatus());
            PlayerTimer[client.Id].timer = 0f;
            PlayerTimer[client.Id].Timing = true;
        }

        Logger.Info("Client " + client.Id + " begin timming", "TimeOutManager");
    }

    public static void OnPlayerLeft(ClientData client)
    {
        if (PlayerTimer.TryGetValue(client.Id, out var status))
        {
            status.Timing = false;
            status.timer = -1f;
            status.Disconnected = true;
            status.Ready = false;
        }
    }

    public static void OnCheckColorRpc(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        // Host decides whether a client is ready. Also Client should not receive check color
        var status = player.GetTimeOutStatus();
        if (status != null)
        {
            if (!status.RpcReady)
            {
                RPC.RpcVersionCheck();
            }

            status.RpcReady = true;
        }
    }

    public static void OnSetColorRpc(PlayerControl player)
    {
        // Clients receive setcolor from host and is aware that the new player is ready
        if (AmongUsClient.Instance.AmHost) return;

        var status = player.GetTimeOutStatus();
        if (status != null)
        {
            if (!status.RpcReady)
            {
                RPC.RpcVersionCheck();
            }

            status.RpcReady = true;
        }
    }

    public static void OnFixedUpdate()
    {
        var workingList = PlayerTimer.Where(x => x.Value.Timing == true).ToList();
        if (workingList.Any())
        {
            foreach (var pt in workingList)
            {
                if (pt.Value.Disconnected)
                {
                    pt.Value.Timing = false;
                    break;
                }

                pt.Value.timer += Time.fixedDeltaTime;

                if (pt.Value.RpcReady)
                {
                    pt.Value.Timing = false;
                    pt.Value.timer = -1;
                    pt.Value.Ready = true;
                    Logger.Info($"Client {pt.Key} spawn is ready !", "TimeOutManager");
                    break;
                }

                if (pt.Value.timer >= CustomTimeOut)
                {
                    pt.Value.Timing = false;
                    pt.Value.Kicking = true;

                    var client = AmongUsClient.Instance.GetClient(pt.Key);

                    if (client != null)
                    {
                        AmongUsClient.Instance.KickPlayer(pt.Key, false);
                        Logger.SendInGame(GetString("Error.InvalidColor") + $" {client.Id}/{client.PlayerName}");
                    }
                }
            }
        }
    }
}
