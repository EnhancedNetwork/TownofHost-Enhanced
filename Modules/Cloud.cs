using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace TOHE;

internal class Cloud
{
    private static string IP;
    private static int EAC_PORT = 0;
    private static Socket EacClientSocket;
    private static long LastRepotTimeStamp = 0;
    public static void Init()
    {
        try
        {
            var content = GetResourcesTxt("TOHE.Resources.Config.Port.txt");
            string[] ar = content.Split('|');
            IP = ar[0];
            //LOBBY_PORT = int.Parse(ar[1]);
            EAC_PORT = int.Parse(ar[2]);
        }
        catch (Exception e)
        {
            Logger.Exception(e, "Cloud Init");
        }
    }
    private static string GetResourcesTxt(string path)
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
        stream.Position = 0;
        using StreamReader reader = new(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static bool connecting = false;
    public static void StartConnect()
    {
        if (connecting || EacClientSocket != null && EacClientSocket.Connected) return;
        connecting = true;
        _ = new LateTask(() =>
        {
            if (!AmongUsClient.Instance.AmHost || !GameData.Instance || AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame)
            {
                connecting = false;
                return;
            }
            try
            {
                if (IP == null || EAC_PORT == 0) throw new("Has no ip or port");
                LastRepotTimeStamp = Utils.GetTimeStamp();
                EacClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                EacClientSocket.Connect(IP, EAC_PORT);
                Logger.Warn("已连接至TOHE服务器", "EAC Cloud");
            }
            catch (Exception e)
            {
                connecting = false;
                Logger.Exception(e, "EAC Cloud");
                throw;
            }
            connecting = false;
        }, 3.5f, "EAC Cloud Connect");
    }
    public static void StopConnect()
    {
        if (EacClientSocket != null && EacClientSocket.Connected)
            EacClientSocket.Close();
    }
    public static void SendData(string msg)
    {
        StartConnect();
        if (EacClientSocket == null || !EacClientSocket.Connected)
        {
            Logger.Warn("未连接至TOHE服务器，报告被取消", "EAC Cloud");
            return;
        }
        EacClientSocket.Send(Encoding.Default.GetBytes(msg));
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    class EACConnectTimeOut
    {
        public static void Postfix(PlayerControl __instance)
        {
            if (__instance.AmOwner && LastRepotTimeStamp != 0 && LastRepotTimeStamp + 8 < Utils.TimeStamp)
            {
                LastRepotTimeStamp = 0;
                StopConnect();
                Logger.Warn("超时自动断开与TOHE服务器的连接", "EAC Cloud");
            }
        }
    }
}
