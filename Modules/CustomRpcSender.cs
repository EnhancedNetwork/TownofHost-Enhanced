using AmongUs.GameOptions;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using InnerNet;
using System;
using System.Text;

namespace TOHE;

public class CustomRpcSender
{
    public MessageWriter stream;
    public readonly string name;
    public readonly SendOption sendOption;
    public bool isUnsafe;
    public delegate void onSendDelegateType();
    public onSendDelegateType onSendDelegate;

    private readonly List<MessageWriter> doneStreams = [];

    public State CurrentState
    {
        get { return currentState; }
        set
        {
            if (isUnsafe) currentState = value;
            else Logger.Warn("CurrentStateはisUnsafeがtrueの時のみ上書きできます", "CustomRpcSender");
        }
    }
    private State currentState = State.BeforeInit;

    //0~: targetClientId (GameDataTo)
    //-1: 全プレイヤー (GameData)
    //-2: 未設定
    private int currentRpcTarget;

    private int rootMessageCount;

    private CustomRpcSender() { }
    public CustomRpcSender(string name, SendOption sendOption, bool isUnsafe)
    {
        stream = MessageWriter.Get(sendOption);

        this.name = name;
        this.sendOption = sendOption;
        this.isUnsafe = isUnsafe;
        this.currentRpcTarget = -2;
        onSendDelegate = () => Logger.Info($"{this.name}'s onSendDelegate =>", "CustomRpcSender");

        currentState = State.Ready;
        rootMessageCount = 0;
        Logger.Info($"\"{name}\" is ready", "CustomRpcSender");
    }
    public static CustomRpcSender Create(string name = "No Name Sender", SendOption sendOption = SendOption.None, bool isUnsafe = false)
    {
        return new CustomRpcSender(name, sendOption, isUnsafe);
    }

    #region Start/End Message

    public CustomRpcSender StartMessage(int targetClientId = -1)
    {
        if (currentState != State.Ready)
        {
            var errorMsg = $"Tried to start Message but State is not Ready (in: \"{name}\")";

            if (isUnsafe)
                Logger.Warn(errorMsg, "CustomRpcSender.Warn");
            else
                throw new InvalidOperationException(errorMsg);
        }

        if (stream.Length > 500)
        {
            doneStreams.Add(stream);
            stream = MessageWriter.Get(sendOption);
        }

        if (targetClientId < 0)
        {
            // RPC for everyone
            stream.StartMessage(5);
            stream.Write(AmongUsClient.Instance.GameId);
        }
        else
        {
            // RPC (Desync) to a specific client
            stream.StartMessage(6);
            stream.Write(AmongUsClient.Instance.GameId);
            stream.WritePacked(targetClientId);
        }

        currentRpcTarget = targetClientId;
        currentState = State.InRootMessage;
        return this;
    }

    public CustomRpcSender EndMessage(bool startNew = false)
    {
        if (currentState != State.InRootMessage)
        {
            var errorMsg = $"Tried to exit Message but State is not InRootMessage (in: \"{name}\")";

            if (isUnsafe)
                Logger.Warn(errorMsg, "CustomRpcSender.Warn");
            else
                throw new InvalidOperationException(errorMsg);
        }

        stream.EndMessage();

        if (startNew)
        {
            doneStreams.Add(stream);
            stream = MessageWriter.Get(sendOption);
        }

        currentRpcTarget = -2;
        currentState = State.Ready;
        return this;
    }

    #endregion
    #region Start/End Rpc
    public CustomRpcSender StartRpc(uint targetNetId, RpcCalls rpcCall)
        => StartRpc(targetNetId, (byte)rpcCall);
    public CustomRpcSender StartRpc(
        uint targetNetId,
        byte callId)
    {
        if (currentState != State.InRootMessage)
        {
            string errorMsg = $"RPCを開始しようとしましたが、StateがInRootMessageではありません (in: \"{name}\")";
            if (isUnsafe)
                Logger.Warn(errorMsg, "CustomRpcSender.Warn");
            else
                throw new InvalidOperationException(errorMsg);
        }

        stream.StartMessage(2);
        stream.WritePacked(targetNetId);
        stream.Write(callId);

        currentState = State.InRpc;
        return this;
    }
    public CustomRpcSender EndRpc()
    {
        if (currentState != State.InRpc)
        {
            string errorMsg = $"RPCを終了しようとしましたが、StateがInRpcではありません (in: \"{name}\")";
            if (isUnsafe)
                Logger.Warn(errorMsg, "CustomRpcSender.Warn");
            else
                throw new InvalidOperationException(errorMsg);
        }

        stream.EndMessage();
        currentState = State.InRootMessage;
        return this;
    }
    #endregion
    public CustomRpcSender AutoStartRpc(
        uint targetNetId,
        byte callId,
        int targetClientId = -1)
    {
        if (targetClientId == -2) targetClientId = -1;
        if (currentState is not State.Ready and not State.InRootMessage)
        {
            string errorMsg = $"RPCを自動で開始しようとしましたが、StateがReadyまたはInRootMessageではありません (in: \"{name}\")";
            if (isUnsafe)
                Logger.Warn(errorMsg, "CustomRpcSender.Warn");
            else
                throw new InvalidOperationException(errorMsg);
        }
        if (currentRpcTarget != targetClientId)
        {
            //StartMessage処理
            if (currentState == State.InRootMessage) this.EndMessage();
            this.StartMessage(targetClientId);
        }
        this.StartRpc(targetNetId, callId);

        return this;
    }
    public void SendMessage(bool dispose = false)
    {
        if (currentState == State.InRootMessage) EndMessage();

        if (currentState != State.Ready && !dispose)
        {
            var errorMsg = $"Tried to send RPC but State is not Ready (in: \"{name}\", state: {currentState})";

            if (isUnsafe)
                Logger.Warn(errorMsg, "CustomRpcSender.Warn");
            else
                throw new InvalidOperationException(errorMsg);
        }

        if (stream.Length >= 1500 && sendOption == SendOption.Reliable && !dispose) Logger.Warn($"Large reliable packet \"{name}\" is sending ({stream.Length} bytes)", "CustomRpcSender");
        else if (stream.Length > 3) Logger.Info($"\"{name}\" is finished (Length: {stream.Length}, dispose: {dispose}, sendOption: {sendOption})", "CustomRpcSender");

        if (!dispose)
        {
            if (doneStreams.Count > 0)
            {
                var sb = new StringBuilder(" + Lengths: ");

                doneStreams.ForEach(x =>
                {
                    if (x.Length >= 1500 && sendOption == SendOption.Reliable) Logger.Warn($"Large reliable packet \"{name}\" is sending ({x.Length} bytes)", "CustomRpcSender");
                    else if (x.Length > 3) sb.Append($" | {x.Length}");

                    AmongUsClient.Instance.SendOrDisconnect(x);
                    x.Recycle();
                });

                Logger.Info(sb.ToString(), "CustomRpcSender");

                doneStreams.Clear();
            }

            AmongUsClient.Instance.SendOrDisconnect(stream);
            onSendDelegate();
        }

        currentState = State.Finished;
        stream.Recycle();
    }

