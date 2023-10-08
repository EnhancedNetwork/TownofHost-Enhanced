using System.Collections.Generic;
using System.Linq;
using Hazel;
using MS.Internal.Xml.XPath;
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

        public static Dictionary<byte, string> MsgToSend = new();
        public static Dictionary<byte, string> MsgToSendTitle = new();

        private static List<EnigmaClue> EnigmaClues = new List<EnigmaClue>
        {
            new EnigmaClue { ClueStage = 1, EnigmaClueType = EnigmaClueType.HatClue },
            new EnigmaClue { ClueStage = 3, EnigmaClueType = EnigmaClueType.HatClue },
            new EnigmaClue { ClueStage = 1, EnigmaClueType = EnigmaClueType.VisorClue },
            new EnigmaClue { ClueStage = 3, EnigmaClueType = EnigmaClueType.VisorClue },
            new EnigmaClue { ClueStage = 1, EnigmaClueType = EnigmaClueType.SkinClue },
            new EnigmaClue { ClueStage = 3, EnigmaClueType = EnigmaClueType.SkinClue },
            new EnigmaClue { ClueStage = 1, EnigmaClueType = EnigmaClueType.PetClue },
            new EnigmaClue { ClueStage = 3, EnigmaClueType = EnigmaClueType.PetClue },
            new EnigmaClue { ClueStage = 1, EnigmaClueType = EnigmaClueType.NameClue },
            new EnigmaClue { ClueStage = 2, EnigmaClueType = EnigmaClueType.NameClue },
            new EnigmaClue { ClueStage = 3, EnigmaClueType = EnigmaClueType.NameClue },
            new EnigmaClue { ClueStage = 1, EnigmaClueType = EnigmaClueType.NameLengthClue },
            new EnigmaClue { ClueStage = 2, EnigmaClueType = EnigmaClueType.NameLengthClue },
            new EnigmaClue { ClueStage = 3, EnigmaClueType = EnigmaClueType.NameLengthClue },
            new EnigmaClue { ClueStage = 1, EnigmaClueType = EnigmaClueType.ColorClue },
            new EnigmaClue { ClueStage = 2, EnigmaClueType = EnigmaClueType.LocationClue },
            new EnigmaClue { ClueStage = 1, EnigmaClueType = EnigmaClueType.KillerStatusClue },
            new EnigmaClue { ClueStage = 1, EnigmaClueType = EnigmaClueType.KillerRoleClue },
            new EnigmaClue { ClueStage = 2, EnigmaClueType = EnigmaClueType.KillerRoleClue }
        };

        private static List<string> Letters = new List<string>
        {
            "a","b","c","d","e","f","g","h","i","j","k","l","m","n","o","p","q","r","s","t","u","v","w","x","y","z"
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

        public static void OnReportDeadBody(GameData.PlayerInfo targetInfo)
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
                var enigmaPlayer = Utils.GetPlayerById(playerId);
                if (enigmaPlayer == null)
                {
                    continue;
                }

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
                if (clues.Count == 0)
                    continue;
                if (showStageClue && clues.Any(a => a.ClueStage == stage))
                    clues = clues.Where(a => a.ClueStage == stage).ToList();

                EnigmaClue clue = clues[rd.Next(0, clues.Count)];

                ShownClues[playerId].Add(new EnigmaClue { ClueStage = clue.ClueStage, EnigmaClueType = clue.EnigmaClueType });

                title = GetTitleForClue(clue.EnigmaClueType);
                msg = GetMessageForClue(stage, clue.EnigmaClueType, killer, showStageClue);

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

        private static string GetMessageForClue(int stage, EnigmaClueType clueType, PlayerControl killer, bool showStageClue)
        {
            var rd = IRandom.Instance;
            GameData.PlayerOutfit killerOutfit;
            string killerName;

            switch (clueType)
            {
                case EnigmaClueType.HatClue:
                    killerOutfit = Camouflage.PlayerSkins[killer.PlayerId];
                    if (killerOutfit.HatId == "hat_EmptyHat")
                        return GetString("EnigmaClueHat2");

                    switch (stage)
                    {
                        case 1:
                        case 2:
                            return GetString("EnigmaClueHat1");
                        case 3:
                            if (showStageClue)
                                return string.Format(GetString("EnigmaClueHat3"), killerOutfit.HatId);
                            return GetString("EnigmaClueHat1");
                    }

                    break;
                case EnigmaClueType.VisorClue:
                    killerOutfit = Camouflage.PlayerSkins[killer.PlayerId];
                    if (killerOutfit.VisorId == "visor_EmptyVisor")
                        return GetString("EnigmaClueVisor2");

                    switch (stage)
                    {
                        case 1:
                        case 2:
                            return GetString("EnigmaClueVisor1");
                        case 3:
                            if (showStageClue)
                                return string.Format(GetString("EnigmaClueVisor3"), killerOutfit.VisorId);
                            return GetString("EnigmaClueVisor1");
                    }

                    break;
                case EnigmaClueType.SkinClue:
                    killerOutfit = Camouflage.PlayerSkins[killer.PlayerId];
                    if (killerOutfit.SkinId == "skin_EmptySkin")
                        return GetString("EnigmaClueSkin2");

                    switch (stage)
                    {
                        case 1:
                        case 2:
                            return GetString("EnigmaClueSkin1");
                        case 3:
                            if (showStageClue)
                                return string.Format(GetString("EnigmaClueSkin3"), killerOutfit.SkinId);
                            return GetString("EnigmaClueSkin1");
                    }

                    break;
                case EnigmaClueType.PetClue:
                    killerOutfit = Camouflage.PlayerSkins[killer.PlayerId];
                    if (killerOutfit.PetId == "pet_EmptyPet")
                        return GetString("EnigmaCluePet2");

                    switch (stage)
                    {
                        case 1:
                        case 2:
                            return GetString("EnigmaCluePet1");
                        case 3:
                            if (showStageClue)
                                return string.Format(GetString("EnigmaCluePet3"), killerOutfit.PetId);
                            return GetString("EnigmaCluePet1");
                    }

                    break;
                case EnigmaClueType.NameClue:
                    killerName = killer.GetRealName();
                    string letter = killerName[rd.Next(0, killerName.Length - 1)].ToString();
                    string letter2 = string.Empty;
                    string randomLetter;
                    int random;

                    switch (stage)
                    {
                        case 1:
                            randomLetter = Letters.Where(a => a != letter).ToArray()[rd.Next(0, Letters.Count - 2)];
                            random = rd.Next(1, 2);
                            if (random == 1)
                                return string.Format(GetString("EnigmaClueName1"), letter, randomLetter);
                            else
                                return string.Format(GetString("EnigmaClueName1"), randomLetter, letter);
                        case 2:
                            if (showStageClue)
                            {
                                return string.Format(GetString("EnigmaClueName2"), letter);
                            }

                            randomLetter = Letters.Where(a => a != letter).ToArray()[rd.Next(0, Letters.Count - 2)];
                            random = rd.Next(1, 2);
                            if (random == 1)
                                return string.Format(GetString("EnigmaClueName1"), letter, randomLetter);
                            else
                                return string.Format(GetString("EnigmaClueName1"), randomLetter, letter);
                        case 3:
                            if (showStageClue)
                            {
                                string tmpName = killerName.Replace(letter, string.Empty);
                                if (!string.IsNullOrEmpty(tmpName))
                                {
                                    letter2 = tmpName[rd.Next(0, tmpName.Length - 1)].ToString();
                                }

                                return string.Format(GetString("EnigmaClueName3"), letter, letter2);
                            }
                            if (rd.Next(0, 100) < EnigmaClueStage2Probability.GetInt())
                            {
                                return string.Format(GetString("EnigmaClueName2"), letter);
                            }

                            randomLetter = Letters.Where(a => a != letter).ToArray()[rd.Next(0, Letters.Count - 2)];
                            random = rd.Next(1, 2);
                            if (random == 1)
                                return string.Format(GetString("EnigmaClueName1"), letter, randomLetter);
                            else
                                return string.Format(GetString("EnigmaClueName1"), randomLetter, letter);
                    }

                    break;
                case EnigmaClueType.NameLengthClue:
                    killerName = killer.GetRealName();

                    int length = killerName.Length;
                    int start = 0;
                    int end = 0;

                    switch (stage)
                    {
                        case 1:
                            start = length - rd.Next(1, length) - 2;
                            end = length + rd.Next(1, length) + 2;
                            break;

                        case 2:
                            if (showStageClue)
                            {
                                start = length - rd.Next(1, length);
                                end = length + rd.Next(1, length);
                            }
                            else
                            {
                                start = length - rd.Next(1, length) - 2;
                                end = length + rd.Next(1, length) + 2;
                            }
                            break;

                        case 3:
                            if (showStageClue)
                                return string.Format(GetString("EnigmaClueNameLength2"), length);

                            if (rd.Next(0, 100) < EnigmaClueStage2Probability.GetInt())
                            {
                                start = length - rd.Next(1, length);
                                end = length + rd.Next(1, length);
                            }
                            else
                            {
                                start = length - rd.Next(1, length) - 2;
                                end = length + rd.Next(1, length) + 2;
                            }
                            break;
                    }

                    start = start < 0 ? 0 : start;
                    end = end > 8 ? 8 : end;

                    return string.Format(GetString("EnigmaClueNameLength1"), start, end);
                case EnigmaClueType.ColorClue:
                    killerOutfit = Camouflage.PlayerSkins[killer.PlayerId];

                    switch (killerOutfit.ColorId)
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

                    break;
                case EnigmaClueType.LocationClue:
                    string room = string.Empty;
                    var targetRoom = Main.PlayerStates[killer.PlayerId].LastRoom;
                    if (targetRoom == null) room += GetString("FailToTrack");
                    else room += GetString(targetRoom.RoomId.ToString());
                    return string.Format(GetString("EnigmaClueLocation"), room);
                case EnigmaClueType.KillerStatusClue:
                    if (killer.inVent)
                        return GetString("EnigmaClueStatus1");
                    if (killer.onLadder)
                        return GetString("EnigmaClueStatus2");
                    if (killer.Data.IsDead)
                        return GetString("EnigmaClueStatus3");
                    return GetString("EnigmaClueStatus4");
                case EnigmaClueType.KillerRoleClue:
                    CustomRoles role = killer.GetCustomRole();
                    switch (stage)
                    {
                        case 1:
                            if (role.IsImpostor())
                                return GetString("EnigmaClueRole1");
                            if (role.IsNeutral())
                                return GetString("EnigmaClueRole2");
                            return GetString("EnigmaClueRole3");
                        case 2:
                            if (showStageClue)
                                string.Format(GetString("EnigmaClueRole4"), killer.GetDisplayRoleName());
                            if (role.IsImpostor())
                                return GetString("EnigmaClueRole1");
                            if (role.IsNeutral())
                                return GetString("EnigmaClueRole2");
                            return GetString("EnigmaClueRole3");
                    }

                    break;
                //case EnigmaClueType.SecurityClue:
                //    break;
                //case EnigmaClueType.SabotageClue:
                //    break;
                //case EnigmaClueType.RandomClue:
                //    break;
                default:
                    break;
            }

            return string.Empty;
        }

        private static string GetTitleForClue(EnigmaClueType clueType)
        {
            switch (clueType)
            {
                case EnigmaClueType.HatClue:
                    return GetString("EnigmaClueHatTitle");
                case EnigmaClueType.SkinClue:
                    return GetString("EnigmaClueSkinTitle");
                case EnigmaClueType.PetClue:
                    return GetString("EnigmaCluePetTitle");
                case EnigmaClueType.NameClue:
                    return GetString("EnigmaClueNameTitle");
                case EnigmaClueType.NameLengthClue:
                    return GetString("EnigmaClueNameLengthTitle");
                case EnigmaClueType.ColorClue:
                    return GetString("EnigmaClueColorTitle");
                case EnigmaClueType.LocationClue:
                    return GetString("EnigmaClueLocationTitle");
                case EnigmaClueType.KillerStatusClue:
                    return GetString("EnigmaClueStatusTitle");
                case EnigmaClueType.KillerRoleClue:
                    return GetString("EnigmaClueRoleTitle");
                default:
                    return GetString("EnigmaClueTitle");
            }
        }
    }

    public class EnigmaClue
    {
        public int ClueStage { get; set; }
        public EnigmaClueType EnigmaClueType { get; set; }
    }

    public enum EnigmaClueType
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
        //SecurityClue,
        //SabotageClue,
        //RandomClue
    }
}