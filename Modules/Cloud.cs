using HarmonyLib;
using System;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace TOHE;

internal class Cloud
{
    private static string IP;
    //private static int LOBBY_PORT = 0;
    private static int EAC_PORT = 0;
    //private static Socket ClientSocket;
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
    /*public static bool ShareLobby(bool command = false)
    {
        try
        {
            if (!Options.ShareLobby.GetBool() && !command) return false;
            if (!Main.newLobby || (GameData.Instance.PlayerCount < Options.ShareLobbyMinPlayer.GetInt() && !command) || !GameStates.IsLobby) return false;
            if (!AmongUsClient.Instance.AmHost || !GameData.Instance || AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame) return false;

            if (IP == null || LOBBY_PORT == 0) throw new("Has no ip or port");

            Main.newLobby = false;
            string msg = $"{GameStartManager.Instance.GameRoomNameCode.text}|{Main.PluginVersion}|{GameData.Instance.PlayerCount + 1}|{TranslationController.Instance.currentLanguage.languageID}|{ServerManager.Instance.CurrentRegion.Name}|{DataManager.player.customization.name}";

            if (msg.Length <= 60)
            {
                byte[] buffer = Encoding.Default.GetBytes(msg);
                ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                ClientSocket.Connect(IP, LOBBY_PORT);
                ClientSocket.Send(buffer);
                ClientSocket.Close();
            }

            Utils.SendMessage(Translator.GetString("Message.LobbyShared"), PlayerControl.LocalPlayer.PlayerId);

        }
        catch (Exception e)
        {
            Utils.SendMessage(Translator.GetString("Message.LobbyShareFailed"), PlayerControl.LocalPlayer.PlayerId);
            Logger.Exception(e, "SentLobbyToQQ");
            throw;
        }
        return true;
    }*/

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
        public static void Postfix()
        {
            if (LastRepotTimeStamp != 0 && LastRepotTimeStamp + 8 < Utils.GetTimeStamp())
            {
                LastRepotTimeStamp = 0;
                StopConnect();
                Logger.Warn("超时自动断开与TOHE服务器的连接", "EAC Cloud");
            }
        }
    }
}