namespace TOHE.Modules.Rpc
{
    class RpcPlaySound : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.PlaySound;
        public RpcPlaySound(uint rpcObjectNetId, byte playerId, Sounds sound) : base(rpcObjectNetId)
        {
            this.playerId = playerId;
            this.sound = sound;
        }
        public override void SerializeRpcValues(Hazel.MessageWriter writer)
        {
            writer.Write(playerId);
            writer.Write((byte)sound);
        }
        private readonly byte playerId;
        private readonly Sounds sound;
    }
}
