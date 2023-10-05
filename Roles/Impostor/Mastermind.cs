using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Crewmate;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor
{
    internal class Mastermind
    {
        private static readonly int Id = 640600;
        public static List<byte> playerIdList = new();

        public static Dictionary<byte, long> ManipulatedPlayers = new();
        public static Dictionary<byte, long> ManipulateDelays = new();
        public static Dictionary<byte, float> TempKCDs = new();

        public static OptionItem KillCooldown;
        //public static OptionItem ManipulateCDOpt;
        public static OptionItem TimeLimit;
        public static OptionItem Delay;

        public static float ManipulateCD;

        public static void SetupCustomOption()
        {
            SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Mastermind, 1);
            KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mastermind])
                .SetValueFormat(OptionFormat.Seconds);
            //ManipulateCDOpt = FloatOptionItem.Create(Id + 11, "MastermindCD", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mastermind])
            //    .SetValueFormat(OptionFormat.Seconds);
            // Manipulation Cooldown = Kill Cooldown + Delay + Time Limit
            TimeLimit = FloatOptionItem.Create(Id + 12, "MastermindTimeLimit", new(1f, 60f, 1f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mastermind])
                .SetValueFormat(OptionFormat.Seconds);
            Delay = FloatOptionItem.Create(Id + 13, "MastermindDelay", new(0f, 30f, 1f), 7f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mastermind])
                .SetValueFormat(OptionFormat.Seconds);
        }

        public static void Init()
        {
            playerIdList = new();
            ManipulatedPlayers = new();
            ManipulateDelays = new();
            TempKCDs = new();
        }

        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            ManipulateCD = KillCooldown.GetFloat() + (TimeLimit.GetFloat() / 2) + (Delay.GetFloat() / 2);
        // Double Trigger
        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();
        }

        public static bool IsEnable => playerIdList.Any();

        public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            if (killer == null) return false;
            if (target == null) return false;

            return killer.CheckDoubleTrigger(target, () =>
            {
                killer.SetKillCooldown(time: ManipulateCD);
                if (target.HasKillButton() || CopyCat.playerIdList.Contains(target.PlayerId) || Main.TasklessCrewmate.Contains(target.PlayerId))
                {
                    ManipulateDelays.TryAdd(target.PlayerId, GetTimeStamp());
                    NotifyRoles(SpecifySeer: GetPlayerById(playerIdList[0]));
                }
            });
        }

        public static void OnFixedUpdate()
        {
            if (GameStates.IsMeeting) return;
            if (!ManipulatedPlayers.Any() && !ManipulateDelays.Any()) return;

            foreach (var x in ManipulateDelays)
            {
                var pc = GetPlayerById(x.Key);

                if (!pc.IsAlive())
                {
                    ManipulateDelays.Remove(x.Key);
                    continue;
                }
                if (x.Value + Delay.GetInt() < GetTimeStamp())
                {
                    ManipulateDelays.Remove(x.Key);
                    ManipulatedPlayers.TryAdd(x.Key, GetTimeStamp());

                    TempKCDs.TryAdd(pc.PlayerId, pc.killTimer);
                    pc.SetKillCooldown(time: 1f);

                    NotifyRoles(SpecifySeer: GetPlayerById(playerIdList[0]));
                }
            }

            foreach (var x in ManipulatedPlayers)
            {
                var player = GetPlayerById(x.Key);

                if (!player.IsAlive())
                {
                    ManipulatedPlayers.Remove(x.Key);
                    TempKCDs.Remove(x.Key);
                    continue;
                }
                if (x.Value + TimeLimit.GetInt() < GetTimeStamp())
                {
                    ManipulatedPlayers.Remove(x.Key);
                    TempKCDs.Remove(x.Key);
                    player.SetRealKiller(GetPlayerById(playerIdList[0]));
                    Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Suicide;
                    player.RpcMurderPlayerV3(player);
                    RPC.PlaySoundRPC(playerIdList[0], Sounds.KillSound);
                }

                var time = TimeLimit.GetInt() - (GetTimeStamp() - x.Value);

                player.Notify(string.Format(GetString("ManipulateNotify"), time), 1.1f);
            }
        }

        public static void OnReportDeadBody()
        {
            foreach (var x in ManipulatedPlayers)
            {
                var pc = GetPlayerById(x.Key);
                if (pc.IsAlive())
                {
                    pc.SetRealKiller(GetPlayerById(playerIdList[0]));
                    Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Suicide;
                    pc.RpcMurderPlayerV3(pc);
                }
            }
            ManipulateDelays.Clear();
            ManipulatedPlayers.Clear();
            TempKCDs.Clear();
        }

        public static bool ForceKillForManipulatedPlayer(PlayerControl killer, PlayerControl target)
        {
            if (killer == null) return false;
            if (target == null) return false;

            ManipulatedPlayers.Remove(killer.PlayerId);

            var mastermind = GetPlayerById(playerIdList[0]);
            mastermind.Notify(string.Format(GetString("ManipulatedKilled"), target.GetRealName()), 4f);
            mastermind.SetKillCooldown(time: KillCooldown.GetFloat());
            killer.Notify(GetString("SurvivedManipulation"));

            if (target.Is(CustomRoles.Pestilence) || Main.VeteranInProtect.ContainsKey(target.PlayerId) || target.Is(CustomRoles.Mastermind))
            {
                target.RpcMurderPlayerV3(killer);
                TempKCDs.Remove(killer.PlayerId);
                return false;
            }

            killer.RpcMurderPlayerV3(target);

            _ = new LateTask(() =>
            {
                killer.SetKillCooldown(time: TempKCDs[killer.PlayerId] + Main.AllPlayerKillCooldown[killer.PlayerId]);
                TempKCDs.Remove(killer.PlayerId);
            }, 0.1f, "Set KCD for Manipulated Kill");

            return true;
        }
    }
}
