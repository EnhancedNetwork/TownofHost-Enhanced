using Hazel;

namespace TOHE.Modules.Rpc
{
    class RpcEndGame : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.EndGame;

        public RpcEndGame(uint rpcObjectNetId, CustomWinner winners, HashSet<AdditionalWinners> additionalWinners, HashSet<CustomRoles> winnerRoles, HashSet<byte> winnerIds) : base(rpcObjectNetId)
        {
            this.WinnerTeam = winners;
            this.AdditionalWinnerTeams = additionalWinners;
            this.WinnerRoles = winnerRoles;
            this.WinnerIds = winnerIds;
        }

        public override void SerializeRpcValues(MessageWriter writer)
        {
            writer.WritePacked((int)WinnerTeam);

            writer.WritePacked(AdditionalWinnerTeams.Count);
            foreach (var wt in AdditionalWinnerTeams)
                writer.WritePacked((int)wt);

            writer.WritePacked(WinnerRoles.Count);
            foreach (var wr in WinnerRoles)
                writer.WritePacked((int)wr);

            writer.WritePacked(WinnerIds.Count);
            foreach (var id in WinnerIds)
                writer.Write(id);
        }

        private readonly CustomWinner WinnerTeam;
        private readonly HashSet<AdditionalWinners> AdditionalWinnerTeams;
        private readonly HashSet<CustomRoles> WinnerRoles;
        private readonly HashSet<byte> WinnerIds;
    }
}
