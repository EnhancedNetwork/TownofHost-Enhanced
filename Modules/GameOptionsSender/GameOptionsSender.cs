using AmongUs.GameOptions;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using TOHE.Modules.Rpc;

namespace TOHE.Modules;

public abstract class GameOptionsSender
{
    #region Static
    public readonly static List<GameOptionsSender> AllSenders = new(100) { new NormalGameOptionsSender() };

    public static void SendAllGameOptions()
    {
        AllSenders.RemoveAll(s => !s.AmValid()); // .AmValid() has a virtual property, so it doesn't always return true
        var AllSendersArray = AllSenders.ToArray();
        foreach (GameOptionsSender sender in AllSendersArray)
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
        if (!AmongUsClient.Instance.AmHost) return;

        var opt = BuildGameOptions();
        var currentGameMode = AprilFoolsMode.IsAprilFoolsModeToggledOn //April fools mode toggled on by host
            ? opt.AprilFoolsOnMode : opt.GameMode; //Change game mode, same as well as in "RpcSyncSettings()"

        // option => byte[]
        MessageWriter writer = MessageWriter.Get(SendOption.None);
        writer.Write(opt.Version);
        writer.StartMessage(0);
        writer.Write((byte)currentGameMode);
        if (opt.TryCast<NormalGameOptionsV09>(out var normalOpt))
            NormalGameOptionsV09.Serialize(writer, normalOpt);
        else if (opt.TryCast<HideNSeekGameOptionsV09>(out var hnsOpt))
            HideNSeekGameOptionsV09.Serialize(writer, hnsOpt);
        else
        {
            writer.Recycle();
            Logger.Error("Option Cast Failed", this.ToString());
        }
        writer.EndMessage();

        // Create into array
        var byteArray = new Il2CppStructArray<byte>(writer.Length - 1);
        // MessageWriter.ToByteArray
        Buffer.BlockCopy(writer.Buffer.CastFast<Array>(), 1, byteArray.CastFast<Array>(), 0, writer.Length - 1);

        SendOptionsArray(byteArray);
        writer.Recycle();
    }
    public virtual void SendOptionsArray(Il2CppStructArray<byte> optionArray)
    {
        try
        {
            byte logicOptionsIndex = 0;
            foreach (var logicComponent in GameManager.Instance.LogicComponents.GetFastEnumerator())
            {
                if (logicComponent.CastFast<LogicOptions>() != null)
                {
                    SendOptionsArray(optionArray, logicOptionsIndex, -1);
                }
                logicOptionsIndex++;
            }
        }
        catch (System.Exception error)
        {
            Logger.Fatal(error.ToString(), "GameOptionsSender.SendOptionsArray");
        }
    }
    protected virtual void SendOptionsArray(Il2CppStructArray<byte> optionArray, byte LogicOptionsIndex, int targetClientId)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        var message = new SendOptionsArray(optionArray);

        if (targetClientId < 0)
        {
            RpcUtils.LateBroadcastReliableMessage(message);
        }
        else
        {
            RpcUtils.LateSpecificSendMessage(message, targetClientId);
        }
    }
    public abstract IGameOptions BuildGameOptions();

    public virtual bool AmValid() => true;
}
