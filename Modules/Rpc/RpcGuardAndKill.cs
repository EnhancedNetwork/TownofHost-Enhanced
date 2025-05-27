using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;

namespace TOHE.Modules.Rpc
{
    class RpcGuardAndKill : CustomModdedData
    {
        public override GameDataTypes FirstDataType => GameDataTypes.DataFlag;
        public RpcGuardAndKill(PlayerControl player, PlayerControl target = null)
        {
            this.netId = player.NetId;
            this.targetNetId = target != null ? target.NetId : player.NetId;

            var optionsender = PlayerGameOptionsSender.AllSenders.OfType<PlayerGameOptionsSender>().FirstOrDefault(x => x.player.PlayerId == player.PlayerId);
            if (optionsender == null)
            {
                Logger.Error($"PlayerGameOptionsSender not found for player {player.PlayerId}", "SendOptionsArrayCustom");
                this.options = GameManager.Instance.LogicOptions.currentGameOptions;
                return;
            }

            this.options = optionsender.BuildGameOptions();
        }
        public override void SerializeCustomValues(Hazel.MessageWriter writer)
        {
            // start message data flag
            var optionsMessage = new SendOptionsArray(options);
            optionsMessage.SerializeCustomValues(writer);
            writer.EndMessage();

            writer.StartMessage((byte)GameDataTypes.RpcFlag);
            writer.WritePacked(netId);
            writer.Write((byte)RpcCalls.MurderPlayer);
            writer.WritePacked(targetNetId);
            writer.Write((int)MurderResultFlags.FailedProtected);
            // endmessage
        }

        private readonly uint netId;
        private readonly uint targetNetId;
        private readonly IGameOptions options;
    }
}
