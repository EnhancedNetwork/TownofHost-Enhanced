using System.Collections.Generic;
using System.Linq;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.MeetingHudStartPatch;

namespace TOHE.Roles.Crewmate
{
    internal class Enigma : RoleBase
    {
        private static readonly int Id = 8100;
        private static List<byte> playerIdList = [];
        public static bool On = false;
        public override bool IsEnable => On;
        public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;

        private static Dictionary<byte, List<EnigmaClue>> ShownClues = [];

        public static OptionItem EnigmaClueStage1Tasks;
        public static OptionItem EnigmaClueStage2Tasks;
        public static OptionItem EnigmaClueStage3Tasks;
        public static OptionItem EnigmaClueStage2Probability;
        public static OptionItem EnigmaClueStage3Probability;
        public static OptionItem EnigmaGetCluesWithoutReporting;

        public static Dictionary<byte, string> MsgToSend = [];
        public static Dictionary<byte, string> MsgToSendTitle = [];

        private static readonly List<EnigmaClue> EnigmaClues =
        [
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
        ];

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Enigma);
            EnigmaClueStage1Tasks = FloatOptionItem.Create(Id + 11, "EnigmaClueStage1Tasks", new(0f, 10f, 1f), 1f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Enigma])
                .SetValueFormat(OptionFormat.Times);
            EnigmaClueStage2Tasks = FloatOptionItem.Create(Id + 12, "EnigmaClueStage2Tasks", new(0f, 10f, 1f), 3f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Enigma])
                .SetValueFormat(OptionFormat.Times);
            EnigmaClueStage3Tasks = FloatOptionItem.Create(Id + 13, "EnigmaClueStage3Tasks", new(0f, 10f, 1f), 7f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Enigma])
                .SetValueFormat(OptionFormat.Times);
            EnigmaClueStage2Probability = IntegerOptionItem.Create(Id + 14, "EnigmaClueStage2Probability", new(0, 100, 5), 75, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Enigma])
                .SetValueFormat(OptionFormat.Percent);
            EnigmaClueStage3Probability = IntegerOptionItem.Create(Id + 15, "EnigmaClueStage3Probability", new(0, 100, 5), 60, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Enigma])
                .SetValueFormat(OptionFormat.Percent);
            EnigmaGetCluesWithoutReporting = BooleanOptionItem.Create(Id + 16, "EnigmaClueGetCluesWithoutReporting", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Enigma]);

            OverrideTasksData.Create(Id + 20, TabGroup.CrewmateRoles, CustomRoles.Enigma);
        }
        public override void Init()
        {
            playerIdList = [];
            On = false;
            ShownClues = [];
            MsgToSend = [];
            MsgToSendTitle = [];
        }
        public override void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            On = true;
            ShownClues.Add(playerId, []);
        }
        public override void Remove(byte playerId)
        {
            playerIdList.Remove(playerId);
            ShownClues.Remove(playerId);
        }

        public override void OnReportDeadBody(PlayerControl player, PlayerControl target)
        {

            if (target == null) return;

            PlayerControl killer = target.GetRealKiller();
            if (killer == null) return;

            string title;
            string msg;
            var rd = IRandom.Instance;

            foreach (var playerId in playerIdList.ToArray())
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
                    MsgToSend[playerId] = msg;
                else
                    MsgToSend.Add(playerId, msg);

                if (MsgToSendTitle.ContainsKey(playerId))
                    MsgToSendTitle[playerId] = title;
                else
                    MsgToSendTitle.Add(playerId, title);
            }
        }

        public override void OnMeetingHudStart(PlayerControl pc)
        {
            if (MsgToSend.ContainsKey(pc.PlayerId))
                AddMsg(MsgToSend[pc.PlayerId], pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Enigma), Enigma.MsgToSendTitle[pc.PlayerId]));
        }
        public override void MeetingHudClear() => MsgToSend = [];
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
            private readonly IRandom rd = IRandom.Instance;

            public override string Title { get { return GetString("EnigmaClueNameTitle"); } }

            public override string GetMessage(PlayerControl killer, bool showStageClue)
            {
                string killerName = killer.GetRealName();
                string letter = killerName[rd.Next(0, killerName.Length)].ToString().ToLower();

                switch (this.ClueStage)
                {
                    case 1:
                        return GetStage1Clue(killer, letter);
                    case 2:
                        if (showStageClue) GetStage2Clue(letter);
                        return GetStage1Clue(killer, letter);
                    case 3:
                        if (showStageClue) GetStage3Clue(killerName, letter);
                        if (rd.Next(0, 100) < Enigma.EnigmaClueStage2Probability.GetInt()) GetStage2Clue(letter);
                        return GetStage1Clue(killer, letter);
                }

                return null;
            }

            private string GetStage1Clue(PlayerControl killer, string letter)
            {
                string randomLetter = GetRandomLetter(killer, letter);
                int random = rd.Next(1, 2);
                if (random == 1)
                    return string.Format(GetString("EnigmaClueName1"), letter, randomLetter);
                else
                    return string.Format(GetString("EnigmaClueName1"), randomLetter, letter);
            }

            private static string GetStage2Clue(string letter)
            {
                return string.Format(GetString("EnigmaClueName2"), letter);
            }

            private string GetStage3Clue(string killerName, string letter)
            {
                string letter2 = string.Empty;
                string tmpName = killerName.Replace(letter, string.Empty);
                if (!string.IsNullOrEmpty(tmpName))
                {
                    letter2 = tmpName[rd.Next(0, tmpName.Length)].ToString().ToLower();
                }

                return string.Format(GetString("EnigmaClueName3"), letter, letter2);
            }

            private string GetRandomLetter(PlayerControl killer, string letter)
            {
                var alivePlayers = Main.AllAlivePlayerControls.Where(a => a.PlayerId != killer.PlayerId).ToList();
                var rndPlayer = alivePlayers[rd.Next(0, alivePlayers.Count)];
                string rndPlayerName = rndPlayer.GetRealName().Replace(letter, "");
                string letter2 = rndPlayerName[rd.Next(0, rndPlayerName.Length)].ToString().ToLower();
                return letter2;
            }
        }
        private class EnigmaNameLengthClue : EnigmaClue
        {
            private readonly IRandom rd = IRandom.Instance;

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
                start = start >= 8 ? 6 : start;
                end = end > 8 ? 8 : end;

                return string.Format(GetString("EnigmaClueNameLength1"), start, end);
            }

            private string GetStage2Clue(int length)
            {
                int start = length - rd.Next(1, 2);
                int end = length + rd.Next(1, 2);

                start = start < 0 ? 0 : start;
                start = start >= 8 ? 7 : start;
                end = end > 8 ? 8 : end;

                return string.Format(GetString("EnigmaClueNameLength1"), start, end);
            }

            private static string GetStage3Clue(int length)
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

            private static string GetStage1Clue(int colorId)
            {
                return colorId switch
                {
                    0 or 3 or 4 or 5 or 7 or 10 or 11 or 13 or 14 or 17 => GetString("EnigmaClueColor1"),
                    1 or 2 or 6 or 8 or 9 or 12 or 15 or 16 => GetString("EnigmaClueColor2"),
                    _ => null,
                };
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
                        if (showStageClue) return string.Format(GetString("EnigmaClueRole4"), killer.GetDisplayRoleAndSubName(killer, false));
                        if (role.IsImpostor()) return GetString("EnigmaClueRole1");
                        if (role.IsNeutral()) return GetString("EnigmaClueRole2");
                        return GetString("EnigmaClueRole3");
                }

                return null;
            }
        }
        private class EnigmaKillerLevelClue : EnigmaClue
        {
            private readonly IRandom rd = IRandom.Instance;

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

            private static string GetStage1Clue(int level)
            {
                if (level > 50) return GetString("EnigmaClueLevel1");
                return GetString("EnigmaClueLevel2");
            }

            private static string GetStage2Clue(int level)
            {
                int rangeStart = level - 15;
                int rangeEnd = level + 15;
                return string.Format(GetString("EnigmaClueLevel3"), rangeStart, rangeEnd >= 100 ? 100 : rangeEnd);
            }

            private static string GetStage3Clue(int level)
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
