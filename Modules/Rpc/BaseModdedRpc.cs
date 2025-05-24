using AmongUs.InnerNet.GameDataMessages;
using Hazel;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using System;

namespace TOHE.Modules.Rpc
{
    public abstract class BaseModdedRpc : BaseGameDataMessage
    {
        // https://github.com/BepInEx/Il2CppInterop/blob/master/Documentation/Class-Injection.md
        // Injecting BaseModdedRpc has a very high chance for the game to crash on load!!!
        // And you need to inject it for all the modded rpc to work!!!
        // Works after injected. No idea how to resolve this problem.
        public BaseModdedRpc(IntPtr ptr) : base(ptr) { }

        public override GameDataTypes GameDataType { get; } = GameDataTypes.RpcFlag;

        [HideFromIl2Cpp]
        public abstract CustomRPC RpcType { get; }

        public BaseModdedRpc(uint rpcObjectNetId) : base(ClassInjector.DerivedConstructorPointer<BaseModdedRpc>())
        {
            ClassInjector.DerivedConstructorBody(this);
            this.rpcObjectNetId = rpcObjectNetId;
        }

        public override void SerializeValues(MessageWriter msg)
        {
            msg.WritePacked(this.rpcObjectNetId);
            msg.Write((byte)this.RpcType);
            SerializeRpcValues(msg);
        }

        [HideFromIl2Cpp]
        public abstract void SerializeRpcValues(MessageWriter msg);

        private readonly uint rpcObjectNetId;
    }
}
