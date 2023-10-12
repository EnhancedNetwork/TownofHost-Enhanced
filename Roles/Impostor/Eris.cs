using System.Collections.Generic;
using System.Linq;
using static TOHE.Options;

namespace TOHE.Roles.Impostor
{
    public class Eris
    {
        private static readonly int Id = 2121212;
        public static List<byte> playerIdList = new();
        public static bool IsEnable = false;

        private static OptionItem KillCooldown;
        private static OptionItem AbilityLimit;
        private static OptionItem KillsPerAbilityUse;

        private static IRandom rd = IRandom.Instance;

        private static Dictionary<int, int> AbilityUseCount;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Eris);
            KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(20f, 180f, 1f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Eris])
                .SetValueFormat(OptionFormat.Seconds);
            AbilityLimit = IntegerOptionItem.Create(Id + 11, "ErisAbilityLimit", new(1, 15, 1), 3, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Eris])
                .SetValueFormat(OptionFormat.Times);
            KillsPerAbilityUse = IntegerOptionItem.Create(Id + 12, "ErisKillsPerAbilityUse", new(1, 15, 1), 1, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Eris])
                .SetValueFormat(OptionFormat.Times);
        }
        public static void Init()
        {
            playerIdList = new();
            IsEnable = false;
            AbilityUseCount = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            IsEnable = true;
            AbilityUseCount.Add(playerId, 0);
        }

        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

        public static void OnPlayerExile(GameData.PlayerInfo exiled)
        {
            if (!IsEnable || !exiled.GetCustomRole().IsCrewmate()) return;

            foreach (var player in playerIdList)
            {
                if (AbilityUseCount[player] >= AbilityLimit.GetInt()) continue;

                var killer = Main.AllPlayerControls.FirstOrDefault(a => a.PlayerId == player);
                if (!killer.IsAlive()) continue;

                List<PlayerControl> killPotentials = new List<PlayerControl>();
                var votedForExiled = MeetingHud.Instance.playerStates.Where(a => a.VotedFor == exiled.PlayerId).ToList();
                foreach (var playerVote in votedForExiled)
                {
                    var crewPlayer = Main.AllPlayerControls.FirstOrDefault(a => a.PlayerId == playerVote.TargetPlayerId);
                    if (crewPlayer == null) continue;
                    if (crewPlayer.Data.GetCustomRole().IsCrewmate())
                        killPotentials.Add(crewPlayer);
                }

                if (!killPotentials.Any()) break;

                for (int i = 0; i < KillsPerAbilityUse.GetInt(); i++)
                {
                    if (!killPotentials.Any()) break;

                    PlayerControl target = killPotentials[rd.Next(0, killPotentials.Count-1)];
                    target.RpcMurderPlayerV3(target);
                    target.SetRealKiller(killer);
                    Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Discord;

                    killPotentials.Remove(target);
                }

                AbilityUseCount[player] += 1;
            }
        }
    }
}
