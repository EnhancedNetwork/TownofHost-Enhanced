using Hazel;

namespace TOHE.Modules.Rpc;

public class RpcCheckVanish : BaseModdedRpc
{
    public override byte RpcType => (byte)RpcCalls.CheckVanish;

    public RpcCheckVanish(uint rpcObjectNetId) : base(rpcObjectNetId)
    {
    }

    public override void SerializeRpcValues(MessageWriter msg)
    {
        msg.Write((float)0f);
    }
}

public class RpcCheckAppear : BaseModdedRpc
{
    public override byte RpcType => (byte)RpcCalls.CheckAppear;

    public RpcCheckAppear(uint rpcObjectNetId, bool shouldAnimate) : base(rpcObjectNetId)
    {
        this.shouldAnimate = shouldAnimate;
    }

    public override void SerializeRpcValues(MessageWriter msg)
    {
        msg.Write(shouldAnimate);
    }

    private readonly bool shouldAnimate;
}

public class RpcVanish : BaseModdedRpc
{
    public override byte RpcType => (byte)RpcCalls.StartVanish;
    public RpcVanish(uint rpcObjectNetId) : base(rpcObjectNetId)
    {
    }
    public override void SerializeRpcValues(MessageWriter msg)
    {
    }
}

public class RpcAppear : BaseModdedRpc
{
    public override byte RpcType => (byte)RpcCalls.StartAppear;

    public RpcAppear(uint rpcObjectNetId, bool shouldAnimate) : base(rpcObjectNetId)
    {
        this.shouldAnimate = shouldAnimate;
    }

    public override void SerializeRpcValues(MessageWriter msg)
    {
        msg.Write(shouldAnimate);
    }

    private readonly bool shouldAnimate;
}
