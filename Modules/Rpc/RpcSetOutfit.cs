using AmongUs.InnerNet.GameDataMessages;
using Hazel;

namespace TOHE.Modules.Rpc
{
    public class RpcSetOutfit : CustomModdedData
    {
        public override GameDataTypes FirstDataType => GameDataTypes.RpcFlag;

        public RpcSetOutfit(uint netId, uint playerInfoNetId, NetworkedPlayerInfo.PlayerOutfit outfit, bool setName, bool setNamePlate) : base()
        {
            this.netId = netId;
            this.playerInfoNetId = playerInfoNetId;
            this.outfit = outfit;

            messages = [];

            if (setName)
            {
                messages.Add(new RpcSetNameMessage(netId, playerInfoNetId, outfit.PlayerName));
            }

            messages.Add(new RpcSetHatStrMessage(netId, outfit.HatId, GetNextSequenceId(netId, RpcCalls.SetHatStr)));
            messages.Add(new RpcSetPetStrMessage(netId, outfit.PetId, GetNextSequenceId(netId, RpcCalls.SetPetStr)));
            messages.Add(new RpcSetSkinStrMessage(netId, outfit.SkinId, GetNextSequenceId(netId, RpcCalls.SetSkinStr)));
            // messages.Add(new RpcSetVisorStrMessage(netId, outfit.VisorId, GetNextSequenceId(netId, RpcCalls.SetVisorStr)));

            if (setNamePlate)
            {
                messages.Add(new RpcSetNamePlateStrMessage(netId, outfit.NamePlateId, GetNextSequenceId(netId, RpcCalls.SetNamePlateStr)));
            }
        }

        public override void SerializeCustomValues(MessageWriter writer)
        {
            // First StartMessage and last EndMessage is done by vanilla code so you need to implement yourself
            // writer.StartMessage(GameDataTypes.RpcFlag); 
            writer.WritePacked(netId);
            writer.Write((byte)RpcCalls.SetColor);
            writer.Write(playerInfoNetId);
            writer.Write((byte)outfit.ColorId);
            writer.EndMessage();

            foreach (var message in messages)
            {
                message.Serialize(writer);
            }

            writer.StartMessage((byte)GameDataTypes.RpcFlag);
            writer.WritePacked(netId);
            writer.Write((byte)RpcCalls.SetVisorStr);
            writer.Write(outfit.VisorId);
            writer.Write(GetNextSequenceId(netId, RpcCalls.SetVisorStr));

            //writer.EndMessage();
        }

        private static byte GetNextSequenceId(uint netId, RpcCalls call)
        {
            if (AmongUsClient.Instance.allObjects.allObjectsFast.TryGetValue(netId, out var obj))
            {
                if (obj is PlayerControl player)
                {
                    return player.GetNextRpcSequenceId(call);
                }
            }

            return 0;
        }

        private readonly uint netId;
        private readonly uint playerInfoNetId;
        private readonly NetworkedPlayerInfo.PlayerOutfit outfit;
        private readonly List<BaseRpcMessage> messages;
    }
}