    public int Length => stream.Length;

    // Write
    #region PublicWriteMethods
    public CustomRpcSender Write(float val) => Write(w => w.Write(val));
    public CustomRpcSender Write(string val) => Write(w => w.Write(val));
    public CustomRpcSender Write(ulong val) => Write(w => w.Write(val));
    public CustomRpcSender Write(int val) => Write(w => w.Write(val));
    public CustomRpcSender Write(uint val) => Write(w => w.Write(val));
    public CustomRpcSender Write(ushort val) => Write(w => w.Write(val));
    public CustomRpcSender Write(byte val) => Write(w => w.Write(val));
    public CustomRpcSender Write(sbyte val) => Write(w => w.Write(val));
    public CustomRpcSender Write(bool val) => Write(w => w.Write(val));
    public CustomRpcSender Write(Il2CppStructArray<byte> bytes) => Write(w => w.Write(bytes));
    public CustomRpcSender Write(Il2CppStructArray<byte> bytes, int offset, int length) => Write(w => w.Write(bytes, offset, length));
    public CustomRpcSender WriteBytesAndSize(Il2CppStructArray<byte> bytes) => Write(w => w.WriteBytesAndSize(bytes));
    public CustomRpcSender WritePacked(int val) => Write(w => w.WritePacked(val));
    public CustomRpcSender WritePacked(uint val) => Write(w => w.WritePacked(val));
    public CustomRpcSender WriteNetObject(InnerNetObject obj) => Write(w => w.WriteNetObject(obj));
    public CustomRpcSender WriteMessageType(byte val) => Write(w => w.StartMessage(val));
    public CustomRpcSender WriteEndMessage() => Write(w => w.EndMessage());
    #endregion

    private CustomRpcSender Write(Action<MessageWriter> action)
    {
        if (currentState != State.InRpc)
        {
            string errorMsg = $"RPCを書き込もうとしましたが、StateがWrite(書き込み中)ではありません (in: \"{name}\")";
            if (isUnsafe)
                Logger.Warn(errorMsg, "CustomRpcSender.Warn");
            else
                throw new InvalidOperationException(errorMsg);
        }
        action(stream);

        return this;
    }
    [Obfuscation(Exclude = true)]
    public enum State
    {
        BeforeInit = 0, //初期化前 何もできない
        Ready, //送信準備完了 StartMessageとSendMessageを実行可能
        InRootMessage, //StartMessage～EndMessageの間の状態 StartRpcとEndMessageを実行可能
        InRpc, //StartRpc～EndRpcの間の状態 WriteとEndRpcを実行可能
        Finished, //送信後 何もできない
    }
}
public static class CustomRpcSenderExtensions
{
    public static void RpcSetRole(this CustomRpcSender sender, PlayerControl player, RoleTypes role, int targetClientId = -1)
    {
        sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetRole, targetClientId)
            .Write((ushort)role)
            .Write(true) // canOverride
            .EndRpc();
    }

    public static void RpcSetCustomRole(this CustomRpcSender sender, byte playerId, CustomRoles role, int targetClientId = -1)
    {
        sender.AutoStartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, targetClientId)
            .Write(playerId)
            .WritePacked((int)role)
            .EndRpc();
    }

    public static void RpcSetName(this CustomRpcSender sender, PlayerControl player, string name, PlayerControl seer = null)
    {
        bool seerIsNull = seer == null;
        int targetClientId = seerIsNull ? -1 : seer.OwnerId;

        name = name.Replace("color=", string.Empty);

        switch (seerIsNull)
        {
            case true when Main.LastNotifyNames.Where(x => x.Key.Item1 == player.PlayerId).All(x => x.Value == name):
            case false when Main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] == name:
                return;
            case true:
                Main.AllPlayerControls.Do(x => Main.LastNotifyNames[(player.PlayerId, x.PlayerId)] = name);
                break;
            default:
                Main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] = name;
                break;
        }

        sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetName, targetClientId)
            .Write(player.Data.NetId)
            .Write(name)
            .Write(false)
            .EndRpc();
    }
}
