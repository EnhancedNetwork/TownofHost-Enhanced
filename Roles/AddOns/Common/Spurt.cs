using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TOHE.Options;
using UnityEngine;

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
            const int id = 648950;
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

        public static void Add()
        {
            foreach ((PlayerControl pc, float speed) in Main.AllAlivePlayerControls.Zip(Main.AllPlayerSpeed.Values))
            {
                if (pc.Is(CustomRoles.Spurt))
                {
                    LastPos[pc.PlayerId] = pc.Pos();
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

        public static string GetSuffix(PlayerControl player, bool isforhud = false)
        {
            if (!player.Is(CustomRoles.Spurt) || !DisplaysCharge.GetBool() || GameStates.IsMeeting)
                return string.Empty;

            int fontsize = isforhud ? 100 : 55;

            return $"<size={fontsize}%>{string.Format(Translator.GetString("SpurtSuffix"), DetermineCharge(player))}</size>";
        }

        public void OnFixedUpdate(PlayerControl player)
        {
            if (!player.Is(CustomRoles.Spurt) || !player.IsAlive()) return;

            var pos = player.Pos();
            bool moving = Vector2.Distance(pos, LastPos[player.PlayerId]) > 0f || player.MyPhysics.Animations.IsPlayingRunAnimation();
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
