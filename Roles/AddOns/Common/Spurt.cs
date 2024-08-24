using static TOHE.Options;
using UnityEngine;
using Hazel;

namespace TOHE.Roles.AddOns.Common
{
    internal class Spurt : IAddon
    {
        private static OptionItem MinSpeed;
        private static OptionItem Modulator;
        private static OptionItem MaxSpeed;
        private static OptionItem DisplaysCharge;

        private static readonly Dictionary<byte, Vector2> LastPos = [];
        public static readonly Dictionary<byte, float> StartingSpeed = [];
        private static readonly Dictionary<byte, int> LastNum = [];
        private static readonly Dictionary<byte, long> LastUpdate = [];
        public AddonTypes Type => AddonTypes.Helpful;

        public void SetupCustomOption()
        {
            const int id = 28800;
            SetupAdtRoleOptions(id, CustomRoles.Spurt, canSetNum: true, teamSpawnOptions: true);
            MinSpeed = FloatOptionItem.Create(id + 6, "SpurtMinSpeed", new(0f, 3f, 0.25f), 0.75f, TabGroup.Addons, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Spurt])
                .SetValueFormat(OptionFormat.Multiplier);
            MaxSpeed = FloatOptionItem.Create(id + 7, "SpurtMaxSpeed", new(1.5f, 3f, 0.25f), 3f, TabGroup.Addons, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Spurt])
                .SetValueFormat(OptionFormat.Multiplier);
            Modulator =FloatOptionItem.Create(id + 8, "SpurtModule", new(0.25f, 3f, 0.25f), 1.25f, TabGroup.Addons, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Spurt])
                .SetValueFormat(OptionFormat.Multiplier);
            DisplaysCharge = BooleanOptionItem.Create(id + 9, "EnableSpurtCharge", false, TabGroup.Addons, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Spurt]);
        }
        private static void Sendrpc(byte playerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SpurtSync, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(Main.AllPlayerSpeed[playerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void RecieveRPC(MessageReader reader)
        {
            byte playerid = reader.ReadByte();
            float speed = reader.ReadSingle();
            Main.AllPlayerSpeed[playerid] = speed;
        }
        public static void Add()
        {
            foreach ((PlayerControl pc, float speed) in Main.AllAlivePlayerControls.Zip(Main.AllPlayerSpeed.Values))
            {
                if (pc.Is(CustomRoles.Spurt))
                {
                    LastPos[pc.PlayerId] = pc.GetCustomPosition();
                    LastNum[pc.PlayerId] = 0;
                    LastUpdate[pc.PlayerId] = Utils.TimeStamp;
                    StartingSpeed[pc.PlayerId] = speed;
                }
            }
        }

        public static void DeathTask(PlayerControl player)
        {
            if (!player.Is(CustomRoles.Spurt)) return;

            Main.AllPlayerSpeed[player.PlayerId] = StartingSpeed[player.PlayerId];
            player.MarkDirtySettings();
        }

        private static int DetermineCharge(PlayerControl player)
        {
            float minSpeed = MinSpeed.GetFloat();
            float maxSpeed = MaxSpeed.GetFloat();

            if (Mathf.Approximately(minSpeed, maxSpeed))
                return 100;

            return (int)((Main.AllPlayerSpeed[player.PlayerId] - minSpeed) / (maxSpeed - minSpeed) * 100);
        }

        public static string GetSuffix(PlayerControl player, bool isforhud = false, bool isformeeting = false)
        {
            if (!player.Is(CustomRoles.Spurt) || !DisplaysCharge.GetBool() || GameStates.IsMeeting || isformeeting)
                return string.Empty;

            int fontsize = isforhud ? 100 : 55;

            return $"<size={fontsize}%>{string.Format(Translator.GetString("SpurtSuffix"), DetermineCharge(player))}</size>";
        }

        public void OnFixedUpdate(PlayerControl player)
        {
            if (!player.Is(CustomRoles.Spurt) || !player.IsAlive()) return;

            var pos = player.GetCustomPosition();
            bool moving = Utils.GetDistance(pos, LastPos[player.PlayerId]) > 0f || player.MyPhysics.Animations.IsPlayingRunAnimation();
            LastPos[player.PlayerId] = pos;

            float modulator = Modulator.GetFloat();
            float ChargeBy = Mathf.Clamp(modulator / 20 * 1.5f, 0.05f, 0.6f);
            float Decreaseby = Mathf.Clamp(modulator / 20 * 0.5f, 0.01f, 0.3f);

            int charge = DetermineCharge(player);
            if (DisplaysCharge.GetBool() && !player.IsModClient() && LastNum[player.PlayerId] != charge)
            {
                LastNum[player.PlayerId] = charge;
                long now = Utils.TimeStamp;
                if (now != LastUpdate[player.PlayerId])
                {
                    Utils.NotifyRoles(SpecifySeer: player, SpecifyTarget: player);
                    LastUpdate[player.PlayerId] = now;
                    Sendrpc(player.PlayerId);
                }
            }

            if (!moving)
            {
                Main.AllPlayerSpeed[player.PlayerId] += Mathf.Clamp(ChargeBy, 0f, MaxSpeed.GetFloat() - Main.AllPlayerSpeed[player.PlayerId]);
                return;
            }

            Main.AllPlayerSpeed[player.PlayerId] -= Mathf.Clamp(Decreaseby, 0f, Main.AllPlayerSpeed[player.PlayerId] - MinSpeed.GetFloat());
            player.MarkDirtySettings();
        }
    }
}
