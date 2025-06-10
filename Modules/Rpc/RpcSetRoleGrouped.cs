using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;
using Hazel;

namespace TOHE.Modules.Rpc
{
    public class RpcSetRoleGrouped : CustomModdedData
    {
        // Use RpcUtils.SendMessageSpecifically to send this message
        public override GameDataTypes FirstDataType => GameDataTypes.RpcFlag;
        public RpcSetRoleGrouped(List<(PlayerControl, RoleTypes)> playerRoles) : base()
        {
            if (playerRoles.Count == 1)
            {
                firstCall = (playerRoles[0].Item1.NetId, playerRoles[0].Item2);
                lastCall = null;
            }
            else
            {
                firstCall = (playerRoles.First().Item1.NetId, playerRoles.First().Item2);
                lastCall = (playerRoles.Last().Item1.NetId, playerRoles.Last().Item2);
            }

            var middleRoles = playerRoles.Count > 2
                ? playerRoles.Skip(1).Take(playerRoles.Count - 2)
                : [];

            foreach (var (player, role) in middleRoles)
            {
                var message = new RpcSetRoleMessage(player.NetId, role, true);
                messages.Add(message);
            }
        }

        public override void SerializeCustomValues(MessageWriter writer)
        {
            // writer.StartMessage(GameDataTypes.RpcFlag);
            writer.WritePacked(firstCall.Item1);
            writer.Write((byte)RpcCalls.SetRole);
            writer.Write((ushort)firstCall.Item2);
            writer.Write(true);

            if (lastCall == null)
            {
                // writer.EndMessage();
                return;
            }

            writer.EndMessage();

            foreach (var message in messages)
            {
                message.Serialize(writer);
            }

            writer.StartMessage((byte)GameDataTypes.RpcFlag);
            writer.WritePacked(lastCall.Value.Item1);
            writer.Write((byte)RpcCalls.SetRole);
            writer.Write((ushort)lastCall.Value.Item2);
            writer.Write(true);
            // writer.EndMessage();
        }

        private readonly (uint, RoleTypes) firstCall;
        private readonly (uint, RoleTypes)? lastCall;
        private readonly List<RpcSetRoleMessage> messages = [];
    }
}
