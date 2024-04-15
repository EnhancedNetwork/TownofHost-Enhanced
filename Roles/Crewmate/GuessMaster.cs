using System.Collections.Generic;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate
{
    public static class GuessMaster
    {
        private static readonly int Id = 26800;
        private static bool IsEnable = false;
        private static HashSet<byte> playerIdList = [];
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.GuessMaster);
        }

        public static void Init()
        {
            IsEnable = false;
            playerIdList = [];
        }
        public static void Add(byte playerId)
        {
            IsEnable = true;
            playerIdList.Add(playerId);
        }
        public static void Remove(byte playerId)
        {
            playerIdList.Remove(playerId);
        }

        public static void OnGuess(CustomRoles role, bool isMisguess = false, PlayerControl dp = null)
        {
            if (!IsEnable) return;
            foreach (var gmID in playerIdList)
            {
                var gmPC = Utils.GetPlayerById(gmID);
                if (gmPC == null || !gmPC.IsAlive()) continue;
                if (isMisguess && dp != null)
                {
                    _ = new LateTask(() =>
                    {
                        Utils.SendMessage(string.Format(GetString("GuessMasterMisguess"), dp.GetRealName()), gmID, Utils.ColorString(Utils.GetRoleColor(CustomRoles.GuessMaster), GetString("GuessMasterTitle")));
                    }, 1f, "GuessMaster On Miss Guess");
                }
                else
                {
                    _ = new LateTask(() =>
                    {
                        Utils.SendMessage(string.Format(GetString("GuessMasterTargetRole"), Utils.GetRoleName(role)), gmID, Utils.ColorString(Utils.GetRoleColor(CustomRoles.GuessMaster), GetString("GuessMasterTitle")));
                    }, 1f, "GuessMaster Target Role");

                }
            }
        }
    }
}