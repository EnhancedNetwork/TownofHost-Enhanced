using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor
{
    public static class Pitfall
    {
        private static readonly int Id = 8050;

        public static List<byte> playerIdList = new();
        public static bool IsEnable = false;

        private static List<PitfallTrap> Traps = new();
        private static List<byte> ReducedVisionPlayers = new();

        private static OptionItem ShapeshiftCooldown;
        public static OptionItem MaxTrapCount;
        public static OptionItem TrapMaxPlayerCount;
        public static OptionItem TrapDuration;
        private static OptionItem TrapRadius;
        private static OptionItem TrapFreezeTime;
        private static OptionItem TrapCauseVision;
        private static OptionItem TrapCauseVisionTime;

        private static float DefaultSpeed = new();

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Pitfall);
            ShapeshiftCooldown = FloatOptionItem.Create(Id + 10, "PitfallTrapCooldown", new(1f, 180f, 1f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pitfall])
                .SetValueFormat(OptionFormat.Seconds);
            MaxTrapCount = FloatOptionItem.Create(Id + 11, "PitfallMaxTrapCount", new(1f, 5f, 1f), 1f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pitfall])
                .SetValueFormat(OptionFormat.Times);
            TrapMaxPlayerCount = FloatOptionItem.Create(Id + 12, "PitfallTrapMaxPlayerCount", new(1f, 15f, 1f), 3f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pitfall])
                .SetValueFormat(OptionFormat.Times);
            TrapDuration = FloatOptionItem.Create(Id + 13, "PitfallTrapDuration", new(5f, 180f, 1f), 30f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Pitfall])
                .SetValueFormat(OptionFormat.Seconds);
            TrapRadius = FloatOptionItem.Create(Id + 14, "PitfallTrapRadius", new(0.5f, 5f, 0.5f), 2f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pitfall])
                .SetValueFormat(OptionFormat.Multiplier);
            TrapFreezeTime = FloatOptionItem.Create(Id + 15, "PitfallTrapFreezeTime", new(0f, 30f, 1f), 5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pitfall])
                .SetValueFormat(OptionFormat.Seconds);
            TrapCauseVision = FloatOptionItem.Create(Id + 16, "PitfallTrapCauseVision", new(0f, 5f, 0.05f), 0.2f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pitfall])
                .SetValueFormat(OptionFormat.Multiplier);
            TrapCauseVisionTime = FloatOptionItem.Create(Id + 17, "PitfallTrapCauseVisionTime", new(0f, 45f, 1f), 15f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pitfall])
                .SetValueFormat(OptionFormat.Seconds);
        }
        public static void ApplyGameOptions()
        {
            AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
            AURoleOptions.ShapeshifterDuration = 1f;
        }

        public static void Init()
        {
            playerIdList = new();
            Traps = new();
            ReducedVisionPlayers = new();
            IsEnable = false;
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            DefaultSpeed = Main.AllPlayerSpeed[playerId];
            IsEnable = true;
        }

        public static void OnShapeshift(PlayerControl shapeshifter)
        {
            // Remove inactive traps so there is room for new traps
            Traps = Traps.Where(a => a.IsActive).ToList();

            Vector2 position = shapeshifter.transform.position;
            var playerTraps = Traps.Where(a => a.PitfallPlayerId == shapeshifter.PlayerId);
            if (playerTraps.Count() >= MaxTrapCount.GetInt())
            {
                var trap = playerTraps.First();
                trap.Location = position;
                trap.PlayersTrapped = new List<int>();
                trap.Timer = 0;
            }
            else
            {
                Traps.Add(new PitfallTrap
                {
                    PitfallPlayerId = shapeshifter.PlayerId,
                    Location = position,
                    PlayersTrapped = new List<int>(),
                    Timer = 0
                });
            }
        }

        public static void OnFixedUpdate(PlayerControl player)
        {
            if (Pelican.IsEaten(player.PlayerId) || !player.IsAlive()) return;

            if (player.GetCustomRole().IsImpostor())
            {
                var traps = Traps.Where(a => a.PitfallPlayerId == player.PlayerId && a.IsActive);
                foreach (var trap in traps)
                {
                    trap.Timer += Time.fixedDeltaTime;
                }
                return;
            }

            Vector2 position = player.transform.position;

            foreach (var trap in Traps.Where(a => a.IsActive))
            {
                if (trap.PlayersTrapped.Contains(player.PlayerId))
                {
                    continue;
                }

                var dis = Vector2.Distance(trap.Location, position);
                if (dis > TrapRadius.GetFloat()) continue;

                if (TrapFreezeTime.GetFloat() > 0)
                {
                    TrapPlayer(player);
                }

                if (TrapCauseVisionTime.GetFloat() > 0)
                {
                    ReducePlayerVision(player);
                }

                trap.PlayersTrapped.Add(player.PlayerId);

                player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Pitfall), GetString("PitfallTrap")));
            }
        }

        public static void SetPitfallTrapVision(IGameOptions opt, PlayerControl target)
        {
            if (ReducedVisionPlayers.Contains(target.PlayerId))
            {
                opt.SetVision(false);
                opt.SetFloat(FloatOptionNames.CrewLightMod, TrapCauseVision.GetFloat());
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, TrapCauseVision.GetFloat());
            }
        }

        private static void TrapPlayer(PlayerControl player)
        {
            Main.AllPlayerSpeed[player.PlayerId] = Main.MinSpeed;
            ReportDeadBodyPatch.CanReport[player.PlayerId] = false;
            player.MarkDirtySettings();
            _ = new LateTask(() =>
            {
                Main.AllPlayerSpeed[player.PlayerId] = DefaultSpeed;
                ReportDeadBodyPatch.CanReport[player.PlayerId] = true;
                player.MarkDirtySettings();
            }, TrapFreezeTime.GetFloat(), "PitfallTrapPlayerFreeze");
        }

        private static void ReducePlayerVision(PlayerControl player)
        {
            if (ReducedVisionPlayers.Contains(player.PlayerId)) return;

            ReducedVisionPlayers.Add(player.PlayerId);
            player.MarkDirtySettings();

            _ = new LateTask(() =>
            {
                ReducedVisionPlayers.Remove(player.PlayerId);
                player.MarkDirtySettings();
            }, TrapCauseVisionTime.GetFloat(), "PitfallTrapPlayerVision");
        }
    }

    public class PitfallTrap
    {
        public int PitfallPlayerId;
        public Vector2 Location;
        public float Timer;
        public List<int> PlayersTrapped;
        public bool IsActive
        {
            get
            {
                return Timer <= Pitfall.TrapDuration.GetFloat() && PlayersTrapped.Count < Pitfall.TrapMaxPlayerCount.GetInt();
            }
        }
    }
}
