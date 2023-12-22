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
using Il2CppSystem.Data;

namespace TOHE.Roles.Neutral
{
    public class Quizmaster
    {
        public static PlayerControl Player;
        public static OptionItem QuestionDifficulty;
        public static OptionItem CanKillAfterMark;
        public static OptionItem CanVentAfterMark;
        public static OptionItem NumOfKillAfterMark;
        public static QuizQuestionBase Question = null;
        public static List<byte> playerIdList = new();
        public static Sabotages lastSabotage = Sabotages.None;
        public static Sabotages firstSabotageOfRound = Sabotages.None;
        public static readonly int Id = 26900;
        public static int killsForRound = 0;
        public static bool allowedKilling = false;
        public static bool allowedVenting = true;
        public static bool IsEnable = false;
        public static bool AlreadyMarked = false;
        public static byte MarkedPlayer = byte.MaxValue;
        public static string lastExiledColor = "None";
        public static string lastReportedColor = "None";
        public static string thisReportedColor = "None";
        public static string lastButtonPressedColor = "None";
        public static string thisButtonPressedColor = "None";
        public static void SetupCustomOption()
        {
            SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Quizmaster, 1);
            QuestionDifficulty = IntegerOptionItem.Create(Id + 10, "QuizmasterQuestionDifficulty", new(1, 5, 1), 1, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Quizmaster]);

