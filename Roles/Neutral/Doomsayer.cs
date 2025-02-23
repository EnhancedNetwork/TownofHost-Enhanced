using AmongUs.GameOptions;
using System.Text;
using TOHE.Modules;
using TOHE.Roles.Core;
using TOHE.Roles.Crewmate;
using UnityEngine;
using static TOHE.MeetingHudStartPatch;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Neutral;

internal class Doomsayer : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Doomsayer;
    private const int Id = 14100;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Doomsayer);
    public override CustomRoles ThisRoleBase => EasyMode.GetBool() ? CustomRoles.Impostor : CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralEvil;
    //==================================================================\\

    private static OptionItem DoomsayerAmountOfGuessesToWin;
    private static OptionItem DCanGuessImpostors;
    private static OptionItem DCanGuessCrewmates;
    private static OptionItem DCanGuessNeutrals;
    private static OptionItem DCanGuessCoven;
    private static OptionItem DCanGuessAdt;
    private static OptionItem AdvancedSettings;
    private static OptionItem MaxNumberOfGuessesPerMeeting;
    private static OptionItem KillCorrectlyGuessedPlayers;
    public static OptionItem DoesNotSuicideWhenMisguessing;
    private static OptionItem MisguessRolePrevGuessRoleUntilNextMeeting;
    private static OptionItem DoomsayerTryHideMsg;
    private static OptionItem ImpostorVision;
    private static OptionItem EasyMode;
    private static OptionItem EvenEasierMode;
    private static OptionItem ObserveCooldown;

    private readonly HashSet<CustomRoles> GuessedRoles = [];
    private static readonly Dictionary<byte, List<string>> MsgToSend = [];
    private static readonly Dictionary<byte, HashSet<byte>> ObserveList = [];


    private int GuessesCount = 0;
    private int GuessesCountPerMeeting = 0;
    private static bool CantGuess = false;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Doomsayer);
        DoomsayerAmountOfGuessesToWin = IntegerOptionItem.Create(Id + 10, "DoomsayerAmountOfGuessesToWin", new(1, 10, 1), 3, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Doomsayer])
            .SetValueFormat(OptionFormat.Times);
        DCanGuessImpostors = BooleanOptionItem.Create(Id + 12, "DCanGuessImpostors", true, TabGroup.NeutralRoles, true)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Doomsayer]);
        DCanGuessCrewmates = BooleanOptionItem.Create(Id + 13, "DCanGuessCrewmates", true, TabGroup.NeutralRoles, true)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Doomsayer]);
        DCanGuessNeutrals = BooleanOptionItem.Create(Id + 14, "DCanGuessNeutrals", true, TabGroup.NeutralRoles, true)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Doomsayer]);
        DCanGuessCoven = BooleanOptionItem.Create(Id + 26, "DCanGuessCoven", true, TabGroup.NeutralRoles, true)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Doomsayer]);
        DCanGuessAdt = BooleanOptionItem.Create(Id + 15, "DCanGuessAdt", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Doomsayer]);

        EasyMode = BooleanOptionItem.Create(Id + 27, "DoomsayerEasyMode", false, TabGroup.NeutralRoles, true)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Doomsayer]);
        ObserveCooldown = FloatOptionItem.Create(Id + 29, "DoomsayerObserveCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(EasyMode)
            .SetValueFormat(OptionFormat.Seconds);
        EvenEasierMode = BooleanOptionItem.Create(Id + 28, "DoomsayerEvenEasierMode", false, TabGroup.NeutralRoles, true)
            .SetParent(EasyMode).SetHidden(true);

        AdvancedSettings = BooleanOptionItem.Create(Id + 16, "DoomsayerAdvancedSettings", true, TabGroup.NeutralRoles, true)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Doomsayer]);
        MaxNumberOfGuessesPerMeeting = IntegerOptionItem.Create(Id + 23, "DoomsayerMaxNumberOfGuessesPerMeeting", new(1, 10, 1), 3, TabGroup.NeutralRoles, false)
            .SetParent(AdvancedSettings);
        KillCorrectlyGuessedPlayers = BooleanOptionItem.Create(Id + 18, "DoomsayerKillCorrectlyGuessedPlayers", true, TabGroup.NeutralRoles, true)
            .SetParent(AdvancedSettings);
        DoesNotSuicideWhenMisguessing = BooleanOptionItem.Create(Id + 24, "DoomsayerDoesNotSuicideWhenMisguessing", true, TabGroup.NeutralRoles, false)
            .SetParent(AdvancedSettings);
        MisguessRolePrevGuessRoleUntilNextMeeting = BooleanOptionItem.Create(Id + 20, "DoomsayerMisguessRolePrevGuessRoleUntilNextMeeting", true, TabGroup.NeutralRoles, true)
            .SetParent(DoesNotSuicideWhenMisguessing);

        ImpostorVision = BooleanOptionItem.Create(Id + 25, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Doomsayer]);
        DoomsayerTryHideMsg = BooleanOptionItem.Create(Id + 21, "DoomsayerTryHideMsg", true, TabGroup.NeutralRoles, true)
            .SetColor(Color.green)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Doomsayer]);
    }
    public override void Init()
    {
        CantGuess = false;
        MsgToSend.Clear();
        ObserveList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(GuessesCount);
        ObserveList[playerId] = [];
        MsgToSend[playerId] = [];
    }
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(ImpostorVision.GetBool());
    public override string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        Color TextColor = GetRoleColor(CustomRoles.Doomsayer).ShadeColor(0.25f);

        //ProgressText.Append(GetTaskCount(playerId, comms));
        ProgressText.Append(ColorString(TextColor, ColorString(Color.white, " - ") + $"({playerId.GetAbilityUseLimit()}/{DoomsayerAmountOfGuessesToWin.GetInt()})"));
        return ProgressText.ToString();
    }
    public static bool CheckCantGuess = CantGuess;
    public static bool NeedHideMsg(PlayerControl pc) => pc.Is(CustomRoles.Doomsayer) && DoomsayerTryHideMsg.GetBool();

    private void CheckCountGuess(PlayerControl doomsayer)
    {
        if (doomsayer.GetAbilityUseLimit() < DoomsayerAmountOfGuessesToWin.GetInt()) return;

        GuessesCount = DoomsayerAmountOfGuessesToWin.GetInt();
        if (!CustomWinnerHolder.CheckForConvertedWinner(doomsayer.PlayerId))
        {
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Doomsayer);
            CustomWinnerHolder.WinnerIds.Add(doomsayer.PlayerId);
        }
    }

    public override void OnReportDeadBody(PlayerControl goku, NetworkedPlayerInfo solos)
    {
        if (!AdvancedSettings.GetBool()) return;

        CantGuess = false;
        GuessesCountPerMeeting = 0;
    }

    public override string NotifyPlayerName(PlayerControl seer, PlayerControl target, string TargetPlayerName = "", bool IsForMeeting = false)
        => seer.IsAlive() && target.IsAlive() ? ColorString(GetRoleColor(CustomRoles.Doomsayer), target.PlayerId.ToString()) + " " + TargetPlayerName : string.Empty;


    public static bool HideTabInGuesserUI(int TabId)
    {
        if (!DCanGuessCrewmates.GetBool() && TabId == 0) return true;
        if (!DCanGuessImpostors.GetBool() && TabId == 1) return true;
        if (!DCanGuessNeutrals.GetBool() && TabId == 2) return true;
        if (!DCanGuessCoven.GetBool() && TabId == 3) return true;
        if (!DCanGuessAdt.GetBool() && TabId == 4) return true;

        return false;
    }

    public override bool GuessCheck(bool isUI, PlayerControl guesser, PlayerControl target, CustomRoles role, ref bool guesserSuicide)
    {
        if (CheckCantGuess || GuessesCountPerMeeting >= MaxNumberOfGuessesPerMeeting.GetInt())
        {
            guesser.ShowInfoMessage(isUI, GetString("DoomsayerCantGuess"));
            return true;
        }

        if (role.IsImpostor() && !DCanGuessImpostors.GetBool())
        {
            guesser.ShowInfoMessage(isUI, GetString("GuessNotAllowed"));
            return true;
        }
        if (role.IsCrewmate() && !DCanGuessCrewmates.GetBool())
        {
            guesser.ShowInfoMessage(isUI, GetString("GuessNotAllowed"));
            return true;
        }
        if (role.IsNeutral() && !DCanGuessNeutrals.GetBool())
        {
            guesser.ShowInfoMessage(isUI, GetString("GuessNotAllowed"));
            return true;
        }
        if (role.IsCoven() && !DCanGuessCoven.GetBool())
        {
            guesser.ShowInfoMessage(isUI, GetString("GuessNotAllowed"));
            return true;
        }
        if (role.IsAdditionRole() && !DCanGuessAdt.GetBool())
        {
            guesser.ShowInfoMessage(isUI, GetString("GuessAdtRole"));
            return true;
        }

        return false;
    }

    public override bool CheckMisGuessed(bool isUI, PlayerControl guesser, PlayerControl target, CustomRoles role, ref bool guesserSuicide)
    {
        if (target.Is(CustomRoles.Rebound) && guesser.Is(CustomRoles.Doomsayer) && !DoesNotSuicideWhenMisguessing.GetBool() && !GuessedRoles.Contains(role))
        {
            guesserSuicide = true;
            Logger.Info($"{guesser.GetNameWithRole().RemoveHtmlTags()} guessed {target.GetNameWithRole().RemoveHtmlTags()}, doomsayer suicide because rebound", "GuessManager");
        }
        else if (AdvancedSettings.GetBool())
        {
            if (GuessesCountPerMeeting >= MaxNumberOfGuessesPerMeeting.GetInt() && guesser.PlayerId != target.PlayerId)
            {
                CantGuess = true;
                guesser.ShowInfoMessage(isUI, GetString("DoomsayerCantGuess"));
                return true;
            }
            else
            {
                GuessesCountPerMeeting++;

                if (GuessesCountPerMeeting >= MaxNumberOfGuessesPerMeeting.GetInt())
                    CantGuess = true;
            }

            if (!KillCorrectlyGuessedPlayers.GetBool() && guesser.PlayerId != target.PlayerId)
            {
                guesser.ShowInfoMessage(isUI, GetString("DoomsayerCorrectlyGuessRole"));

                if (GuessedRoles.Contains(role))
                {
                    _ = new LateTask(() =>
                    {
                        SendMessage(GetString("DoomsayerGuessSameRoleAgainMsg"), guesser.PlayerId, ColorString(GetRoleColor(CustomRoles.Doomsayer), GetString("DoomsayerGuessCountTitle")));
                    }, 0.7f, "Doomsayer Guess Same Role Again Msg");
                }
                else
                {
                    guesser.RpcIncreaseAbilityUseLimitBy(1);
                    GuessedRoles.Add(role);

                    _ = new LateTask(() =>
                    {
                        SendMessage(string.Format(GetString("DoomsayerGuessCountMsg"), guesser.GetAbilityUseLimit()), guesser.PlayerId, ColorString(GetRoleColor(CustomRoles.Doomsayer), GetString("DoomsayerGuessCountTitle")));
                    }, 0.7f, "Doomsayer Guess Msg 1");
                }

                CheckCountGuess(guesser);

                return true;
            }
            else if (DoesNotSuicideWhenMisguessing.GetBool() && guesser.PlayerId == target.PlayerId)
            {
                guesser.ShowInfoMessage(isUI, GetString("DoomsayerNotCorrectlyGuessRole"));

                if (MisguessRolePrevGuessRoleUntilNextMeeting.GetBool())
                {
                    CantGuess = true;
                }

                return true;
            }
        }

        return false;
    }

    public void SendMessageAboutGuess(PlayerControl guesser, PlayerControl playerMisGuessed, CustomRoles role)
    {
        if (guesser.Is(CustomRoles.Doomsayer) && guesser.PlayerId != playerMisGuessed.PlayerId)
        {
            guesser.RpcIncreaseAbilityUseLimitBy(1);

            if (!GuessedRoles.Contains(role))
                GuessedRoles.Add(role);

            CheckCountGuess(guesser);

            _ = new LateTask(() =>
            {
                SendMessage(string.Format(GetString("DoomsayerGuessCountMsg"), guesser.GetAbilityUseLimit()), guesser.PlayerId, ColorString(GetRoleColor(CustomRoles.Doomsayer), GetString("DoomsayerGuessCountTitle")));
            }, 0.7f, "Doomsayer Guess Msg 2");
        }
    }
    public override bool CanUseKillButton(PlayerControl pc) => EasyMode.GetBool();
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = ObserveCooldown.GetFloat();

    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        if (!EasyMode.GetBool()) return false;
        if (!EvenEasierMode.GetBool()) MsgToSend[killer.PlayerId].Add(string.Format(ObserveRiddleMsg(target), target.GetRealName()));
        else MsgToSend[killer.PlayerId].Add(string.Format(ObserveRiddleMsg(target), target.GetRealName()) + "<br><size=1>" + ObserveRolesMsg(target) + "</size>");
        killer.Notify(string.Format(GetString("DoomsayerObserveNotif"), target.GetRealName()));
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        return false;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton?.OverrideText(GetString("DoomsayerKillButtonText"));
    }

    // i hate this code so much no wonder why we reworked fortune teller, oh well, here we go again
    public static string ObserveRiddleMsg(PlayerControl player)
    {
        string result = "DoomsayerObserve.";
        var role = player.GetCustomRole();
        if (role.IsGhostRole())
        {
            return GetString(result + "Dead");
        }
        if (Main.PlayerStates[player.PlayerId].IsNecromancer)
        {
            return GetString(result + "Bodies");
        }
        if (role.IsVanilla())
        {
            return GetString(result + "Basic");
        }
        if (role.IsRevealingRole(player))
        {
            return GetString(result + "Obvious");
        }
        switch (role)
        {
            case CustomRoles.Arrogance:
            case CustomRoles.Berserker:
            case CustomRoles.Blackmailer:
            case CustomRoles.Bomber:
            case CustomRoles.Captain:
            case CustomRoles.ChiefOfPolice:
            case CustomRoles.Councillor:
            case CustomRoles.CovenLeader:
            case CustomRoles.Death:
            case CustomRoles.Demon:
            case CustomRoles.Dictator:
            case CustomRoles.God:
            case CustomRoles.Godfather:
            case CustomRoles.Famine:
            case CustomRoles.Infectious:
            case CustomRoles.Instigator:
            case CustomRoles.Jailer:
            case CustomRoles.Judge:
            case CustomRoles.Mayor:
            case CustomRoles.Marshall:
            case CustomRoles.Monarch:
            case CustomRoles.Parasite:
            case CustomRoles.Pestilence:
            case CustomRoles.Pitfall:
            case CustomRoles.Poisoner:
            case CustomRoles.President:
            case CustomRoles.SerialKiller:
            case CustomRoles.Shocker:
            case CustomRoles.Sheriff:
            case CustomRoles.Twister:
            case CustomRoles.Vampire:
            case CustomRoles.Vindicator:
            case CustomRoles.Virus:
            case CustomRoles.War:
                result += "Fear";
                break;
            case CustomRoles.Abyssbringer:
            case CustomRoles.Chameleon:
            case CustomRoles.Chronomancer:
            case CustomRoles.Conjurer:
            case CustomRoles.Deathpact:
            case CustomRoles.Eraser:
            case CustomRoles.Guardian:
            case CustomRoles.FortuneTeller:
            case CustomRoles.HexMaster:
            case CustomRoles.Lightning:
            case CustomRoles.Medusa:
            case CustomRoles.MoonDancer:
            case CustomRoles.Observer:
            case CustomRoles.Oracle:
            case CustomRoles.Overseer:
            case CustomRoles.Pixie:
            case CustomRoles.Psychic:
            case CustomRoles.RiftMaker:
            case CustomRoles.ShapeMaster:
            case CustomRoles.SoulCatcher:
            case CustomRoles.Specter:
            case CustomRoles.Swooper:
            case CustomRoles.TimeMaster:
            case CustomRoles.Transporter:
            case CustomRoles.Warlock:
            case CustomRoles.Wildling:
            case CustomRoles.Witch:
            case CustomRoles.Wraith:
            case CustomRoles.YinYanger:
                result += "Magic";
                break;
            case CustomRoles.Alchemist:
            case CustomRoles.Baker:
            case CustomRoles.Camouflager:
            case CustomRoles.Celebrity:
            case CustomRoles.Cleanser:
            case CustomRoles.Consigliere:
            case CustomRoles.CopyCat:
            case CustomRoles.Dazzler:
            case CustomRoles.Deputy:
            case CustomRoles.Doomsayer:
            case CustomRoles.EvilGuesser:
            case CustomRoles.EvilHacker:
            case CustomRoles.EvilTracker:
            case CustomRoles.Inspector:
            case CustomRoles.Investigator:
            case CustomRoles.Keeper:
            case CustomRoles.Knight:
            case CustomRoles.Lookout:
            case CustomRoles.Mechanic:
            case CustomRoles.Medic:
            case CustomRoles.Merchant:
            case CustomRoles.NiceGuesser:
            case CustomRoles.Pickpocket:
            case CustomRoles.PlagueDoctor:
            case CustomRoles.PlagueBearer:
            case CustomRoles.PotionMaster:
            case CustomRoles.QuickShooter:
            case CustomRoles.Ritualist:
            case CustomRoles.Sniper:
            case CustomRoles.Stealth:
            case CustomRoles.TaskManager:
            case CustomRoles.Telecommunication:
            case CustomRoles.TimeThief:
            case CustomRoles.Veteran:
            case CustomRoles.Visionary:
                result += "Skilled";
                break;
            case CustomRoles.Agitater:
            case CustomRoles.AntiAdminer:
            case CustomRoles.Bandit:
            case CustomRoles.Benefactor:
            case CustomRoles.Bodyguard:
            case CustomRoles.BountyHunter:
            case CustomRoles.Disperser:
            case CustomRoles.DoubleAgent:
            case CustomRoles.Escapist:
            case CustomRoles.Fireworker:
            case CustomRoles.Huntsman:
            case CustomRoles.Imitator:
            case CustomRoles.Jackal:
            case CustomRoles.KillingMachine:
            case CustomRoles.Lawyer:
            case CustomRoles.Lighter:
            case CustomRoles.Maverick:
            case CustomRoles.Miner:
            case CustomRoles.Ninja:
            case CustomRoles.Opportunist:
            case CustomRoles.Quizmaster:
            case CustomRoles.Seeker:
            case CustomRoles.TimeManager:
            case CustomRoles.Vector:
            case CustomRoles.Ventguard:
            case CustomRoles.Workaholic:
                result += "Dedicated";
                break;
            case CustomRoles.Addict:
            case CustomRoles.Cultist:
            case CustomRoles.Deceiver:
            case CustomRoles.Devourer:
            case CustomRoles.Doppelganger:
            case CustomRoles.Follower:
            case CustomRoles.Gangster:
            case CustomRoles.Greedy:
            case CustomRoles.Hangman:
            case CustomRoles.Hater:
            case CustomRoles.Inhibitor:
            case CustomRoles.Jinx:
            case CustomRoles.Kamikaze:
            case CustomRoles.LazyGuy:
            case CustomRoles.Ludopath:
            case CustomRoles.Lurker:
            case CustomRoles.Mini:
            case CustomRoles.Mole:
            case CustomRoles.Nemesis:
            case CustomRoles.Pacifist:
            case CustomRoles.Provocateur:
            case CustomRoles.PunchingBag:
            case CustomRoles.Puppeteer:
            case CustomRoles.Pyromaniac:
            case CustomRoles.Revolutionist:
            case CustomRoles.Saboteur:
            case CustomRoles.Sacrifist:
            case CustomRoles.Snitch:
            case CustomRoles.Spy:
            case CustomRoles.Stalker:
            case CustomRoles.Swapper:
            case CustomRoles.Taskinator:
            case CustomRoles.Terrorist:
            case CustomRoles.Traitor:
            case CustomRoles.Troller:
            case CustomRoles.Vigilante:
                result += "Shunned";
                break;
            case CustomRoles.Altruist:
            case CustomRoles.Amnesiac:
            case CustomRoles.Anonymous:
            case CustomRoles.Butcher:
            case CustomRoles.Cleaner:
            case CustomRoles.Coroner:
            case CustomRoles.CursedSoul:
            case CustomRoles.Detective:
            case CustomRoles.Doctor:
            case CustomRoles.Medium:
            case CustomRoles.Mortician:
            case CustomRoles.Necromancer:
            case CustomRoles.Pelican:
            case CustomRoles.Scavenger:
            case CustomRoles.SoulCollector:
            case CustomRoles.Spiritcaller:
            case CustomRoles.Spiritualist:
            case CustomRoles.Tracefinder:
            case CustomRoles.Trapster:
            case CustomRoles.Undertaker:
            case CustomRoles.Vulture:
            case CustomRoles.Witness:
            case CustomRoles.Zombie:
                result += "Bodies";
                break;
            case CustomRoles.Admirer:
            case CustomRoles.Bard:
            case CustomRoles.Bastion:
            case CustomRoles.Collector:
            case CustomRoles.Crewpostor:
            case CustomRoles.Crusader:
            case CustomRoles.CursedWolf:
            case CustomRoles.DollMaster:
            case CustomRoles.Enigma:
            case CustomRoles.Grenadier:
            case CustomRoles.Innocent:
            case CustomRoles.Jester:
            case CustomRoles.Mastermind:
            case CustomRoles.Mercenary:
            case CustomRoles.Penguin:
            case CustomRoles.Pursuer:
            case CustomRoles.Randomizer:
            case CustomRoles.Refugee:
            case CustomRoles.Retributionist:
            case CustomRoles.Revenant:
            case CustomRoles.Reverie:
            case CustomRoles.Romantic:
            case CustomRoles.RuthlessRomantic:
            case CustomRoles.VengefulRomantic:
            case CustomRoles.Shaman:
            case CustomRoles.Underdog:
            case CustomRoles.VoodooMaster:
            case CustomRoles.Werewolf:
                result += "Misunderstood";
                break;
            case CustomRoles.Solsticer:
                result += "Obvious";
                break;
            default:
                result += "Unknown";
                break;
        }
        return GetString(result);
    }
    public static string ObserveRolesMsg(PlayerControl player) {
        string result = "DoomsayerRoles.";
        var role = player.GetCustomRole();
        if (role.IsRevealingRole(player))
        {
            return string.Empty;
        }
        if (role.IsGhostRole())
        {
            return GetString(result + "Dead");
        }
        if (Main.PlayerStates[player.PlayerId].IsNecromancer)
        {
            return GetString(result + "Bodies");
        }
        if (role.IsVanilla())
        {
            return GetString(result + "Basic");
        }
        switch (role)
        {
            case CustomRoles.Arrogance:
            case CustomRoles.Berserker:
            case CustomRoles.Blackmailer:
            case CustomRoles.Bomber:
            case CustomRoles.Captain:
            case CustomRoles.ChiefOfPolice:
            case CustomRoles.Councillor:
            case CustomRoles.CovenLeader:
            case CustomRoles.Death:
            case CustomRoles.Demon:
            case CustomRoles.Dictator:
            case CustomRoles.God:
            case CustomRoles.Godfather:
            case CustomRoles.Famine:
            case CustomRoles.Infectious:
            case CustomRoles.Instigator:
            case CustomRoles.Jailer:
            case CustomRoles.Judge:
            case CustomRoles.Mayor:
            case CustomRoles.Marshall:
            case CustomRoles.Monarch:
            case CustomRoles.Parasite:
            case CustomRoles.Pestilence:
            case CustomRoles.Pitfall:
            case CustomRoles.Poisoner:
            case CustomRoles.President:
            case CustomRoles.SerialKiller:
            case CustomRoles.Shocker:
            case CustomRoles.Sheriff:
            case CustomRoles.Twister:
            case CustomRoles.Vampire:
            case CustomRoles.Vindicator:
            case CustomRoles.Virus:
            case CustomRoles.War:
                result += "Fear";
                break;
            case CustomRoles.Abyssbringer:
            case CustomRoles.Chameleon:
            case CustomRoles.Chronomancer:
            case CustomRoles.Conjurer:
            case CustomRoles.Deathpact:
            case CustomRoles.Eraser:
            case CustomRoles.Guardian:
            case CustomRoles.FortuneTeller:
            case CustomRoles.HexMaster:
            case CustomRoles.Lightning:
            case CustomRoles.Medusa:
            case CustomRoles.MoonDancer:
            case CustomRoles.Observer:
            case CustomRoles.Oracle:
            case CustomRoles.Overseer:
            case CustomRoles.Pixie:
            case CustomRoles.Psychic:
            case CustomRoles.RiftMaker:
            case CustomRoles.ShapeMaster:
            case CustomRoles.SoulCatcher:
            case CustomRoles.Specter:
            case CustomRoles.Swooper:
            case CustomRoles.TimeMaster:
            case CustomRoles.Transporter:
            case CustomRoles.Warlock:
            case CustomRoles.Wildling:
            case CustomRoles.Witch:
            case CustomRoles.Wraith:
            case CustomRoles.YinYanger:
                result += "Magic";
                break;
            case CustomRoles.Alchemist:
            case CustomRoles.Baker:
            case CustomRoles.Camouflager:
            case CustomRoles.Celebrity:
            case CustomRoles.Cleanser:
            case CustomRoles.Consigliere:
            case CustomRoles.CopyCat:
            case CustomRoles.Dazzler:
            case CustomRoles.Deputy:
            case CustomRoles.Doomsayer:
            case CustomRoles.EvilGuesser:
            case CustomRoles.EvilHacker:
            case CustomRoles.EvilTracker:
            case CustomRoles.Inspector:
            case CustomRoles.Investigator:
            case CustomRoles.Keeper:
            case CustomRoles.Knight:
            case CustomRoles.Lookout:
            case CustomRoles.Mechanic:
            case CustomRoles.Medic:
            case CustomRoles.Merchant:
            case CustomRoles.NiceGuesser:
            case CustomRoles.Pickpocket:
            case CustomRoles.PlagueDoctor:
            case CustomRoles.PlagueBearer:
            case CustomRoles.PotionMaster:
            case CustomRoles.QuickShooter:
            case CustomRoles.Ritualist:
            case CustomRoles.Sniper:
            case CustomRoles.Stealth:
            case CustomRoles.TaskManager:
            case CustomRoles.Telecommunication:
            case CustomRoles.TimeThief:
            case CustomRoles.Veteran:
            case CustomRoles.Visionary:
                result += "Skilled";
                break;
            case CustomRoles.Agitater:
            case CustomRoles.AntiAdminer:
            case CustomRoles.Bandit:
            case CustomRoles.Benefactor:
            case CustomRoles.Bodyguard:
            case CustomRoles.BountyHunter:
            case CustomRoles.Disperser:
            case CustomRoles.DoubleAgent:
            case CustomRoles.Escapist:
            case CustomRoles.Fireworker:
            case CustomRoles.Huntsman:
            case CustomRoles.Imitator:
            case CustomRoles.Jackal:
            case CustomRoles.KillingMachine:
            case CustomRoles.Lawyer:
            case CustomRoles.Lighter:
            case CustomRoles.Maverick:
            case CustomRoles.Miner:
            case CustomRoles.Ninja:
            case CustomRoles.Opportunist:
            case CustomRoles.Quizmaster:
            case CustomRoles.Seeker:
            case CustomRoles.TimeManager:
            case CustomRoles.Vector:
            case CustomRoles.Ventguard:
            case CustomRoles.Workaholic:
                result += "Dedicated";
                break;
            case CustomRoles.Addict:
            case CustomRoles.Cultist:
            case CustomRoles.Deceiver:
            case CustomRoles.Devourer:
            case CustomRoles.Doppelganger:
            case CustomRoles.Follower:
            case CustomRoles.Gangster:
            case CustomRoles.Greedy:
            case CustomRoles.Hangman:
            case CustomRoles.Hater:
            case CustomRoles.Inhibitor:
            case CustomRoles.Jinx:
            case CustomRoles.Kamikaze:
            case CustomRoles.LazyGuy:
            case CustomRoles.Ludopath:
            case CustomRoles.Lurker:
            case CustomRoles.Mini:
            case CustomRoles.Mole:
            case CustomRoles.Nemesis:
            case CustomRoles.Pacifist:
            case CustomRoles.Provocateur:
            case CustomRoles.PunchingBag:
            case CustomRoles.Puppeteer:
            case CustomRoles.Pyromaniac:
            case CustomRoles.Revolutionist:
            case CustomRoles.Saboteur:
            case CustomRoles.Sacrifist:
            case CustomRoles.Snitch:
            case CustomRoles.Spy:
            case CustomRoles.Stalker:
            case CustomRoles.Swapper:
            case CustomRoles.Taskinator:
            case CustomRoles.Terrorist:
            case CustomRoles.Traitor:
            case CustomRoles.Troller:
            case CustomRoles.Vigilante:
                result += "Shunned";
                break;
            case CustomRoles.Altruist:
            case CustomRoles.Amnesiac:
            case CustomRoles.Anonymous:
            case CustomRoles.Butcher:
            case CustomRoles.Cleaner:
            case CustomRoles.Coroner:
            case CustomRoles.CursedSoul:
            case CustomRoles.Detective:
            case CustomRoles.Doctor:
            case CustomRoles.Medium:
            case CustomRoles.Mortician:
            case CustomRoles.Necromancer:
            case CustomRoles.Pelican:
            case CustomRoles.Scavenger:
            case CustomRoles.SoulCollector:
            case CustomRoles.Spiritcaller:
            case CustomRoles.Spiritualist:
            case CustomRoles.Tracefinder:
            case CustomRoles.Trapster:
            case CustomRoles.Undertaker:
            case CustomRoles.Vulture:
            case CustomRoles.Witness:
            case CustomRoles.Zombie:
                result += "Bodies";
                break;
            case CustomRoles.Admirer:
            case CustomRoles.Bard:
            case CustomRoles.Bastion:
            case CustomRoles.Collector:
            case CustomRoles.Crewpostor:
            case CustomRoles.Crusader:
            case CustomRoles.CursedWolf:
            case CustomRoles.DollMaster:
            case CustomRoles.Enigma:
            case CustomRoles.Grenadier:
            case CustomRoles.Innocent:
            case CustomRoles.Jester:
            case CustomRoles.Mastermind:
            case CustomRoles.Mercenary:
            case CustomRoles.Penguin:
            case CustomRoles.Pursuer:
            case CustomRoles.Randomizer:
            case CustomRoles.Refugee:
            case CustomRoles.Retributionist:
            case CustomRoles.Revenant:
            case CustomRoles.Reverie:
            case CustomRoles.Romantic:
            case CustomRoles.RuthlessRomantic:
            case CustomRoles.VengefulRomantic:
            case CustomRoles.Shaman:
            case CustomRoles.Underdog:
            case CustomRoles.VoodooMaster:
            case CustomRoles.Werewolf:
                result += "Misunderstood";
                break;
            default: // also includes schrodinger's cat, glitch, illusionist, and trickster
                result += "Unknown";
                break;
        }
        if (result.Contains("Unknown")) return string.Empty;
        return GetString(result);
    }
    public override void OnMeetingHudStart(PlayerControl pc)
    {
        if (MsgToSend.ContainsKey(pc.PlayerId))
        {
            foreach (var msg in MsgToSend[pc.PlayerId])
            {
                AddMsg(msg, pc.PlayerId, ColorString(GetRoleColor(CustomRoles.Doomsayer), GetString("DoomsayerObserveTitle")));
            }
        }
    }
}
