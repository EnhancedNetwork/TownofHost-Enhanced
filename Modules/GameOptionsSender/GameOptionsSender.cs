using System.Collections;
using System.Diagnostics;
using AmongUs.GameOptions;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using InnerNet;
using TOHE.Modules.Rpc;
using UnityEngine;

namespace TOHE.Modules;

public abstract class GameOptionsSender
{
    protected abstract bool IsDirty { get; set; }

    private Il2CppStructArray<byte> BuildOptionArray()
    {
        IGameOptions opt = BuildGameOptions();
        var currentGameMode = AprilFoolsMode.IsAprilFoolsModeToggledOn ? opt.AprilFoolsOnMode : opt.GameMode;

        // option => byte[]
        MessageWriter writer = MessageWriter.Get();
        writer.Write(opt.Version);
        writer.StartMessage(0);
        writer.Write((byte)currentGameMode);

        if (opt.TryCast(out NormalGameOptionsV10 normalOpt))
            NormalGameOptionsV10.Serialize(writer, normalOpt);
        else if (opt.TryCast(out HideNSeekGameOptionsV10 hnsOpt))
            HideNSeekGameOptionsV10.Serialize(writer, hnsOpt);
        else
            Logger.Error("Option cast failed", ToString());

        writer.EndMessage();

        Il2CppStructArray<byte> optionArray = writer.ToByteArray(false);
        writer.Recycle();
        return optionArray;
    }

    protected virtual void SendGameOptions()
    {
        Il2CppStructArray<byte> optionArray = BuildOptionArray();
        SendOptionsArray(optionArray);
    }

    protected virtual IEnumerator SendGameOptionsAsync()
    {
        Il2CppStructArray<byte> optionArray = BuildOptionArray();
        yield return SendOptionsArrayAsync(optionArray);
    }

    private void SendOptionsArray(Il2CppStructArray<byte> optionArray)
    {
        int count = GameManager.Instance.LogicComponents.Count;

        for (byte i = 0; i < count; i++)
        {
            Il2CppSystem.Object logicComponent = GameManager.Instance.LogicComponents[i];
            if (logicComponent.TryCast<LogicOptions>(out _)) SendOptionsArray(optionArray, i);
        }
    }

    private IEnumerator SendOptionsArrayAsync(Il2CppStructArray<byte> optionArray)
    {
        int count = GameManager.Instance.LogicComponents.Count;

        for (byte i = 0; i < count; i++)
        {
            Il2CppSystem.Object logicComponent = GameManager.Instance.LogicComponents[i];
            if (logicComponent.TryCast<LogicOptions>(out _)) SendOptionsArray(optionArray, i);
            yield return WaitFrameIfNecessary();
        }
    }

    protected abstract void SendOptionsArray(Il2CppStructArray<byte> optionArray, byte logicOptionsIndex);

    public abstract IGameOptions BuildGameOptions();

    protected virtual bool AmValid()
    {
        return true;
    }

    #region Static

    public static readonly List<GameOptionsSender> AllSenders = [new NormalGameOptionsSender()];

    protected static MessageWriter PackedWriter;
    protected static int PackedWriterMessages;

    public static IEnumerator SendDirtyGameOptionsContinuously()
    {
        try
        {
            while (GameStates.InGame || GameStates.IsLobby)
            {
                if (GameStates.InGame)
                {
                    PackedWriterMessages = 0;
                    PackedWriter = MessageWriter.Get(SendOption.Reliable);
                    PackedWriter.StartMessage(26);
                    PackedWriter.WritePacked(AmongUsClient.Instance.GameId);
                }
                
                for (var index = 0; index < AllSenders.Count; index++)
                {
                    yield return WaitFrameIfNecessary();

                    if (PackedWriter != null && (PackedWriter.Length > 500 || PackedWriterMessages >= AmongUsClient.Instance.GetMaxMessagePackingLimit()))
                    {
                        PackedWriter.EndMessage();
                        var qa = DataFlagRateLimiter.Enqueue(() => AmongUsClient.Instance.SendOrDisconnect(PackedWriter));
                        yield return qa.Wait();
                        PackedWriterMessages = 0;
                        if (qa.Dropped) break;
                        PackedWriter.Clear(SendOption.Reliable);
                        PackedWriter.StartMessage(26);
                        PackedWriter.WritePacked(AmongUsClient.Instance.GameId);
                    }
                    
                    yield return WaitFrameIfNecessary();
                    
                    if (index >= AllSenders.Count) break;
                    GameOptionsSender sender = AllSenders[index];

                    if (sender == null || !sender.AmValid())
                    {
                        AllSenders.RemoveAt(index);
                        index--;
                        continue;
                    }

                    if (sender.IsDirty)
                        yield return sender.SendGameOptionsAsync();

                    sender.IsDirty = false;
                }

                yield return WaitFrameIfNecessary();

                if (PackedWriterMessages > 0 && PackedWriter != null)
                {
                    PackedWriter.EndMessage();
                    yield return DataFlagRateLimiter.Enqueue(() => AmongUsClient.Instance.SendOrDisconnect(PackedWriter)).Wait();
                }

                PackedWriter?.Recycle();
                PackedWriter = null;
                PackedWriterMessages = 0;

                ForceWaitFrame = true;
                yield return WaitFrameIfNecessary();
            }
        }
        finally
        {
            ActiveCoroutine = null;
            PackedWriter?.Recycle();
            PackedWriter = null;
            PackedWriterMessages = 0;
        }
    }

    protected static IEnumerator WaitFrameIfNecessary()
    {
        if (ForceWaitFrame || Stopwatch.ElapsedMilliseconds >= FrameBudget)
        {
            ForceWaitFrame = false;
            Stopwatch.Reset();
            yield return null;
            Stopwatch.Start();
        }
    }

    public static Coroutine ActiveCoroutine;
    private static readonly Stopwatch Stopwatch = new();
    private const int FrameBudget = 3; // in milliseconds
    protected static bool ForceWaitFrame;

    #endregion
}
