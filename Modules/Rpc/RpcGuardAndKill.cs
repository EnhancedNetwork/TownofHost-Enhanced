using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;
using Hazel;

namespace TOHE.Modules.Rpc
{
    public class RpcGuardAndKill : CustomModdedData
    {
        public override GameDataTypes FirstDataType => GameDataTypes.RpcFlag;
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
        public override void SerializeCustomValues(MessageWriter writer)
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

    public class RpcGuardAndKillModded : CustomModdedData
    {
        public override GameDataTypes FirstDataType => GameDataTypes.RpcFlag;
        public RpcGuardAndKillModded(PlayerControl player, PlayerControl target, float timer)
        {
            this.netId = player.NetId;
            this.targetNetId = target.NetId;
            this.timer = timer;
        }
        public override void SerializeCustomValues(MessageWriter writer)
        {
            writer.WritePacked(netId);
            writer.Write((byte)CustomRPC.PlayGuardAndKill);
            writer.WritePacked(targetNetId);
            writer.EndMessage();

            writer.StartMessage((byte)GameDataTypes.RpcFlag);
            writer.WritePacked(netId);
            writer.Write((byte)CustomRPC.SetKillTimer);
            writer.Write(timer);
        }
        private readonly uint netId;
        private readonly uint targetNetId;
        private readonly float timer;
    }
}
