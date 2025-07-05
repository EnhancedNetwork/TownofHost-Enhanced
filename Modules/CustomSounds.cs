using Hazel;
using System;
using System.IO;
using System.Runtime.InteropServices;
using TOHE.Modules.Rpc;

namespace TOHE.Modules;

public static class CustomSoundsManager
{
    public static void RPCPlayCustomSound(this PlayerControl pc, string sound, bool force = false)
    {
        if (!force) if (!AmongUsClient.Instance.AmHost || !pc.IsModded()) return;
        if (pc == null || PlayerControl.LocalPlayer.PlayerId == pc.PlayerId)
        {
            Play(sound);
            return;
        }
        RpcUtils.LateSpecificSendMessage(new RpcPlayCustomSound(pc.NetId, sound), pc.GetClientId());
    }
    public static void RPCPlayCustomSoundAll(string sound)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        RpcUtils.LateBroadcastReliableMessage(new RpcPlayCustomSound(PlayerControl.LocalPlayer.NetId, sound));
        Play(sound);
    }
    public static void ReceiveRPC(MessageReader reader) => Play(reader.ReadString());


    private static readonly string SOUNDS_PATH = @$"{Environment.CurrentDirectory.Replace(@"\", "/")}/BepInEx/resources/";
    public static void Play(string sound)
    {
        if (!Constants.ShouldPlaySfx() || !Main.EnableCustomSoundEffect.Value) return;
        var path = SOUNDS_PATH + sound + ".wav";
        if (!Directory.Exists(SOUNDS_PATH)) Directory.CreateDirectory(SOUNDS_PATH);
        DirectoryInfo folder = new(SOUNDS_PATH);
        if ((folder.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
            folder.Attributes = FileAttributes.Hidden;
        if (!File.Exists(path))
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TOHE.Resources.Sounds." + sound + ".wav");
            if (stream == null)
            {
                Logger.Warn($"Sound file missing：{sound}", "CustomSounds");
                return;
            }
            var fs = File.Create(path);
            stream.CopyTo(fs);
            fs.Close();
        }
        StartPlay(path);
        Logger.Msg($"play sound：{sound}", "CustomSounds");
    }

    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    private static extern bool PlaySound(string Filename, int Mod, int Flags);
    private static void StartPlay(string path) => PlaySound(@$"{path}", 0, 1); //第3个形参，把1换为9，连续播放

}
