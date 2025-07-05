using Hazel;

namespace TOHE.Modules.Rpc
{
    class RpcSyncGeneralOptions : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SyncGeneralOptions;

        public RpcSyncGeneralOptions(uint rpcObjectNetId, byte playerId, CustomRoles role, bool isDead, bool isDisconnected, PlayerState.DeathReason deathReason, float killCooldown, float speed) : base(rpcObjectNetId)
        {
            this.playerId = playerId;
            this.role = role;
            this.isDead = isDead;
            this.isDisconnected = isDisconnected;
            this.deathReason = deathReason;
            this.killCooldown = killCooldown;
            this.speed = speed;
        }

        public override void SerializeRpcValues(MessageWriter writer)
        {
            writer.Write(playerId);
            writer.WritePacked((int)role);
            writer.Write(isDead);
            writer.Write(isDisconnected);
            writer.WritePacked((int)deathReason);
            writer.Write(killCooldown);
            writer.Write(speed);

        }

        private readonly byte playerId;
        private readonly CustomRoles role;
        private readonly bool isDead;
        private readonly bool isDisconnected;
        private readonly PlayerState.DeathReason deathReason;
        private readonly float killCooldown;
        private readonly float speed;

    }
}