            CanVentAfterMark = BooleanOptionItem.Create(Id + 11, "QuizmasterCanVentAfterMark", true, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Quizmaster]);
            CanKillAfterMark = BooleanOptionItem.Create(Id + 12, "QuizmasterCanKillAfterMark", false, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Quizmaster]);
            NumOfKillAfterMark = IntegerOptionItem.Create(Id + 13, "QuizmasterNumOfKillAfterMark", new(1, 15, 1), 1, TabGroup.NeutralRoles, false)
                .SetParent(CanKillAfterMark);
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
        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = 15;
        public static bool CanUseKillButton(PlayerControl pc)
        {
            if (pc == null || !pc.IsAlive()) return false;
            bool canKill;
            if (CanKillAfterMark.GetBool())
            {
                bool didLimit = killsForRound >= NumOfKillAfterMark.GetInt();
                canKill = !didLimit;
                allowedKilling = MarkedPlayer != byte.MaxValue;
            }
            else
            {
                allowedKilling = false;
                canKill = MarkedPlayer == byte.MaxValue;
            }
            return canKill;
        }

        public static bool CanUseVentButton(PlayerControl pc)
        {
            if (pc == null || !pc.IsAlive()) return false;
            bool canVent;
            if (CanVentAfterMark.GetBool())
            {
                canVent = true;
            }
            else
            {
                canVent = MarkedPlayer == byte.MaxValue && !allowedVenting;
            }
            return canVent;
        }

        public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            if (AlreadyMarked == false)
            {
                allowedVenting = false;
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
            return true;
        }

        static QuizQuestionBase GetRandomQuestion(List<QuizQuestionBase> qt)
        {
            List<QuizQuestionBase> questions = qt.Where(a => a.Stage <= QuestionDifficulty.GetInt()).ToList();
            var rnd = IRandom.Instance;
            QuizQuestionBase question = questions[rnd.Next(0, questions.Count)];
            question.FixUnsetAnswers();
            return question;
        }

        public static void OnReportDeadBody(PlayerControl player, GameData.PlayerInfo targetInfo)
        {
            lastReportedColor = thisReportedColor;
            thisReportedColor = targetInfo.GetPlayerColorString();
            DoQuestion();
        }

        public static void OnButtonPress(PlayerControl player)
        {
            lastButtonPressedColor = thisButtonPressedColor;
            thisButtonPressedColor = player.Data.GetPlayerColorString();
            DoQuestion();
        }

        public static void DoQuestion()
        {
            if (MarkedPlayer != byte.MaxValue)
            {
                List<QuizQuestionBase> Questions = new List<QuizQuestionBase>
                {
                    new SabotageQuestion { Stage = 1, Question = "LastSabotage",/* JSON ENTRIES */ QuizmasterQuestionType = QuizmasterQuestionType.LatestSabotageQuestion },
                    new SabotageQuestion { Stage = 1, Question = "FirstRoundSabotage", QuizmasterQuestionType = QuizmasterQuestionType.FirstRoundSabotageQuestion },
                    new PlrColorQuestion { Stage = 1, Question = "LastEjectedPlayerColor", QuizmasterQuestionType = QuizmasterQuestionType.EjectionColorQuestion },
                    new PlrColorQuestion { Stage = 1, Question = "LastReportPlayerColor", QuizmasterQuestionType = QuizmasterQuestionType.ReportColorQuestion },
                    new PlrColorQuestion { Stage = 1, Question = "LastButtonPressedPlayerColor", QuizmasterQuestionType = QuizmasterQuestionType.LastMeetingColorQuestion },
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
            killsForRound = 0;
            allowedVenting = true;
            allowedKilling = false;
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

        public static void SetKillButtonText(HudManager instance)
        {
            if (allowedKilling)
                instance.KillButton.OverrideText(GetString("KillButtonText"));
            else
                instance.KillButton.OverrideText(GetString("QuizmasterKillButtonText"));
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

        public static void RightAnswer(PlayerControl target)
        {
            lastReportedColor = thisReportedColor;
            Utils.SendMessage(GetString("QuizmasterCorrectTarget"), target.PlayerId, GetString("QuizmasterChatNoticeTitle"));
            Utils.SendMessage(GetString("QuizmasterCorrect").Replace("{QMTARGET}", target.GetRealName()), Player.PlayerId, GetString("QuizmasterChatNoticeTitle"));
            ResetMarkedPlayer();
        }

        public static void WrongAnswer(PlayerControl target, string wrongAnswer, string rightAnswer)
        {
            lastReportedColor = thisReportedColor;
            KillPlayer(target);
            Utils.SendMessage(GetString("QuizmasterWrong").Replace("{QMTARGET}", target.GetRealName()), Player.PlayerId, GetString("QuizmasterChatNoticeTitle"));
            Utils.SendMessage(GetString("QuizmasterWrongTarget").Replace("{QMWRONG}", wrongAnswer).Replace("{QMRIGHT}", rightAnswer).Replace("{QM}", Player.GetRealName()), target.PlayerId, GetString("QuizmasterChatNoticeTitle"));
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

    class PlrColorQuestion : QuizQuestionBase
    {

        public override void FixUnsetAnswers()
        {
            Answers = new List<string>{ };
            List<string> PossibleAnswers = new List<string> { };

            foreach (PlayerControl plr in Main.AllPlayerControls)
            {
                if (!PossibleAnswers.Contains(plr.Data.GetPlayerColorString())) PossibleAnswers.Add(plr.Data.GetPlayerColorString());
            }


            var rnd = IRandom.Instance;
            int positionForRightAnswer = rnd.Next(0, 3);

            if (QuizmasterQuestionType == QuizmasterQuestionType.EjectionColorQuestion)
                Answer = Quizmaster.lastReportedColor;
            else if (QuizmasterQuestionType == QuizmasterQuestionType.ReportColorQuestion)
                Answer = Quizmaster.lastReportedColor;
            else if (QuizmasterQuestionType == QuizmasterQuestionType.LastMeetingColorQuestion)
                Answer = Quizmaster.lastButtonPressedColor;
            PossibleAnswers.Remove(Answer);
            for (int numOfQuestionsDone = 0; numOfQuestionsDone < 3; numOfQuestionsDone++)
            {
                var prefix = "";
                if (numOfQuestionsDone == positionForRightAnswer)
                {
                    AnswerLetter = new List<string> { "A", "B", "C" }[positionForRightAnswer];
                    if (Answer == "None") prefix = "Quizmaster.";
                    Answers.Add(prefix + Answer);
                }
                else
                {
                    string thatAnswer = PossibleAnswers[rnd.Next(0, PossibleAnswers.Count)];
                    if (Answer == "None") prefix = "Quizmaster.";
                    Answers.Add(prefix + thatAnswer);
                    PossibleAnswers.Remove(thatAnswer);
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
            List<string> PossibleAnswers = (MapNames)Main.NormalOptions.MapId switch
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

            PossibleAnswers.Remove(Answer);
            for (int numOfQuestionsDone = 0; numOfQuestionsDone < 3; numOfQuestionsDone++)
            {
                var prefix = "QuizmasterSabotages.";
                if (numOfQuestionsDone == positionForRightAnswer)
                {
                    AnswerLetter = new List<string> { "A", "B", "C" }[positionForRightAnswer];
                    if (Answer == "None") prefix = "Quizmaster.";
                    Answers.Add(prefix + Answer);
                }
                else
                {
                    string thatAnswer = PossibleAnswers[rnd.Next(0, PossibleAnswers.Count)];
                    if (Answer == "None") prefix = "Quizmaster.";
                    Answers.Add(prefix + thatAnswer);
                    PossibleAnswers.Remove(thatAnswer);
                }
            }
        }
    }

    public enum QuizmasterQuestionType
    {
        FirstRoundSabotageQuestion,
        LatestSabotageQuestion,
        EjectionColorQuestion,
        ReportColorQuestion,
        LastMeetingColorQuestion,
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
}