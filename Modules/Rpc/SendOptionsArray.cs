using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace TOHE.Modules.Rpc
{
    class SendOptionsArray : CustomModdedData
    {
        public override GameDataTypes FirstDataType => GameDataTypes.DataFlag;

        public SendOptionsArray(Il2CppStructArray<byte> optionArray)
        {
            if (GameManager.Instance == null || GameManager.Instance.LogicOptions == null)
            {
                Logger.Error("GameManager or LogicOptions is null in SendOptionsArray constructor", "SendOptionsArrayCustom");
                return;
            }

            this.netId = GameManager.Instance.NetId;
            this.logicOptionsIndex = (byte)GameManager.Instance.LogicComponents.IndexOf(GameManager.Instance.LogicOptions);
            this.optionArray = optionArray;
        }

        public SendOptionsArray(PlayerControl player)
        {
            if (GameManager.Instance == null || GameManager.Instance.LogicOptions == null)
            {
                Logger.Error("GameManager or LogicOptions is null in SendOptionsArray constructor", "SendOptionsArrayCustom");
                return;
            }

            var optionsender = PlayerGameOptionsSender.AllSenders.OfType<PlayerGameOptionsSender>().FirstOrDefault(x => x.player.PlayerId == player.PlayerId);
            if (optionsender == null)
            {
                Logger.Error($"PlayerGameOptionsSender not found for player {player.PlayerId}", "SendOptionsArrayCustom");
                return;
            }

            var options = optionsender.BuildGameOptions();
            this.netId = GameManager.Instance.NetId;
            this.logicOptionsIndex = (byte)GameManager.Instance.LogicComponents.IndexOf(GameManager.Instance.LogicOptions);
            this.optionArray = GameManager.Instance.LogicOptions.gameOptionsFactory.ToBytes(options, false);
        }

        public SendOptionsArray(IGameOptions options)
        {
            if (GameManager.Instance == null || GameManager.Instance.LogicOptions == null)
            {
                Logger.Error("GameManager or LogicOptions is null in SendOptionsArray constructor", "SendOptionsArrayCustom");
                return;
            }
            this.netId = GameManager.Instance.NetId;
            this.logicOptionsIndex = (byte)GameManager.Instance.LogicComponents.IndexOf(GameManager.Instance.LogicOptions);
            this.optionArray = GameManager.Instance.LogicOptions.gameOptionsFactory.ToBytes(options, false);
        }

        public override void SerializeCustomValues(MessageWriter writer)
        {
            writer.WritePacked(netId);
            writer.StartMessage(logicOptionsIndex);
            {
                writer.WriteBytesAndSize(optionArray);
            }
            writer.EndMessage();
        }

        private readonly uint netId;
        private readonly byte logicOptionsIndex;
        private readonly Il2CppStructArray<byte> optionArray;
    }
}
