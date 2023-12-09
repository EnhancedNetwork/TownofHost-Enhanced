using Hazel;
using System.Collections.Generic;
using System.Linq;
using TOHE.Modules;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Neutral;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral
{

    public class Quizmaster
    {
        private static readonly int Id = 26200;
        public static List<byte> playerIdList = new();
        public static bool IsEnable = false;
        private static OptionItem QuestionDifficulty;

        public static byte MarkedPlayer = (dynamic)null;
        private static List<QuizQuestionBase> Quetions = new List<QuizQuestionBase>
        {
            new SabotageQuestion { Stage = 1, Question = "What was the sabotage was called last?", QuizmasterQuestionType = QuizmasterQuestionType.SabotageQuestion },
        };
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Quizmaster);
            QuestionDifficulty = IntegerOptionItem.Create(Id + 10, "QuizmasterQuestionDifficulty", new(1, 5, 1), 1, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Quizmaster]);
        }
        public static void Init()
        {
            playerIdList = new();
            IsEnable = false;
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
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
            return false;
        }
        public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            MarkedPlayer = target.PlayerId;
                SendRPC(killer.PlayerId);
                killer.ResetKillCooldown();
                killer.SetKillCooldown();
                killer.SyncSettings();
                killer.RPCPlayCustomSound("Clothe");
               return false;
        }

        public static void OnReportDeadBody(PlayerControl player, PlayerControl targetInfo)
        {
            if (targetInfo == Main.AllPlayerControls[MarkedPlayer])

            HudManager.Instance.Chat.AddChat(Main.AllPlayerControls[MarkedPlayer], "HAHA FUCK YOU");
            HudManager.Instance.Chat.AddChat(Main.AllPlayerControls[player.PlayerId], "HAHA YOU KILLED SOMEONE");
        }
    }

    abstract class QuizQuestionBase
    {
        public int Stage { get; set; }
        public QuizmasterQuestionType QuizmasterQuestionType { get; set; }

        public abstract string Question { get; set; }
        public abstract string Answer { get; set; }
        public abstract List<string> Answers { get; set; }
        public abstract string GetMessage(PlayerControl killer, bool showStageClue);
        public abstract string FixUnsetAnswers();
    }

    class SabotageQuestion: QuizQuestionBase
    {
        private static List<Sabotages> AirshitSabotages = new List<Sabotages> { Sabotages.Lights, Sabotages.Reactor, Sabotages.Communications };
        private static List<Sabotages> SkeldSabotages = new List<Sabotages> { Sabotages.Lights, Sabotages.Reactor, Sabotages.O2 };
        private static List<Sabotages> PolusSabotages = new List<Sabotages> { Sabotages.Lights, Sabotages.Reactor, Sabotages.Communications };
        private static List<Sabotages> MiraSabotages = new List<Sabotages> { Sabotages.Lights, Sabotages.Reactor, Sabotages.O2, Sabotages.Communications };
        private static List<Sabotages> FungleSabotages = new List<Sabotages> { Sabotages.Communications, Sabotages.Reactor, Sabotages.MushroomMixup };

        public override string Question { get => Question; set => throw new System.NotImplementedException(); }
        public override string Answer { get => Answer; set => Answer = value; }
        public override List<string> Answers { get => Answers; set => Answers = value; }

        public override string FixUnsetAnswers()
        {
            Answers = (MapNames)Main.NormalOptions.MapId switch
            {
                MapNames.Skeld => SkeldSabotages,
                MapNames.Dleks => SkeldSabotages,
                MapNames.Mira => MiraSabotages,
                MapNames.Polus => PolusSabotages,
                MapNames.Airship => AirshitSabotages,
                MapNames.Fungle => FungleSabotages,
                _ => throw new System.NotImplementedException(),
            } as dynamic;

            Answer = (dynamic)QuizmasterHelpers.lastSabotage;

            return null;
        }

        public override string GetMessage(PlayerControl killer, bool showStageClue)
        {
            throw new System.NotImplementedException();
        }
    }
    /*class Question : QuizQuestionBase
    {
        public override string GetMessage(PlayerControl killer, bool showStageClue)
        {
            var killerOutfit = Camouflage.PlayerSkins[killer.PlayerId];
            if (killerOutfit.HatId == "hat_EmptyHat")
                return GetString("EnigmaClueHat2");

            return null;
        }
    }*/

    enum QuizmasterQuestionType
    {
        SabotageQuestion
    }

    enum Sabotages
    {
        None,

        Reactor,
        Lights,
        Communications,
        O2,
        MushroomMixup
    }
}