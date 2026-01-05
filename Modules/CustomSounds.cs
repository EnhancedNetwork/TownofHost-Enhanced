using Hazel;
using System;
using System.IO;
using TOHE.Modules.Rpc;

#if !ANDROID
using System.Runtime.InteropServices;
#else
using UnityEngine;
#endif

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

#if ANDROID
    private static readonly string SOUNDS_PATH = Path.Combine(UnityEngine.Application.persistentDataPath, "TOHE-DATA", "resources");
#else
    private static readonly string SOUNDS_PATH = Path.Combine(Environment.CurrentDirectory, "BepInEx", "resources");
#endif

    public static void Play(string sound)
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

        StartPlay(path);
        Logger.Msg($"play sound：{sound}", "CustomSounds");
    }

#if !ANDROID
    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    private static extern bool PlaySound(string Filename, int Mod, int Flags);
    
    private static void StartPlay(string path) => PlaySound(path, 0, 1);
#else
    private static void StartPlay(string path)
    {
        try
        {
            // 在 Android 上使用 Unity 的 AudioSource 播放音频
            var audioClip = LoadAudioClip(path);
            if (audioClip != null)
            {
                // 创建临时的 AudioSource 来播放音频
                var gameObject = new GameObject("TempAudioSource");
                var audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.clip = audioClip;
                audioSource.volume = 1.0f;
                audioSource.Play();

                // 播放完成后销毁对象
                UnityEngine.Object.Destroy(gameObject, audioClip.length);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to play sound on Android: {ex.Message}", "CustomSounds.Android");
        }
    }

    private static AudioClip LoadAudioClip(string path)
    {
        try
        {
            // 使用 Unity 的 UnityWebRequestMultimedia 加载音频文件
            var www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip($"file://{path}", AudioType.WAV);
            www.SendWebRequest();

            // 等待加载完成（简化处理，生产环境应该使用异步）
            while (!www.isDone) { }

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                return UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
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
#endif
}
