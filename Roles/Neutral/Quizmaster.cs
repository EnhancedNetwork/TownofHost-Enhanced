using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using System;
using TOHE.Modules;
using TOHE.Roles.Core;
using static TOHE.MeetingHudStartPatch;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Quizmaster : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Quizmaster;
    private const int Id = 27000;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Quizmaster);
    public override bool IsExperimental => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => CanKillsAfterMark() ? Custom_RoleType.NeutralKilling : Custom_RoleType.NeutralChaos;
    //==================================================================\\

    private static OptionItem MinQuestionDifficulty;
    private static OptionItem MaxQuestionDifficulty;
    public static OptionItem CanKillAfterMarkOpt;
    private static OptionItem CanVent;
    private static OptionItem HasImpostorVision;
    private static OptionItem CanGiveQuestionsAboutPastGames;

    private static QuizQuestionBase Question = new SetAnswersQuestion { Stage = 0, Answer = "Select Me", PossibleAnswers = { "Select me", "Die", "Die", "Die" }, Question = "This question is to prevent crashes answer the letter with the answer \"Select me\"", HasAnswersTranslation = false, HasQuestionTranslation = false };
    private static QuizQuestionBase previousQuestion = new SetAnswersQuestion { Stage = 0, Answer = "Select Me", PossibleAnswers = { "Select me", "Die", "Die", "Die" }, Question = "This question is to prevent crashes answer the letter with the answer \"Select me\"", HasAnswersTranslation = false, HasQuestionTranslation = false };

    private static PlayerControl Player;
    public static Sabotages lastSabotage = Sabotages.None;
    public static Sabotages firstSabotageOfRound = Sabotages.None;
    private static bool allowedKilling = false;
    private static bool AlreadyMarked = false;
    private static byte MarkedPlayer = byte.MaxValue;
    public static string lastExiledColor = "None";
    public static string lastReportedColor = "None";
    public static string thisReportedColor = "None";
    public static string lastButtonPressedColor = "None";
    public static string thisButtonPressedColor = "None";
    public static int meetingNum = 0;
    public static int diedThisRound = 0;
    public static int buttonMeeting = 0;

    public static bool CanKillAfterMark = false;

    public override void SetupCustomOption()
    {
        TabGroup tab = TabGroup.NeutralRoles;

        SetupSingleRoleOptions(Id, tab, CustomRoles.Quizmaster, 1);
        MinQuestionDifficulty = IntegerOptionItem.Create(Id + 15, "QuizmasterSettings.MinQuestionDifficulty", new(1, 5, 1), 1, tab, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Quizmaster]);
        MaxQuestionDifficulty = IntegerOptionItem.Create(Id + 10, "QuizmasterSettings.MaxQuestionDifficulty", new(1, 5, 1), 1, tab, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Quizmaster]);
        CanVent = BooleanOptionItem.Create(Id + 11, GeneralOption.CanVent, true, tab, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Quizmaster]);
        CanKillAfterMarkOpt = BooleanOptionItem.Create(Id + 12, "QuizmasterSettings.CanKillAfterMark", false, tab, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Quizmaster]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, GeneralOption.ImpostorVision, true, tab, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Quizmaster]);
        CanGiveQuestionsAboutPastGames = BooleanOptionItem.Create(Id + 14, "QuizmasterSettings.CanGiveQuestionsAboutPastGames", false, tab, false)
           .SetParent(CustomRoleSpawnChances[CustomRoles.Quizmaster]);
    }
    public override void Init()
    {
        Player = null;
        firstSabotageOfRound = Sabotages.None;
        allowedKilling = false;
        AlreadyMarked = false;
        MarkedPlayer = byte.MaxValue;

        if (!CanGiveQuestionsAboutPastGames.GetBool())
        {
            lastExiledColor = "None";
            lastReportedColor = "None";
            lastButtonPressedColor = "None";
            lastSabotage = Sabotages.None;
        }

        thisReportedColor = "None";
        thisButtonPressedColor = "None";
        diedThisRound = 0;
        meetingNum = 0;
        buttonMeeting = 0;

        CanKillAfterMark = CanKillAfterMarkOpt.GetBool();
    }
    public override void Add(byte playerId)
    {
        Player = _Player;
        MarkedPlayer = byte.MaxValue;

        CustomRoleManager.CheckDeadBodyOthers.Add(OnPlayerDead);
    }
    public void SendRPC(byte targetId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte targetId = reader.ReadByte();

        if (targetId != byte.MaxValue)
        {
            AlreadyMarked = true;
            MarkedPlayer = targetId;

            allowedKilling = CanKillAfterMark;
        }
        else
        {
            MarkedPlayer = targetId;
        }
    }

    public static bool CanKillsAfterMark() => CanKillAfterMark;

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = 15;
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(HasImpostorVision.GetBool());

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!killer.RpcCheckAndMurder(target, true)) return false;
        if (AlreadyMarked == false)
        {
            AlreadyMarked = true;
            MarkedPlayer = target.PlayerId;
            SendRPC(target.PlayerId);

            Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);

            allowedKilling = CanKillAfterMark;

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            killer.MarkDirtySettings();
            killer.RPCPlayCustomSound("Clothe");

            return false;
        }
        return allowedKilling && AlreadyMarked;
    }

    private static QuizQuestionBase GetRandomQuestion(List<QuizQuestionBase> qt)
    {
        List<QuizQuestionBase> questions = qt.Where(a => a.Stage <= MaxQuestionDifficulty.GetInt() && a.Stage >= MinQuestionDifficulty.GetInt()).ToList();
        var rnd = IRandom.Instance;
        QuizQuestionBase question = questions[rnd.Next(0, questions.Count)];
        if (question == previousQuestion)
        {
            question = questions[rnd.Next(0, questions.Count)];
        }
        question ??= new PlrColorQuestion { Stage = 1, Question = "LastReportPlayerColor", QuizmasterQuestionType = QuizmasterQuestionType.ReportColorQuestion };

        previousQuestion = question;
        question.FixUnsetAnswers();
        return question;
    }

    private static CustomRoles GetRandomRole(List<CustomRoles> roles, bool AllowAddons)
    {
        var rnd = IRandom.Instance;
        CustomRoles chosenRole = roles[rnd.Next(0, roles.Count)];
        if (chosenRole.IsAdditionRole() && !AllowAddons)
        {
            for (int s = 0; s < -1; s++)
            {
                if (chosenRole.IsAdditionRole() && !AllowAddons)
                {
                    chosenRole = roles[rnd.Next(0, roles.Count)];
                }
                else
                {
                    break;
                }
            }
        }
        return chosenRole;
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (reporter == null) return;

        if (target == null)
        {
            buttonMeeting++;
            lastButtonPressedColor = thisButtonPressedColor;
            thisButtonPressedColor = reporter.Data.GetPlayerColorString();
        }
        else
        {
            var targetInfo = target;
            lastReportedColor = thisReportedColor;
            thisReportedColor = targetInfo.GetPlayerColorString();
        }
        meetingNum++;
        DoQuestion();
    }

    private void DoQuestion()
    {
        Player = _Player;
        if (MarkedPlayer != byte.MaxValue)
        {
            CustomRoles randomRole = GetRandomRole([.. CustomRolesHelper.AllRoles], false);
            CustomRoles randomRoleWithAddon = GetRandomRole([.. CustomRolesHelper.AllRoles], false);
            List<QuizQuestionBase> Questions =
            [
                new SabotageQuestion { Stage = 1, Question = "LastSabotage",/* JSON ENTRIES */ QuizmasterQuestionType = QuizmasterQuestionType.LatestSabotageQuestion },
                new SabotageQuestion { Stage = 1, Question = "FirstRoundSabotage", QuizmasterQuestionType = QuizmasterQuestionType.FirstRoundSabotageQuestion },
                new PlrColorQuestion { Stage = 1, Question = "LastEjectedPlayerColor", QuizmasterQuestionType = QuizmasterQuestionType.EjectionColorQuestion },
                new PlrColorQuestion { Stage = 1, Question = "LastReportPlayerColor", QuizmasterQuestionType = QuizmasterQuestionType.ReportColorQuestion },
                new PlrColorQuestion { Stage = 1, Question = "LastButtonPressedPlayerColor", QuizmasterQuestionType = QuizmasterQuestionType.LastMeetingColorQuestion },

                new CountQuestion { Stage = 2, Question = "MeetingPassed", QuizmasterQuestionType = QuizmasterQuestionType.MeetingCountQuestion },
                new SetAnswersQuestion { Stage = 2, Question = "HowManyFactions", Answer = "Four", PossibleAnswers = { "One", "Two", "Three", "Four", "Five" }, QuizmasterQuestionType = QuizmasterQuestionType.FactionQuestion },
                new SetAnswersQuestion { Stage = 2, Question = GetString("QuizmasterQuestions.FactionOfRole").Replace("{QMRole}", randomRoleWithAddon.ToString()), HasQuestionTranslation = false, Answer = CustomRolesHelper.GetCustomRoleTeam(randomRoleWithAddon).ToString(), PossibleAnswers = { "Crewmate", "Impostor", "Neutral", "Coven", "Addon" }, QuizmasterQuestionType = QuizmasterQuestionType.RoleFactionQuestion },
                new SetAnswersQuestion { Stage = 2, Question = GetString("QuizmasterQuestions.BasisOfRole").Replace("{QMRole}", randomRole.ToString()), HasQuestionTranslation = false, Answer = CustomRolesHelper.GetRoleTypes(randomRole).ToString(), PossibleAnswers = { "Crewmate", "Impostor", "Shapeshifter", "Scientist", "Engineer", "GuardianAngel" }, QuizmasterQuestionType = QuizmasterQuestionType.RoleBasisQuestion },

                new SetAnswersQuestion { Stage = 3, Question = "FactionRemovedName", Answer = "None", PossibleAnswers = { "Sabotuer", "Sorcerers", "Coven", "Killer", "None" }, QuizmasterQuestionType = QuizmasterQuestionType.RemovedFactionQuestion },
                // ^ I added Coven back so this question no longer applies :) - Marg
                new SetAnswersQuestion { Stage = 3, Question = "WhatDoesEOgMeansInName", Answer = "Edited", PossibleAnswers = { "Edition", "Experimental", "Enhanced", "Edited" }, QuizmasterQuestionType = QuizmasterQuestionType.NameOriginQuestion },
                new CountQuestion { Stage = 3, Question = "HowManyDiedFirstRound", QuizmasterQuestionType = QuizmasterQuestionType.DiedFirstRoundCountQuestion },
                new CountQuestion { Stage = 3, Question = "ButtonPressedBefore", QuizmasterQuestionType = QuizmasterQuestionType.ButtonPressedBeforeThisQuestion },
                new SetAnswersQuestion { Stage = 3, Question = "WhoOwns", Answer = "Lime", PossibleAnswers = { "Lauryn", "Jackler", "Lime", "Marg", "Sarha", "laikrai", "Niko", "D1GQ", "KARPED1EM", "Matt" }, QuizmasterQuestionType = QuizmasterQuestionType.WhoOwns },

                new DeathReasonQuestion { Stage = 4, Question = "PlrDieReason", QuizmasterQuestionType = QuizmasterQuestionType.PlrDeathReasonQuestion},
                new DeathReasonQuestion { Stage = 4, Question = "PlrDieMethod", QuizmasterQuestionType = QuizmasterQuestionType.PlrDeathMethodQuestion},
                new SetAnswersQuestion { Stage = 4, Question = "LastAddedRoleForKarped", Answer = "Pacifist", PossibleAnswers = { "Pacifist", "Vampire", "Snitch", "Vigilante", "Jackal", "Mole", "Sniper" }, QuizmasterQuestionType = QuizmasterQuestionType.RoleAddedQuestion },
                new DeathReasonQuestion { Stage = 4, Question = "PlrDieFaction", QuizmasterQuestionType = QuizmasterQuestionType.PlrDeathKillerFactionQuestion},
                new SetAnswersQuestion { Stage = 4, Question = "QuizmasterCooldown", Answer = "15", PossibleAnswers = { "15", "30", "0", "999", AURoleOptions.KillCooldown.ToString() }, QuizmasterQuestionType = QuizmasterQuestionType.QuizmasterCooldownQuestion }, // this is a level 4 because the only way to know this would be to look at the code for Quizmaster
                new SetAnswersQuestion { Stage = 4, Question = "WhoCoded", Answer = "Multiple People", PossibleAnswers = { "Furo", "Drakos", "Lime", "Marg", "Multiple People", "TommyXL", "Niko", "Pyro", "KARPED1EM", "Ryuk" }, QuizmasterQuestionType = QuizmasterQuestionType.WhoCoded },

                new SetAnswersQuestion { Stage = 5, Question = "TOHEPartners", Answer = "Modded Among Us Lobbies", PossibleAnswers = { "Innersloth", "Modded Among Us Lobbies", "Purple Among Us", "Modded Among Us Lobbies & Purple Among Us", "Steam", "Twitter", "Town Of Us: Reactivated", "Lime Corporation", "Digital Bandidos" }, QuizmasterQuestionType = QuizmasterQuestionType.TOHEPartners },
                new SetAnswersQuestion { Stage = 5, Question = "TOHEEventCoordinator", Answer = "Sarha", PossibleAnswers = { "Lime", "Sarha", "Lauryn", "Jackler", "Matt", "Tasha", "Pyro", "Fish" }, QuizmasterQuestionType = QuizmasterQuestionType.TOHEEventCoordinator },
                new SetAnswersQuestion { Stage = 5, Question = "HowManyCats", Answer = "3", PossibleAnswers = { "0", "1", "2", "3", "4", "5", "6" }, QuizmasterQuestionType = QuizmasterQuestionType.HowManyCats }, // Copycat, Schrodinger's Cat, OIIAI (I want to count Jinx because of its origin in TOS2, but I won't)
                new SetAnswersQuestion { Stage = 5, Question = "GuessingCommand", Answer = "Bet", PossibleAnswers = { "Nothing, it's just /bt", "Bet", "Bloodthirst", "Betray Them", "Bomb Tag", "Bad Thing" }, QuizmasterQuestionType = QuizmasterQuestionType.GuessingCommand },
                new SetAnswersQuestion { Stage = 5, Question = "NotFromTOS2", Answer = "Moon Dancer", PossibleAnswers = { "Coven Leader", "Jinx", "Marshall", "Doomsayer", "Baker", "Moon Dancer", "Pirate", "Mayor", "Veteran", "Psychic" }, QuizmasterQuestionType = QuizmasterQuestionType.NotFromTOS2 },
            ];

            Question = GetRandomQuestion(Questions);
        }
    }
    public override void OnMeetingHudStart(PlayerControl pc)
    {
        if (Player == null) return;
        if (pc.PlayerId == Player.PlayerId && MarkedPlayer != byte.MaxValue)
        {
            AddMsg(GetString("QuizmasterChat.Marked").Replace("{QMTARGET}", Utils.GetPlayerById(MarkedPlayer)?.GetRealName(isMeeting: true)).Replace("{QMQUESTION}", Question.HasQuestionTranslation ? GetString("QuizmasterQuestions." + Question.Question) : Question.Question), pc.PlayerId, GetString("QuizmasterChat.Title"));
        }
    }
    public override void OnOthersMeetingHudStart(PlayerControl pc)
    {
        if (pc == null || Player == null || !MarkedPlayer.GetPlayer().IsAlive()) return;

        if (pc.PlayerId == MarkedPlayer)
        {
            ShowQuestion(pc);
        }
        else if (pc.PlayerId != Player.PlayerId && pc.PlayerId != MarkedPlayer)
        {
            AddMsg(GetString("QuizmasterChat.MarkedPublic").Replace("{QMCOLOR}", Utils.GetRoleColorCode(CustomRoles.Quizmaster)).Replace("{QMTARGET}", Utils.GetPlayerById(MarkedPlayer)?.GetRealName(isMeeting: true)), pc.PlayerId, GetString("QuizmasterChat.Title"));
        }
    }
    public override void OnPlayerExiled(PlayerControl player, NetworkedPlayerInfo exiled)
    {
        if (exiled == null) return;

        if (exiled.Object.Is(CustomRoles.Quizmaster))
        {
            ResetMarkedPlayer(false);
        }
        lastExiledColor = exiled.GetPlayerColorString();
    }

    public override void AfterMeetingTasks()
    {
        if (Player == null) return;

        firstSabotageOfRound = Sabotages.None;
        allowedKilling = false;
        diedThisRound = 0;
        if (MarkedPlayer != byte.MaxValue)
            KillPlayer(Utils.GetPlayerById(MarkedPlayer));

        ResetMarkedPlayer(true);
    }

    public static void ResetMarkedPlayer(bool canMarkAgain = true)
    {
        if (Player == null) return;

        if (canMarkAgain)
            AlreadyMarked = false;

        MarkedPlayer = byte.MaxValue;
        if (Player?.GetRoleClass() is Quizmaster Quiz)
            Quiz.SendRPC(byte.MaxValue);
    }

    private void OnPlayerDead(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        diedThisRound++;
        if (target.PlayerId == MarkedPlayer) ResetMarkedPlayer(false);
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
            => hud.KillButton.OverrideText(GetString(allowedKilling ? "KillButtonText" : "QuizmasterKillButtonText"));

    public override string GetMark(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false)
        => (!isForMeeting && seer.PlayerId != target.PlayerId && MarkedPlayer == target.PlayerId) ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Quizmaster), " ?!") : string.Empty;


    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
            => (isForMeeting && MarkedPlayer == target.PlayerId) ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Quizmaster), " ?!") : string.Empty;


    public static void OnSabotageCall(SystemTypes systemType)
    {
        if (!Main.MeetingIsStarted
            && systemType is
                SystemTypes.HeliSabotage or
                SystemTypes.Laboratory or
                SystemTypes.Reactor or
                SystemTypes.Electrical or
                SystemTypes.LifeSupp or
                SystemTypes.Comms or
                SystemTypes.MushroomMixupSabotage)
        {
            switch (systemType)
            {
                case SystemTypes.HeliSabotage: //The Airhip
                case SystemTypes.Laboratory: //Polus
                case SystemTypes.Reactor: //Other maps
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
    }

    private static void KillPlayer(PlayerControl plrToKill)
    {
        plrToKill.SetDeathReason(PlayerState.DeathReason.WrongAnswer);
        Main.PlayerStates[plrToKill.PlayerId].SetDead();
        plrToKill.Data.IsDead = true;
        plrToKill.RpcExileV2();
        plrToKill.SetRealKiller(Player);
        ResetMarkedPlayer(true);
    }

    private static void RightAnswer(PlayerControl target)
    {
        if (Player == null || target == null) return;
        lastReportedColor = thisReportedColor;
        foreach (var plr in Main.AllPlayerControls)
        {
            if (plr.PlayerId != Player.PlayerId && target.PlayerId != plr.PlayerId)
            {
                Utils.SendMessage(GetString("QuizmasterChat.CorrectPublic").Replace("{QMCOLOR}", Utils.GetRoleColorCode(CustomRoles.Quizmaster)).Replace("{QMTARGET}", target.GetRealName()), plr.PlayerId, GetString("QuizmasterChat.Title"));
            }
        }
        Utils.SendMessage(GetString("QuizmasterChat.CorrectTarget"), target.PlayerId, GetString("QuizmasterChat.Title"));
        Utils.SendMessage(GetString("QuizmasterChat.Correct").Replace("{QMTARGET}", target.GetRealName()), Player.PlayerId, GetString("QuizmasterChat.Title"));
        ResetMarkedPlayer(true);
    }

    private static void WrongAnswer(PlayerControl target, string wrongAnswer, string rightAnswer)
    {
        if (Player == null) return;
        lastReportedColor = thisReportedColor;
        KillPlayer(target);
        foreach (var plr in Main.AllPlayerControls)
        {
            if (plr.PlayerId != Player.PlayerId && target.PlayerId != plr.PlayerId)
            {
                Utils.SendMessage(GetString("QuizmasterChat.WrongPublic").Replace("{QMCOLOR}", Utils.GetRoleColorCode(CustomRoles.Quizmaster)).Replace("{QMTARGET}", target.GetRealName()), plr.PlayerId, GetString("QuizmasterChat.Title"));
            }
        }
        Utils.SendMessage(GetString("QuizmasterChat.Wrong").Replace("{QMTARGET}", target.GetRealName()), Player.PlayerId, GetString("QuizmasterChat.Title"));
        Utils.SendMessage(GetString("QuizmasterChat.WrongTarget").Replace("{QMWRONG}", wrongAnswer).Replace("{QMRIGHT}", rightAnswer).Replace("{QM}", Player.GetRealName()), target.PlayerId, GetString("QuizmasterChat.Title"));
    }
    public static void AnswerByChat(PlayerControl plr, string[] args)
    {
        if (Player == null) return;
        if (MarkedPlayer == plr.PlayerId)
        {
            var answerSyntaxValid = args.Length == 2;
            if (answerSyntaxValid)
            {
                string answer = args[1].ToUpper();
                var answerValid = (answer == "A" || answer == "B" || answer == "C");
                var rightAnswer = Question.AnswerLetter.Trim().ToUpper();

                if (answerValid)
                {
                    if (rightAnswer == answer)
                        RightAnswer(plr);
                    else
                        WrongAnswer(plr, answer, rightAnswer);
                }
                else
                {
                    Utils.SendMessage(GetString("QuizmasterChat.AnswerNotValid"), plr.PlayerId, GetString("QuizmasterChat.Title"));
                }
            }
            else
            {
                Utils.SendMessage(GetString("QuizmasterChat.SyntaxNotValid"), plr.PlayerId, GetString("QuizmasterChat.Title"));
            }
        }
        else if (plr.GetCustomRole() is CustomRoles.Quizmaster)
        {
            Utils.SendMessage(GetString("QuizmasterChat.CantAnswer"), plr.PlayerId, GetString("QuizmasterChat.Title"));
        }
    }

    public static void ShowQuestion(PlayerControl plr)
    {
        if (Player == null) return;
        if (plr.PlayerId == MarkedPlayer)
        {
            Utils.SendMessage(GetString("QuizmasterChat.MarkedBy").Replace("{QMCOLOR}", Utils.GetRoleColorCode(CustomRoles.Quizmaster)).Replace("{QMQUESTION}", Question.HasQuestionTranslation ? GetString("QuizmasterQuestions." + Question.Question) : Question.Question), MarkedPlayer, GetString("QuizmasterChat.Title"));
            Utils.SendMessage(GetString("QuizmasterChat.Answers").Replace("{QMA}", Question.HasAnswersTranslation ? GetString(Question.Answers[0], showInvalid: Question.ShowInvalid) : Question.Answers[0]).Replace("{QMB}", Question.HasAnswersTranslation ? GetString(Question.Answers[1], showInvalid: Question.ShowInvalid) : Question.Answers[1]).Replace("{QMC}", Question.HasAnswersTranslation ? GetString(Question.Answers[2], showInvalid: Question.ShowInvalid) : Question.Answers[2]), MarkedPlayer, GetString("QuizmasterChat.Title"));
        }
    }
}

abstract public class QuizQuestionBase
{
    public int Stage { get; set; }
    public QuizmasterQuestionType QuizmasterQuestionType { get; set; }

    public string Question { get; set; }
    public string Answer { get; set; }
    public string AnswerLetter { get; set; }
    public List<string> Answers { get; set; }
    public List<string> PossibleAnswers { get; set; } = [];
    public bool HasAnswersTranslation { get; set; } = true;
    public bool HasQuestionTranslation { get; set; } = true;
    public bool ShowInvalid { get; set; } = true;
    public abstract void FixUnsetAnswers();
}

class PlrColorQuestion : QuizQuestionBase
{
    public override void FixUnsetAnswers()
    {
        Answers = [];

        foreach (PlayerControl plr in Main.AllPlayerControls)
        {
            if (!PossibleAnswers.Contains(plr.Data.GetPlayerColorString()))
                PossibleAnswers.Add(plr.Data.GetPlayerColorString());
        }

        var rnd = IRandom.Instance;
        int positionForRightAnswer = rnd.Next(3);

        Answer = QuizmasterQuestionType switch
        {
            QuizmasterQuestionType.EjectionColorQuestion => Quizmaster.lastExiledColor,
            QuizmasterQuestionType.ReportColorQuestion => Quizmaster.lastReportedColor,
            QuizmasterQuestionType.LastMeetingColorQuestion => Quizmaster.lastButtonPressedColor,
            _ => "None"
        };

        HasAnswersTranslation = false;
        ShowInvalid = false;

        if (PossibleAnswers.Contains(Answer))
            PossibleAnswers.Remove(Answer);

        for (int numOfQuestionsDone = 0; numOfQuestionsDone < 3; numOfQuestionsDone++)
        {
            if (numOfQuestionsDone == positionForRightAnswer)
            {
                AnswerLetter = new List<string> { "A", "B", "C" }[positionForRightAnswer];
                Answers.Add(Answer);
            }
            else
            {
                string thatAnswer = PossibleAnswers.RandomElement();
                Answers.Add(thatAnswer);
                PossibleAnswers.Remove(thatAnswer);
            }
        }
    }
}

class DeathReasonQuestion : QuizQuestionBase
{
    public override void FixUnsetAnswers()
    {
        Answers = [];

        var rnd = IRandom.Instance;

        PlayerControl chosenPlayer = null;

        if (QuizmasterQuestionType == QuizmasterQuestionType.PlrDeathReasonQuestion)
        {
            PossibleAnswers.Add("None");
            PossibleAnswers.Add(PlayerState.DeathReason.etc.ToString());
            PossibleAnswers.Add(GetString("DeathReason.Vote"));
        }
        else if (QuizmasterQuestionType == QuizmasterQuestionType.PlrDeathMethodQuestion)
        {
            PossibleAnswers.Add(GetString("Disconnected"));
            PossibleAnswers.Add(GetString("DeathReason.Vote"));
            PossibleAnswers.Add(GetString("DeathReason.Kill"));
        }
        else if (QuizmasterQuestionType == QuizmasterQuestionType.PlrDeathKillerFactionQuestion)
        {
            PossibleAnswers.Add(GetString("QuizmasterAnswers.Impostor"));
            PossibleAnswers.Add(GetString("QuizmasterAnswers.Crewmate"));
            PossibleAnswers.Add(GetString("QuizmasterAnswers.Neutral"));
            PossibleAnswers.Add(GetString("QuizmasterAnswers.Coven"));
        }

        chosenPlayer = Main.AllPlayerControls[rnd.Next(Main.AllPlayerControls.Length)];


        foreach (PlayerControl plr in Main.AllPlayerControls)
        {
            if (QuizmasterQuestionType == QuizmasterQuestionType.PlrDeathReasonQuestion)
            {
                if (plr.Data.IsDead && !PossibleAnswers.Contains(Main.PlayerStates[chosenPlayer.PlayerId].deathReason.ToString()))
                    PossibleAnswers.Add(Main.PlayerStates[chosenPlayer.PlayerId].deathReason.ToString());
            }
        }

        int positionForRightAnswer = rnd.Next(0, 3);

        HasQuestionTranslation = false; //doing this do i can just change the player name in question
        Question = GetString("QuizmasterQuestions." + Question).Replace("{PLR}", chosenPlayer.GetRealName());

        ShowInvalid = false;

        Answer = QuizmasterQuestionType switch
        {
            QuizmasterQuestionType.PlrDeathReasonQuestion => chosenPlayer.Data.IsDead ? Main.PlayerStates[chosenPlayer.PlayerId].deathReason.ToString() : "None",
            QuizmasterQuestionType.PlrDeathMethodQuestion => chosenPlayer.Data.Disconnected ? GetString("Disconnected") : (Main.PlayerStates[chosenPlayer.PlayerId].deathReason == PlayerState.DeathReason.Vote ? GetString("DeathReason.Vote") : GetString("DeathReason.Kill")),
            QuizmasterQuestionType.PlrDeathKillerFactionQuestion => CustomRolesHelper.GetCustomRoleTeam(chosenPlayer.GetRealKiller().GetCustomRole()).ToString(),
            _ => "None"
        };

        PossibleAnswers.Remove(Answer);
        for (int numOfQuestionsDone = 0; numOfQuestionsDone < 3; numOfQuestionsDone++)
        {
            var prefix = "";
            if (numOfQuestionsDone == positionForRightAnswer)
            {
                AnswerLetter = new List<string> { "A", "B", "C" }[positionForRightAnswer];
                if (Answer == "None") prefix = "Quizmaster.";
                if (prefix != "")
                    Answer = GetString(prefix + Answer);
                Answers.Add(prefix + Answer);
            }
            else
            {
                string thatAnswer = PossibleAnswers[rnd.Next(0, PossibleAnswers.Count)];
                if (thatAnswer == "None") prefix = "Quizmaster.";
                if (prefix != "")
                    thatAnswer = GetString(prefix + thatAnswer);
                Answers.Add(prefix + thatAnswer);
                PossibleAnswers.Remove(thatAnswer);
            }
        }
    }
}

class CountQuestion : QuizQuestionBase
{
    public override void FixUnsetAnswers()
    {
        var rnd = IRandom.Instance;

        Answer = QuizmasterQuestionType switch
        {
            QuizmasterQuestionType.MeetingCountQuestion => Quizmaster.meetingNum.ToString(),
            QuizmasterQuestionType.ButtonPressedBeforeThisQuestion => (Quizmaster.buttonMeeting - 1).ToString(),
            QuizmasterQuestionType.DiedFirstRoundCountQuestion => Quizmaster.diedThisRound.ToString(),
            _ => "None"
        };

        Answers = [];
        int ans = int.Parse(Answer);
        if (ans < 1)
        {
            PossibleAnswers.Add((ans + rnd.Next(1, 3)).ToString());
            PossibleAnswers.Add((ans + rnd.Next(3, 5)).ToString());
        }
        else
        {
            PossibleAnswers.Add((ans + rnd.Next(1, 3)).ToString());
            PossibleAnswers.Add((ans - 1).ToString());
        }

        HasAnswersTranslation = false;

        int positionForRightAnswer = rnd.Next(0, 3);

        PossibleAnswers.Remove(Answer);
        for (int numOfQuestionsDone = 0; numOfQuestionsDone < 3; numOfQuestionsDone++)
        {
            if (numOfQuestionsDone == positionForRightAnswer)
            {
                AnswerLetter = new List<string> { "A", "B", "C" }[positionForRightAnswer];
                Answers.Add(Answer);
            }
            else
            {
                string thatAnswer = PossibleAnswers[rnd.Next(0, PossibleAnswers.Count)];
                Answers.Add(thatAnswer);
                PossibleAnswers.Remove(thatAnswer);
            }
        }
    }
}

class SetAnswersQuestion : QuizQuestionBase
{
    public override void FixUnsetAnswers()
    {
        Answers = [];

        var rnd = IRandom.Instance;
        int positionForRightAnswer = rnd.Next(0, 3);

        PossibleAnswers.Remove(Answer);
        for (int numOfQuestionsDone = 0; numOfQuestionsDone < 3; numOfQuestionsDone++)
        {
            var prefix = QuizmasterQuestionType switch
            {
                QuizmasterQuestionType.RoleBasisQuestion or QuizmasterQuestionType.RoleFactionQuestion or QuizmasterQuestionType.FactionQuestion or QuizmasterQuestionType.NameOriginQuestion or QuizmasterQuestionType.RemovedFactionQuestion or QuizmasterQuestionType.RoleAddedQuestion => "QuizmasterAnswers.",
                _ => ""
            };

            if (numOfQuestionsDone == positionForRightAnswer)
            {
                AnswerLetter = new List<string> { "A", "B", "C" }[positionForRightAnswer];
                if (Answer == "None") prefix = "Quizmaster.";
                Answers.Add(prefix + Answer);

                ShowInvalid = false;
            }
            else
            {
                string thatAnswer = PossibleAnswers.RandomElement();
                if (thatAnswer == "None") prefix = "Quizmaster.";
                Answers.Add(prefix + thatAnswer);
                PossibleAnswers.Remove(thatAnswer);
            }
        }
    }
}

class SabotageQuestion : QuizQuestionBase
{
    private static readonly List<Sabotages> SkeldSabotages = [Sabotages.None, Sabotages.Lights, Sabotages.Reactor, Sabotages.O2];
    private static readonly List<Sabotages> MiraSabotages = [Sabotages.None, Sabotages.Lights, Sabotages.Reactor, Sabotages.O2, Sabotages.Communications];
    private static readonly List<Sabotages> PolusSabotages = [Sabotages.None, Sabotages.Lights, Sabotages.Reactor, Sabotages.Communications];
    private static readonly List<Sabotages> AirshitSabotages = [Sabotages.None, Sabotages.Lights, Sabotages.Reactor, Sabotages.Communications];
    private static readonly List<Sabotages> FungleSabotages = [Sabotages.None, Sabotages.Communications, Sabotages.Reactor, Sabotages.MushroomMixup];

    public override void FixUnsetAnswers()
    {
        Answers = [];

        PossibleAnswers = Utils.GetActiveMapName() switch
        {
            MapNames.Skeld => SkeldSabotages.ConvertAll(f => f.ToString()),
            MapNames.Dleks => SkeldSabotages.ConvertAll(f => f.ToString()),
            MapNames.MiraHQ => MiraSabotages.ConvertAll(f => f.ToString()),
            MapNames.Polus => PolusSabotages.ConvertAll(f => f.ToString()),
            MapNames.Airship => AirshitSabotages.ConvertAll(f => f.ToString()),
            MapNames.Fungle => FungleSabotages.ConvertAll(f => f.ToString()),
            _ => throw new NotImplementedException(),
        };


        var rnd = IRandom.Instance;
        int positionForRightAnswer = rnd.Next(0, 3);

        Answer = QuizmasterQuestionType switch
        {
            QuizmasterQuestionType.LatestSabotageQuestion => Quizmaster.lastSabotage.ToString(),
            QuizmasterQuestionType.FirstRoundSabotageQuestion => Quizmaster.firstSabotageOfRound.ToString(),
            _ => Sabotages.None.ToString(),
        };

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
                if (thatAnswer == "None") prefix = "Quizmaster.";
                Answers.Add(prefix + thatAnswer);
                PossibleAnswers.Remove(thatAnswer);
            }
        }
    }
}

[Obfuscation(Exclude = true)]
public enum QuizmasterQuestionType
{
    FirstRoundSabotageQuestion,
    LatestSabotageQuestion,
    EjectionColorQuestion,
    ReportColorQuestion,
    LastMeetingColorQuestion,
    RoleBasisQuestion,
    RoleFactionQuestion,
    MeetingCountQuestion,
    FactionQuestion,
    RemovedFactionQuestion,
    ButtonPressedBeforeThisQuestion,
    DiedFirstRoundCountQuestion,
    NameOriginQuestion,
    PlrDeathReasonQuestion,
    PlrDeathMethodQuestion,
    RoleAddedQuestion,
    PlrDeathKillerFactionQuestion,
    QuizmasterCooldownQuestion,
    WhoCoded,
    WhoOwns,
    TOHEPartners,
    TOHEEventCoordinator,
    HowManyCats,
    GuessingCommand,
    NotFromTOS2,
}

[Obfuscation(Exclude = true)]
public enum Sabotages
{
    None = -1,

    Lights,
    Reactor,
    O2,
    Communications,
    MushroomMixup
}
