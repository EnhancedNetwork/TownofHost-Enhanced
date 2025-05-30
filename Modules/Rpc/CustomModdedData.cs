using AmongUs.InnerNet.GameDataMessages;
using Hazel;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using System;

namespace TOHE.Modules.Rpc
{
    // Used to serialize multiple gamedata calls in a single message item
    public abstract class CustomModdedData : BaseGameDataMessage
    {
        // Refer BaseModdedRpc for how to code this dogshit framework
        public CustomModdedData(IntPtr ptr) : base(ptr) { }

        public override GameDataTypes GameDataType => GameDataTypes.Invalid;

        [HideFromIl2Cpp]
        public abstract GameDataTypes FirstDataType { get; }

        public CustomModdedData() : base(ClassInjector.DerivedConstructorPointer<CustomModdedData>())
        {
            ClassInjector.DerivedConstructorBody(this);
        }

        public override void SerializeValues(MessageWriter msg)
        {
            msg.CancelMessage();
            msg.StartMessage((byte)this.FirstDataType);
            // Logger.Info($"Serializing CustomModdedData: {this.FirstDataType}", "CustomModdedData.SerializeValues");
            SerializeCustomValues(msg);
        }

        [HideFromIl2Cpp]
        public abstract void SerializeCustomValues(MessageWriter msg);
        // Vanilla code will start first game data message and end it for you.
        // See RpcSetOutfit for example.
    }
}
