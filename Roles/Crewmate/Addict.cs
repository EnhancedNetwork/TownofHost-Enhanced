using UnityEngine;
using System.Collections.Generic;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate
{
    public static class Addict
    {
        private static readonly int Id = 5200;
        private static List<byte> playerIdList = new();
        public static bool IsEnable = false;

        public static OptionItem VentCooldown;
        public static OptionItem TimeLimit;
        public static OptionItem ImmortalTimeAfterVent;
   //     public static OptionItem SpeedWhileImmortal;
        public static OptionItem FreezeTimeAfterImmortal;

        private static Dictionary<byte, float> SuicideTimer = new();
        private static Dictionary<byte, float> ImmortalTimer = new();

        private static float DefaultSpeed = new();

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Addict);
            VentCooldown = FloatOptionItem.Create(Id + 11, "VentCooldown", new(5f, 180f, 1f), 40f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Addict])
                .SetValueFormat(OptionFormat.Seconds);
            TimeLimit = FloatOptionItem.Create(Id + 12, "SerialKillerLimit", new(5f, 180f, 1f), 45f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Addict])
                .SetValueFormat(OptionFormat.Seconds);
            ImmortalTimeAfterVent = FloatOptionItem.Create(Id + 13, "AddictInvulnerbilityTimeAfterVent", new(0f, 60f, 1f), 10f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Addict])
                .SetValueFormat(OptionFormat.Seconds);
       //     SpeedWhileImmortal = FloatOptionItem.Create(Id + 14, "AddictSpeedWhileInvulnerble", new(0.25f, 5f, 0.25f), 1.75f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Addict])
         //       .SetValueFormat(OptionFormat.Multiplier);
            FreezeTimeAfterImmortal = FloatOptionItem.Create(Id + 15, "AddictFreezeTimeAfterInvulnerbility", new(0f, 60f, 1f), 10f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Addict])
                .SetValueFormat(OptionFormat.Seconds);
        }
        public static void Init()
        {
            playerIdList = new();
            SuicideTimer = new();
            ImmortalTimer = new();
            DefaultSpeed = new();
            IsEnable = false;
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            SuicideTimer.TryAdd(playerId, -10f);
            ImmortalTimer.TryAdd(playerId, 420f);
            DefaultSpeed = Main.AllPlayerSpeed[playerId];
            IsEnable = true;
        }

        public static bool IsImmortal(PlayerControl player) => player.Is(CustomRoles.Addict) && ImmortalTimer[player.PlayerId] <= ImmortalTimeAfterVent.GetFloat();

        public static void OnReportDeadBody()
        {
            foreach (var player in playerIdList)
            {
                SuicideTimer[player] = -10f;
                ImmortalTimer[player] = 420f;
                Main.AllPlayerSpeed[player] = DefaultSpeed;
            }
        }

        public static void OnFixedUpdate(PlayerControl player)
        {
            if (!SuicideTimer.ContainsKey(player.PlayerId) || !player.IsAlive()) return;

            if (SuicideTimer[player.PlayerId] >= TimeLimit.GetFloat())
            {
                Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Suicide;
                player.RpcMurderPlayerV3(player);
                SuicideTimer.Remove(player.PlayerId);
            }
            else
            { 
                SuicideTimer[player.PlayerId] += Time.fixedDeltaTime;

                if (IsImmortal(player))
                {
                    ImmortalTimer[player.PlayerId] += Time.fixedDeltaTime;
                }
                else
                {
                    if (ImmortalTimer[player.PlayerId] != 420f && FreezeTimeAfterImmortal.GetFloat() > 0)
                    {
                        AddictGetDown(player);
                        ImmortalTimer[player.PlayerId] = 420f;
                    }
                }
            }
        }

        public static void OnEnterVent(PlayerControl pc, Vent vent) 
        {
            if (!IsEnable) return;
            if (!pc.Is(CustomRoles.Addict)) return;

            SuicideTimer[pc.PlayerId] = 0f;
            ImmortalTimer[pc.PlayerId] = 0f;

         //   Main.AllPlayerSpeed[pc.PlayerId] = SpeedWhileImmortal.GetFloat();
            pc.MarkDirtySettings();
        }

        private static void AddictGetDown(PlayerControl addict)
        {
            Main.AllPlayerSpeed[addict.PlayerId] = Main.MinSpeed;
            ReportDeadBodyPatch.CanReport[addict.PlayerId] = false;
            addict.MarkDirtySettings();
            _ = new LateTask(() =>
            {
                Main.AllPlayerSpeed[addict.PlayerId] = DefaultSpeed;
                ReportDeadBodyPatch.CanReport[addict.PlayerId] = true;
                addict.MarkDirtySettings();
            }, FreezeTimeAfterImmortal.GetFloat(), "AddictGetDown");
        }
    }
}