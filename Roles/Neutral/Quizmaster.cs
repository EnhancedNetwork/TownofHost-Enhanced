using Hazel;
using System.Collections.Generic;
using System;
using System.Linq;
using TOHE.Modules;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Neutral;
using static TOHE.Options;
using static TOHE.Translator;
using AsmResolver.Collections;

namespace TOHE.Roles.Neutral
{
    public class Quizmaster
    {
        public static PlayerControl Player;
        public static OptionItem QuestionDifficulty;
        public static QuizQuestionBase Question = null;
        public static List<byte> playerIdList = new();
        public static Sabotages lastSabotage = Sabotages.None;
        public static Sabotages firstSabotageOfRound = Sabotages.None;
        public static PlayerColors lastExiledColor = PlayerColors.None;
        public static readonly int Id = 26400;
        public static bool IsEnable = false;
        public static bool AlreadyMarked = false;
        public static byte MarkedPlayer = byte.MaxValue;
        public static void SetupCustomOption()
        {
            SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Quizmaster, 1);
            QuestionDifficulty = IntegerOptionItem.Create(Id + 10, "QuizmasterQuestionDifficulty", new(1, 5, 1), 1, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Quizmaster]);
        }
        public static void Init()
        {
            playerIdList = new();
            MarkedPlayer = byte.MaxValue;
            Player = null;
            IsEnable = false;
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            MarkedPlayer = byte.MaxValue;
            Player = Utils.GetPlayerByRole(CustomRoles.Quizmaster);
            firstSabotageOfRound = Sabotages.None;
            IsEnable = true;
        }
        private static void SendRPC(byte playerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetMarkedPlayer, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(MarkedPlayer);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            byte playerId = reader.ReadByte();
            byte targetId = reader.ReadByte();

            if (targetId != byte.MaxValue)
                MarkedPlayer = targetId;
        }
        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = 30;
        public static bool CanUseKillButton(PlayerControl pc)
        {
            if (pc == null || !pc.IsAlive()) return false;
            return MarkedPlayer == byte.MaxValue && !AlreadyMarked;
        }

        public static bool CanUseVentButton(PlayerControl pc)
        {
            if (pc == null || !pc.IsAlive()) return false;
            return MarkedPlayer == byte.MaxValue;
        }

        public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            AlreadyMarked = true;
            MarkedPlayer = target.PlayerId;
            Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);

            SendRPC(killer.PlayerId);
                killer.ResetKillCooldown();
                killer.SetKillCooldown();
                killer.SyncSettings();
                killer.RPCPlayCustomSound("Clothe");
            return false;
        }

        static QuizQuestionBase GetRandomQuestion(List<QuizQuestionBase> qt)
        {
            List<QuizQuestionBase> questions = qt.Where(a => a.Stage < QuestionDifficulty.GetInt()).ToList();
            var rnd = IRandom.Instance;
            QuizQuestionBase question = questions[rnd.Next(0, questions.Count)];
            question.FixUnsetAnswers();
            return question;
        }

        public static void OnReportDeadBody(PlayerControl player, GameData.PlayerInfo targetInfo)
        {
            Player = Utils.GetPlayerByRole(CustomRoles.Quizmaster);
            if (MarkedPlayer != byte.MaxValue)
            {
                List<QuizQuestionBase> Questions = new List<QuizQuestionBase>
                {
                    new SabotageQuestion { Stage = 1, Question = "LastSabotage",/* JSON ENTRIES */ QuizmasterQuestionType = QuizmasterQuestionType.LatestSabotageQuestion },
                    new SabotageQuestion { Stage = 1, Question = "FirstRoundSabotage", QuizmasterQuestionType = QuizmasterQuestionType.FirstRoundSabotageQuestion },
                    new EjectionQuestion { Stage = 1, Question = "LastEjectedPlayerColor", QuizmasterQuestionType = QuizmasterQuestionType.EjectionColorQuestion },
                };
                Question = GetRandomQuestion(Questions);
                _ = new LateTask(() =>
                {
                    Utils.SendMessage(GetString("QuizmasterMarkedBy").Replace("{QMCOLOR}", Utils.GetRoleColorCode(CustomRoles.Quizmaster)).Replace("{QMQUESTION}", GetString("QuizmasterQuestions." + Question.Question)), MarkedPlayer, GetString("QuizmasterChatTitle"));
                    Utils.SendMessage(GetString("QuizmasterAnswers").Replace("{QMA}", GetString(Question.Answers[0])).Replace("{QMB}", GetString(Question.Answers[1])).Replace("{QMC}", GetString(Question.Answers[2])), MarkedPlayer, GetString("QuizmasterChatTitle"));
                    Utils.SendMessage(GetString("QuizmasterMarked").Replace("{QMTARGET}", Utils.GetPlayerById(MarkedPlayer).GetRealName()), Player.PlayerId, GetString("QuizmasterChatTitle"));
                }, 5.1f, "Quizmaster Chat Notice");
            }
        }

        public static void OnMeetingEnd() /* NEW ROUND START */
        {
            firstSabotageOfRound = Sabotages.None;
            if (MarkedPlayer != byte.MaxValue)
                KillPlayer(Utils.GetPlayerById(MarkedPlayer));

            ResetMarkedPlayer();
        }

        public static void ResetMarkedPlayer()
        {
            AlreadyMarked = false;
            MarkedPlayer = byte.MaxValue;
        }

        public static void OnPlayerDead(PlayerControl target)
        {
            if (target.PlayerId == MarkedPlayer) MarkedPlayer = byte.MaxValue;
        }

        public static string TargetMark(PlayerControl seer, PlayerControl target)
            => (target.PlayerId == MarkedPlayer) ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Quizmaster), " ?!") : "";

        public static void OnSabotageCall(SystemTypes systemType)
        {
            switch (systemType)
            {
                case SystemTypes.HeliSabotage:
                case SystemTypes.Laboratory:
                case SystemTypes.Reactor:
                    lastSabotage = Sabotages.Reactor;
                    break;
                case SystemTypes.Electrical:
                    lastSabotage = Sabotages.Lights;
                    break;
                case SystemTypes.LifeSupp:
                    lastSabotage = Sabotages.O2;
                    break;
                case SystemTypes.Comms:
                    lastSabotage = Sabotages.Communications;
                    break;
                case SystemTypes.MushroomMixupSabotage:
                    lastSabotage = Sabotages.MushroomMixup;
                    break;
            }

            if (firstSabotageOfRound == Sabotages.None)
                firstSabotageOfRound = lastSabotage;
        }

        public static void KillPlayer(PlayerControl plrToKill)
        {
            plrToKill.Data.IsDead = true;
            Main.PlayerStates[plrToKill.PlayerId].deathReason = PlayerState.DeathReason.WrongAnswer;
            Main.PlayerStates[plrToKill.PlayerId].SetDead();
            plrToKill.RpcExileV2();
            ResetMarkedPlayer();
        }
    }

    abstract public class QuizQuestionBase
    {
        public int Stage { get; set; }
        public QuizmasterQuestionType QuizmasterQuestionType { get; set; }

        public string Question { get; set;  }
        public string Answer { get; set; }
        public string AnswerLetter { get; set; }
        public List<string> Answers { get; set; }
        public abstract void FixUnsetAnswers();
    }

    class EjectionQuestion : QuizQuestionBase
    {

        public override void FixUnsetAnswers()
        {
            Answers = new List<string>{ };
            List<string> PosibleAnswers = new List<string> { };

            foreach (PlayerControl plr in Main.AllPlayerControls)
            {
                PosibleAnswers.Add(plr.Data.GetPlayerColorString());
            }


            var rnd = IRandom.Instance;
            int positionForRightAnswer = rnd.Next(0, 3);

            if (QuizmasterQuestionType == QuizmasterQuestionType.EjectionColorQuestion)
                Answer = Quizmaster.lastExiledColor.ToString();

            PosibleAnswers.Remove(Answer);
            for (int numOfQuestionsDone = 0; numOfQuestionsDone < 3; numOfQuestionsDone++)
            {
                if (numOfQuestionsDone == positionForRightAnswer)
                {
                    AnswerLetter = new List<string> { "A", "B", "C" }[positionForRightAnswer];
                    Answers.Add("QuizmasterSabotages." + Answer);
                }
                else
                {
                    string thatAnswer = PosibleAnswers[rnd.Next(0, PosibleAnswers.Count)];
                    Answers.Add("QuizmasterSabotages." + thatAnswer);
                    PosibleAnswers.Remove(thatAnswer);
                }
            }
        }
    }

    class SabotageQuestion : QuizQuestionBase
    {
        private static List<Sabotages> AirshitSabotages = new List<Sabotages> { Sabotages.None, Sabotages.Lights, Sabotages.Reactor, Sabotages.Communications };
        private static List<Sabotages> SkeldSabotages = new List<Sabotages> { Sabotages.None, Sabotages.Lights, Sabotages.Reactor, Sabotages.O2 };
        private static List<Sabotages> PolusSabotages = new List<Sabotages> { Sabotages.None, Sabotages.Lights, Sabotages.Reactor, Sabotages.Communications };
        private static List<Sabotages> MiraSabotages = new List<Sabotages> { Sabotages.None, Sabotages.Lights, Sabotages.Reactor, Sabotages.O2, Sabotages.Communications };
        private static List<Sabotages> FungleSabotages = new List<Sabotages> { Sabotages.None, Sabotages.Communications, Sabotages.Reactor, Sabotages.MushroomMixup };

        public override void FixUnsetAnswers()
        {
            Answers = new List<string> { };
            List<string> PosibleAnswers = (MapNames)Main.NormalOptions.MapId switch
            {
                MapNames.Skeld => SkeldSabotages.ConvertAll(f => f.ToString()),
                MapNames.Dleks => SkeldSabotages.ConvertAll(f => f.ToString()),
                MapNames.Mira => MiraSabotages.ConvertAll(f => f.ToString()),
                MapNames.Polus => PolusSabotages.ConvertAll(f => f.ToString()),
                MapNames.Airship => AirshitSabotages.ConvertAll(f => f.ToString()),
                MapNames.Fungle => FungleSabotages.ConvertAll(f => f.ToString()),
                _ => throw new NotImplementedException(),
            };


            var rnd = IRandom.Instance;
            int positionForRightAnswer = rnd.Next(0, 3);

            if (QuizmasterQuestionType == QuizmasterQuestionType.LatestSabotageQuestion)
                Answer = Quizmaster.lastSabotage.ToString();
            else if (QuizmasterQuestionType == QuizmasterQuestionType.FirstRoundSabotageQuestion)
                Answer = Quizmaster.firstSabotageOfRound.ToString();

            PosibleAnswers.Remove(Answer);
            for (int numOfQuestionsDone = 0; numOfQuestionsDone < 3; numOfQuestionsDone++)
            {
                if (numOfQuestionsDone == positionForRightAnswer)
                {
                    AnswerLetter = new List<string> { "A", "B", "C" }[positionForRightAnswer];
                    Answers.Add("QuizmasterSabotages." + Answer);
                }
                else
                {
                    string thatAnswer = PosibleAnswers[rnd.Next(0, PosibleAnswers.Count)];
                    Answers.Add("QuizmasterSabotages." + thatAnswer);
                    PosibleAnswers.Remove(thatAnswer);
                }
            }
        }
    }

    public enum QuizmasterQuestionType
    {
        FirstRoundSabotageQuestion,
        LatestSabotageQuestion,
        EjectionColorQuestion,
        ReportQuestion,
        MeetingQuestion,
        BasisQuestion,
    }

    public enum Sabotages
    {
        None = -1,

        Lights,
        Reactor,
        O2,
        Communications,
        MushroomMixup
    }

    public enum PlayerColors
    {
        None = -1,

        Red,
        Blue,
        Green,
        Pink,
        Orange,
        Yellow,
        Black,
        White,
        Purple,
        Brown,
        Cyan,
        Lime,
        Maroon,
        Rose,
        Banana,
        Gray,
        Tan,
        Coral
    }
}