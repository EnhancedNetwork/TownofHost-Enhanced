using Hazel;
using System;
using System.IO;
using TOHE.Modules.Rpc;
using UnityEngine;

namespace TOHE.Modules;

public static class CustomSoundsManager
{
    public static void RPCPlayCustomSound(this PlayerControl pc, string sound, float volume = 1f, float pitch = 1f, bool force = false)
    {
        if (!force && (!AmongUsClient.Instance.AmHost || !pc.IsModded())) return;
        if (pc == null || PlayerControl.LocalPlayer.PlayerId == pc.PlayerId)
        {
            Play(sound);
            return;
        }
        RpcUtils.LateSpecificSendMessage(new RpcPlayCustomSound(pc.NetId, sound, volume, pitch), pc.GetClientId());
    }

    public static void RPCPlayCustomSoundAll(string sound, float volume = 1f, float pitch = 1f)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        RpcUtils.LateBroadcastReliableMessage(new RpcPlayCustomSound(PlayerControl.LocalPlayer.NetId, sound, volume, pitch));
        Play(sound);
    }

    public static void ReceiveRPC(MessageReader reader) => Play(reader.ReadString(), reader.ReadSingle(), reader.ReadSingle());

    private static readonly string SOUNDS_PATH = OperatingSystem.IsAndroid() ? Path.Combine(UnityEngine.Application.persistentDataPath, "TOHE-DATA", "resources") : Path.Combine(Environment.CurrentDirectory, "BepInEx", "resources");

    public static void Play(string sound, float volume = 1f, float pitch = 1f)
    {
        if (!Constants.ShouldPlaySfx() || !Main.EnableCustomSoundEffect.Value) return;

        var path = Path.Combine(SOUNDS_PATH, sound + ".wav");

        if (!Directory.Exists(SOUNDS_PATH))
            Directory.CreateDirectory(SOUNDS_PATH);

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

        StartPlay(path, volume, pitch);
        Logger.Msg($"play sound：{sound}", "CustomSounds");
    }

    private static readonly Dictionary<string, AudioClip> audioCache = [];

    private static void StartPlay(string path, float volume = 1f, float pitch = 1f)
    {
        try
        {
            var audioClip = LoadAudioClip(path);
            if (audioClip != null)
            {
                SoundManager.Instance.PlaySoundImmediate(audioClip, false, volume, pitch);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to play sound: {ex.Message}", "CustomSounds");
        }
    }

    private static AudioClip LoadAudioClip(string path)
    {
        if (audioCache.ContainsKey(path)) return audioCache[path];

        try
        {
            // 使用 Unity 的 UnityWebRequestMultimedia 加载音频文件
            var www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip($"file://{path}", AudioType.WAV);
            www.SendWebRequest();

            // 等待加载完成（简化处理，生产环境应该使用异步）
            while (!www.isDone) { }

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                audioCache[path] = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                return audioCache[path];
            }
            else
            {
                Logger.Error($"Failed to load audio clip: {www.error}", "CustomSounds.LoadAudioClip");
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Exception loading audio clip: {ex.Message}", "CustomSounds.LoadAudioClip");
            return null;
        }
    }
}
