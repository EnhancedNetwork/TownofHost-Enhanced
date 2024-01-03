using System.Collections.Generic;
using AmongUs.GameOptions;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using InnerNet;

namespace TOHE.Modules;

public abstract class GameOptionsSender
{
    #region Static
    public readonly static List<GameOptionsSender> AllSenders = new(15) { new NormalGameOptionsSender() };

    public static void SendAllGameOptions()
    {
        AllSenders.RemoveAll(s => !s.AmValid()); // .AmValid() has a virtual property, so it doesn't always return true
        var array = AllSenders.ToArray();
        foreach (GameOptionsSender sender in array)
        {
            if (sender.IsDirty) sender.SendGameOptions();
            sender.IsDirty = false;
        }
    }
    #endregion

    public abstract IGameOptions BasedGameOptions { get; }
    public abstract bool IsDirty { get; protected set; }


    public virtual void SendGameOptions()
    {
        var opt = BuildGameOptions();

        // option => byte[]
        MessageWriter writer = MessageWriter.Get(SendOption.None);
        writer.Write(opt.Version);
        writer.StartMessage(0);
        writer.Write((byte)opt.GameMode);
        if (opt.TryCast<NormalGameOptionsV07>(out var normalOpt))
            NormalGameOptionsV07.Serialize(writer, normalOpt);
        else if (opt.TryCast<HideNSeekGameOptionsV07>(out var hnsOpt))
            HideNSeekGameOptionsV07.Serialize(writer, hnsOpt);
        else
        {
            writer.Recycle();
            Logger.Error("Option Cast Failed", this.ToString());
        }
        writer.EndMessage();

        // Create into array
        var byteArray = new Il2CppStructArray<byte>(writer.Length - 1);
        // MessageWriter.ToByteArray
        Buffer.BlockCopy(writer.Buffer.Cast<Array>(), 1, byteArray.Cast<Array>(), 0, writer.Length - 1);

        SendOptionsArray(byteArray);
        writer.Recycle();
    }
    public virtual void SendOptionsArray(Il2CppStructArray<byte> optionArray)
    {
        try
        {
            for (byte i = 0; i < GameManager.Instance.LogicComponents.Count; i++)
            {
                if (GameManager.Instance.LogicComponents[i].TryCast<LogicOptions>(out _))
                {
                    SendOptionsArray(optionArray, i, -1);
                }
            }
        }
        catch (System.Exception error)
        {
            Logger.Fatal(error.ToString(), "GameOptionsSender.SendOptionsArray");
        }
    }
    protected virtual void SendOptionsArray(Il2CppStructArray<byte> optionArray, byte LogicOptionsIndex, int targetClientId)
    {
        var writer = MessageWriter.Get(SendOption.Reliable);

        writer.StartMessage(targetClientId == -1 ? Tags.GameData : Tags.GameDataTo);
        {
            writer.Write(AmongUsClient.Instance.GameId);
            if (targetClientId != -1) writer.WritePacked(targetClientId);
            writer.StartMessage(1);
            {
                writer.WritePacked(GameManager.Instance.NetId);
                writer.StartMessage(LogicOptionsIndex);
                {
                    writer.WriteBytesAndSize(optionArray);
                }
                writer.EndMessage();
            }
            writer.EndMessage();
        }
        writer.EndMessage();

        AmongUsClient.Instance.SendOrDisconnect(writer);
        writer.Recycle();
    }
    public abstract IGameOptions BuildGameOptions();

    public virtual bool AmValid() => true;
}