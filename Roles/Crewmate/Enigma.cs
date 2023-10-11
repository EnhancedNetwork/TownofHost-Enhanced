using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using static Logger;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate
{
    public class Enigma
    {
        private static readonly int Id = 8460;
        private static List<byte> playerIdList = new();
        private static Dictionary<byte, List<EnigmaClue>> ShownClues = new();

        public static OptionItem EnigmaClueStage1Tasks;
        public static OptionItem EnigmaClueStage2Tasks;
        public static OptionItem EnigmaClueStage3Tasks;
        public static OptionItem EnigmaClueStage2Probability;
        public static OptionItem EnigmaClueStage3Probability;
        public static OptionItem EnigmaGetCluesWithoutReporting;

        public static Dictionary<byte, string> MsgToSend = new();
        public static Dictionary<byte, string> MsgToSendTitle = new();

        private static List<EnigmaClue> EnigmaClues = new List<EnigmaClue>
        {
            new EnigmaHatClue { ClueStage = 1, EnigmaClueType = EnigmaClueType.HatClue },
            new EnigmaHatClue { ClueStage = 3, EnigmaClueType = EnigmaClueType.HatClue },
            new EnigmaVisorClue { ClueStage = 1, EnigmaClueType = EnigmaClueType.VisorClue },
            new EnigmaVisorClue { ClueStage = 3, EnigmaClueType = EnigmaClueType.VisorClue },
            new EnigmaSkinClue { ClueStage = 1, EnigmaClueType = EnigmaClueType.SkinClue },
            new EnigmaSkinClue { ClueStage = 3, EnigmaClueType = EnigmaClueType.SkinClue },
            new EnigmaPetClue { ClueStage = 1, EnigmaClueType = EnigmaClueType.PetClue },
            new EnigmaPetClue { ClueStage = 3, EnigmaClueType = EnigmaClueType.PetClue },
            new EnigmaNameClue { ClueStage = 1, EnigmaClueType = EnigmaClueType.NameClue },
            new EnigmaNameClue { ClueStage = 2, EnigmaClueType = EnigmaClueType.NameClue },
            new EnigmaNameClue { ClueStage = 3, EnigmaClueType = EnigmaClueType.NameClue },
            new EnigmaNameLengthClue { ClueStage = 1, EnigmaClueType = EnigmaClueType.NameLengthClue },
            new EnigmaNameLengthClue { ClueStage = 2, EnigmaClueType = EnigmaClueType.NameLengthClue },
            new EnigmaNameLengthClue { ClueStage = 3, EnigmaClueType = EnigmaClueType.NameLengthClue },
            new EnigmaColorClue { ClueStage = 1, EnigmaClueType = EnigmaClueType.ColorClue },
            new EnigmaColorClue { ClueStage = 3, EnigmaClueType = EnigmaClueType.ColorClue },
            new EnigmaLocationClue { ClueStage = 2, EnigmaClueType = EnigmaClueType.LocationClue },
            new EnigmaKillerStatusClue { ClueStage = 1, EnigmaClueType = EnigmaClueType.KillerStatusClue },
            new EnigmaKillerRoleClue { ClueStage = 1, EnigmaClueType = EnigmaClueType.KillerRoleClue },
            new EnigmaKillerRoleClue { ClueStage = 2, EnigmaClueType = EnigmaClueType.KillerRoleClue },
            new EnigmaKillerLevelClue { ClueStage = 1, EnigmaClueType = EnigmaClueType.KillerLevelClue },
            new EnigmaKillerLevelClue { ClueStage = 2, EnigmaClueType = EnigmaClueType.KillerLevelClue },
            new EnigmaKillerLevelClue { ClueStage = 3, EnigmaClueType = EnigmaClueType.KillerLevelClue },
            new EnigmaFriendCodeClue { ClueStage = 3, EnigmaClueType = EnigmaClueType.FriendCodeClue },
        };

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Enigma);
            EnigmaClueStage1Tasks = FloatOptionItem.Create(Id + 11, "EnigmaClueStage1Tasks", new(0f, 10f, 1f), 1f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Enigma])
                .SetValueFormat(OptionFormat.Times);
            EnigmaClueStage2Tasks = FloatOptionItem.Create(Id + 12, "EnigmaClueStage2Tasks", new(0f, 10f, 1f), 3f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Enigma])
                .SetValueFormat(OptionFormat.Times);
            EnigmaClueStage3Tasks = FloatOptionItem.Create(Id + 13, "EnigmaClueStage3Tasks", new(0f, 10f, 1f), 6f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Enigma])
                .SetValueFormat(OptionFormat.Times);
            EnigmaClueStage2Probability = IntegerOptionItem.Create(Id + 14, "EnigmaClueStage2Probability", new(0, 100, 5), 80, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Enigma])
                .SetValueFormat(OptionFormat.Percent);
            EnigmaClueStage3Probability = IntegerOptionItem.Create(Id + 15, "EnigmaClueStage3Probability", new(0, 100, 5), 80, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Enigma])
                .SetValueFormat(OptionFormat.Percent);
            EnigmaGetCluesWithoutReporting = BooleanOptionItem.Create(Id + 16, "EnigmaClueGetCluesWithoutReporting", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Enigma]);

            OverrideTasksData.Create(Id + 20, TabGroup.CrewmateRoles, CustomRoles.Enigma);
        }
        public static void Init()
        {
            playerIdList = new();
            ShownClues = new();
            MsgToSend = new();
            MsgToSendTitle = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            ShownClues.Add(playerId, new List<EnigmaClue>());
        }
        public static bool IsEnable => playerIdList.Count > 0;

        public static void OnReportDeadBody(PlayerControl player, GameData.PlayerInfo targetInfo)
        {
            if (targetInfo == null || !IsEnable) return;

            var target = Utils.GetPlayerById(targetInfo.PlayerId);
            if (target == null) return;

            PlayerControl killer = target.GetRealKiller();
            if (killer == null) return;

            string title;
            string msg;
            var rd = IRandom.Instance;

            foreach (var playerId in playerIdList)
            {
                if (!EnigmaGetCluesWithoutReporting.GetBool() && playerId != player.PlayerId) continue;

                var enigmaPlayer = Utils.GetPlayerById(playerId);
                if (enigmaPlayer == null) continue;

                int tasksCompleted = enigmaPlayer.GetPlayerTaskState().CompletedTasksCount;
                int stage = 0;
                bool showStageClue = false;

                if (tasksCompleted >= EnigmaClueStage3Tasks.GetInt())
                {
                    stage = 3;
                    showStageClue = rd.Next(0, 100) < EnigmaClueStage3Probability.GetInt();
                }
                else if (tasksCompleted >= EnigmaClueStage2Tasks.GetInt())
                {
                    stage = 2;
                    showStageClue = rd.Next(0, 100) < EnigmaClueStage2Probability.GetInt();
                }
                else if (tasksCompleted >= EnigmaClueStage1Tasks.GetInt())
                    stage = 1;

                var clues = EnigmaClues.Where(a => a.ClueStage <= stage &&
                    !ShownClues[playerId].Any(b => b.EnigmaClueType == a.EnigmaClueType && b.ClueStage == a.ClueStage))
                    .ToList();
                if (clues.Count == 0) continue;
                if (showStageClue && clues.Any(a => a.ClueStage == stage))
                    clues = clues.Where(a => a.ClueStage == stage).ToList();

                EnigmaClue clue = clues[rd.Next(0, clues.Count)];
                title = clue.Title;
                msg = clue.GetMessage(killer, showStageClue);

                ShownClues[playerId].Add(clue);

                if (MsgToSend.ContainsKey(playerId))
                {
                    MsgToSend[playerId] = msg;
                    MsgToSendTitle[playerId] = title;
                }
                else
                {
                    MsgToSend.Add(playerId, msg);
                    MsgToSendTitle.Add(playerId, title);
                }
            }
        }

        private abstract class EnigmaClue
        {
            public int ClueStage { get; set; }
            public EnigmaClueType EnigmaClueType { get; set; }

            public abstract string Title { get; }
            public abstract string GetMessage(PlayerControl killer, bool showStageClue);
        }
        private class EnigmaHatClue : EnigmaClue
        {
            public override string Title { get { return GetString("EnigmaClueHatTitle"); } }

            public override string GetMessage(PlayerControl killer, bool showStageClue)
            {
                var killerOutfit = Camouflage.PlayerSkins[killer.PlayerId];
                if (killerOutfit.HatId == "hat_EmptyHat")
                    return GetString("EnigmaClueHat2");

                switch (this.ClueStage)
                {
                    case 1:
                    case 2:
                        return GetString("EnigmaClueHat1");
                    case 3:
                        if (showStageClue)
                            return string.Format(GetString("EnigmaClueHat3"), killerOutfit.HatId);
                        return GetString("EnigmaClueHat1");
                }

                return null;
            }
        }
        private class EnigmaVisorClue : EnigmaClue
        {
            public override string Title { get { return GetString("EnigmaClueVisorTitle"); } }

            public override string GetMessage(PlayerControl killer, bool showStageClue)
            {
                var killerOutfit = Camouflage.PlayerSkins[killer.PlayerId];
                if (killerOutfit.VisorId == "visor_EmptyVisor")
                    return GetString("EnigmaClueVisor2");

                switch (this.ClueStage)
                {
                    case 1:
                    case 2:
                        return GetString("EnigmaClueVisor1");
                    case 3:
                        if (showStageClue)
                            return string.Format(GetString("EnigmaClueVisor3"), killerOutfit.VisorId);
                        return GetString("EnigmaClueVisor1");
                }

                return null;
            }
        }
        private class EnigmaSkinClue : EnigmaClue
        {
            public override string Title { get { return GetString("EnigmaClueSkinTitle"); } }

            public override string GetMessage(PlayerControl killer, bool showStageClue)
            {
                var killerOutfit = Camouflage.PlayerSkins[killer.PlayerId];
                if (killerOutfit.SkinId == "skin_EmptySkin")
                    return GetString("EnigmaClueSkin2");

                switch (this.ClueStage)
                {
                    case 1:
                    case 2:
                        return GetString("EnigmaClueSkin1");
                    case 3:
                        if (showStageClue)
                            return string.Format(GetString("EnigmaClueSkin3"), killerOutfit.SkinId);
                        return GetString("EnigmaClueSkin1");
                }

                return null;
            }
        }
        private class EnigmaPetClue : EnigmaClue
        {
            public override string Title { get { return GetString("EnigmaCluePetTitle"); } }

            public override string GetMessage(PlayerControl killer, bool showStageClue)
            {
                var killerOutfit = Camouflage.PlayerSkins[killer.PlayerId];
                if (killerOutfit.PetId == "pet_EmptyPet")
                    return GetString("EnigmaCluePet2");

                switch (this.ClueStage)
                {
                    case 1:
                    case 2:
                        return GetString("EnigmaCluePet1");
                    case 3:
                        if (showStageClue)
                            return string.Format(GetString("EnigmaCluePet3"), killerOutfit.PetId);
                        return GetString("EnigmaCluePet1");
                }

                return null;
            }
        }
        private class EnigmaNameClue : EnigmaClue
        {
            private IRandom rd = IRandom.Instance;

            public override string Title { get { return GetString("EnigmaClueNameTitle"); } }

            private static List<string> Letters = new List<string>
            {
                "a","b","c","d","e","f","g","h","i","j","k","l","m","n","o","p","q","r","s","t","u","v","w","x","y","z"
            };

            public override string GetMessage(PlayerControl killer, bool showStageClue)
            {
                string killerName = killer.GetRealName();
                string letter = killerName[rd.Next(0, killerName.Length - 1)].ToString().ToLower();

                switch (this.ClueStage)
                {
                    case 1:
                        return GetStage1Clue(letter);
                    case 2:
                        if (showStageClue) GetStage2Clue(letter);
                        return GetStage1Clue(letter);
                    case 3:
                        if (showStageClue) GetStage3Clue(killerName, letter);
                        if (rd.Next(0, 100) < Enigma.EnigmaClueStage2Probability.GetInt()) GetStage2Clue(letter);
                        return GetStage1Clue(letter);
                }

                return null;
            }

            private string GetStage1Clue(string letter)
            {
                string randomLetter = GetRandomLetter(letter);
                int random = rd.Next(1, 2);
                if (random == 1)
                    return string.Format(GetString("EnigmaClueName1"), letter, randomLetter);
                else
                    return string.Format(GetString("EnigmaClueName1"), randomLetter, letter);
            }

            private string GetStage2Clue(string letter)
            {
                return string.Format(GetString("EnigmaClueName2"), letter);
            }

            private string GetStage3Clue(string killerName, string letter)
            {
                string letter2 = string.Empty;
                string tmpName = killerName.Replace(letter, string.Empty);
                if (!string.IsNullOrEmpty(tmpName))
                {
                    letter2 = tmpName[rd.Next(0, tmpName.Length - 1)].ToString().ToLower();
                }

                return string.Format(GetString("EnigmaClueName3"), letter, letter2);
            }

            private string GetRandomLetter(string letter)
            {
                return Letters.Where(a => a != letter).ToArray()[rd.Next(0, Letters.Count - 2)];
            }
        }
        private class EnigmaNameLengthClue : EnigmaClue
        {
            private IRandom rd = IRandom.Instance;

            public override string Title { get { return GetString("EnigmaClueNameLengthTitle"); } }

            public override string GetMessage(PlayerControl killer, bool showStageClue)
            {
                int length = killer.GetRealName().Length;

                switch (this.ClueStage)
                {
                    case 1:
                        return GetStage1Clue(length);
                    case 2:
                        if (showStageClue) return GetStage2Clue(length);
                        return GetStage1Clue(length);
                    case 3:
                        if (showStageClue) return GetStage3Clue(length);
                        if (rd.Next(0, 100) < Enigma.EnigmaClueStage2Probability.GetInt()) return GetStage2Clue(length);
                        return GetStage1Clue(length);
                }

                return null;
            }

            private string GetStage1Clue(int length)
            {
                int start = length - rd.Next(2, 3);
                int end = length + rd.Next(2, 3);

                start = start < 0 ? 0 : start;
                end = end > 8 ? 8 : end;

                return string.Format(GetString("EnigmaClueNameLength1"), start, end);
            }

            private string GetStage2Clue(int length)
            {
                int start = length - rd.Next(1, 2);
                int end = length + rd.Next(1, 2);

                start = start < 0 ? 0 : start;
                end = end > 8 ? 8 : end;

                return string.Format(GetString("EnigmaClueNameLength1"), start, end);
            }

            private string GetStage3Clue(int length)
            {
                return string.Format(GetString("EnigmaClueNameLength2"), length);
            }
        }
        private class EnigmaColorClue : EnigmaClue
        {
            public override string Title { get { return GetString("EnigmaClueColorTitle"); } }

            public override string GetMessage(PlayerControl killer, bool showStageClue)
            {
                var killerOutfit = Camouflage.PlayerSkins[killer.PlayerId];

                switch (this.ClueStage)
                {
                    case 1:
                    case 2:
                        return GetStage1Clue(killerOutfit.ColorId);
                    case 3:
                        if (showStageClue) return string.Format(GetString("EnigmaClueColor3"), killer.Data.ColorName);
                        return GetStage1Clue(killerOutfit.ColorId);
                }

                return GetStage1Clue(killerOutfit.ColorId);
            }

            private string GetStage1Clue(int colorId)
            {
                switch (colorId)
                {
                    case 0:
                    case 3:
                    case 4:
                    case 5:
                    case 7:
                    case 10:
                    case 11:
                    case 13:
                    case 14:
                    case 17:
                        return GetString("EnigmaClueColor1");
                    case 1:
                    case 2:
                    case 6:
                    case 8:
                    case 9:
                    case 12:
                    case 15:
                    case 16:
                        return GetString("EnigmaClueColor2");
                }

                return null;
            }
        }
        private class EnigmaLocationClue : EnigmaClue
        {
            public override string Title { get { return GetString("EnigmaClueLocationTitle"); } }

            public override string GetMessage(PlayerControl killer, bool showStageClue)
            {
                string room = string.Empty;
                var targetRoom = Main.PlayerStates[killer.PlayerId].LastRoom;
                if (targetRoom == null) room += GetString("FailToTrack");
                else room += GetString(targetRoom.RoomId.ToString());
                return string.Format(GetString("EnigmaClueLocation"), room);
            }
        }
        private class EnigmaKillerStatusClue : EnigmaClue
        {
            public override string Title { get { return GetString("EnigmaClueStatusTitle"); } }

            public override string GetMessage(PlayerControl killer, bool showStageClue)
            {
                if (killer.inVent)
                    return GetString("EnigmaClueStatus1");
                if (killer.onLadder)
                    return GetString("EnigmaClueStatus2");
                if (killer.Data.IsDead)
                    return GetString("EnigmaClueStatus3");
                return GetString("EnigmaClueStatus4");
            }
        }
        private class EnigmaKillerRoleClue : EnigmaClue
        {
            public override string Title { get { return GetString("EnigmaClueRoleTitle"); } }

            public override string GetMessage(PlayerControl killer, bool showStageClue)
            {
                CustomRoles role = killer.GetCustomRole();
                switch (this.ClueStage)
                {
                    case 1:
                        if (role.IsImpostor()) return GetString("EnigmaClueRole1");
                        if (role.IsNeutral()) return GetString("EnigmaClueRole2");
                        return GetString("EnigmaClueRole3");
                    case 2:
                        if (showStageClue) return string.Format(GetString("EnigmaClueRole4"), killer.GetDisplayRoleName());
                        if (role.IsImpostor()) return GetString("EnigmaClueRole1");
                        if (role.IsNeutral()) return GetString("EnigmaClueRole2");
                        return GetString("EnigmaClueRole3");
                }

                return null;
            }
        }
        private class EnigmaKillerLevelClue : EnigmaClue
        {
            private IRandom rd = IRandom.Instance;

            public override string Title { get { return GetString("EnigmaClueLevelTitle"); } }

            public override string GetMessage(PlayerControl killer, bool showStageClue)
            {
                int level = (int)killer.Data.PlayerLevel;

                switch (this.ClueStage)
                {
                    case 1:
                        return GetStage1Clue(level);
                    case 2:
                        if (showStageClue) return GetStage2Clue(level);
                        return GetStage1Clue(level);
                    case 3:
                        if (showStageClue) return GetStage3Clue(level);
                        if (rd.Next(0, 100) < Enigma.EnigmaClueStage2Probability.GetInt()) return GetStage2Clue(level);
                        return GetStage1Clue(level);
                }

                return null;
            }

            private string GetStage1Clue(int level)
            {
                if (level > 50) return GetString("EnigmaClueLevel1");
                return GetString("EnigmaClueLevel2");
            }

            private string GetStage2Clue(int level)
            {
                int rangeStart = level - 15;
                int rangeEnd = level + 15;
                return string.Format(GetString("EnigmaClueLevel3"), rangeStart, rangeEnd >= 100 ? 100 : rangeEnd);
            }

            private string GetStage3Clue(int level)
            {
                return string.Format(GetString("EnigmaClueLevel4"), level);
            }
        }
        private class EnigmaFriendCodeClue : EnigmaClue
        {
            public override string Title { get { return GetString("EnigmaClueFriendCodeTitle"); } }

            public override string GetMessage(PlayerControl killer, bool showStageClue)
            {
                string friendcode = killer.Data.FriendCode;
                return string.Format(GetString("EnigmaClueFriendCode"), friendcode);
            }
        }

        private enum EnigmaClueType
        {
            HatClue,
            VisorClue,
            SkinClue,
            PetClue,
            NameClue,
            NameLengthClue,
            ColorClue,
            LocationClue,
            KillerStatusClue,
            KillerRoleClue,
            KillerLevelClue,
            FriendCodeClue
            //SecurityClue,
            //SabotageClue,
            //RandomClue
        }
    }
}