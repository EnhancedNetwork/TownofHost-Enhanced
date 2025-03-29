using AmongUs.GameOptions;
using System.Text;
using TOHE.Modules;
using TOHE.Roles.Core;
using TOHE.Roles.Coven;
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
    public override bool IsDesyncRole => EasyMode.GetBool();
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
        => IsForMeeting && seer.IsAlive() && target.IsAlive() ? ColorString(GetRoleColor(CustomRoles.Doomsayer), target.PlayerId.ToString()) + " " + TargetPlayerName : string.Empty;


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
        MsgToSend[killer.PlayerId].Add(string.Format(ObserveRiddleMsg(target), target.GetRealName()));
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
        if (role == CustomRoles.GM)
        {
            return GetString(result + "GM");
        }
        if (role.IsGhostRole())
        {
            return GetString(result + "Dead");
        }
        if (Main.PlayerStates[player.PlayerId].IsNecromancer || Illusionist.IsNonCovIllusioned(player.PlayerId)|| role is CustomRoles.SchrodingersCat or CustomRoles.Glitch)
        {
            return GetString(result + "Unknown");
        }
        if (role.IsVanilla() || Illusionist.IsCovIllusioned(player.PlayerId) || role == CustomRoles.Trickster)
        {
            return GetString(result + "Basic");
        }
        if (role.IsRevealingRole(player) || role == CustomRoles.Solsticer)
        {
            return GetString(result + "Obvious");
        }
        if (DCanGuessAdt.GetBool() && player.IsAnySubRole(sub => sub.IsConverted()))
        {
            return GetString(result + "Secret");
        }
        if (role == CustomRoles.God)
        {
            return GetString(result + "Fear");
        }
        switch (role.GetCustomRoleType())
        {
            case Custom_RoleType.CrewmateVanilla:          
            case Custom_RoleType.CrewmateBasic:          
            case Custom_RoleType.NeutralBenign:          
            case Custom_RoleType.ImpostorVanilla:   
                result += "Basic";
                break;
            case Custom_RoleType.CrewmatePower:
            case Custom_RoleType.CovenPower:
            case Custom_RoleType.NeutralApocalypse:
                result += "Fear";
                break;
            case Custom_RoleType.CrewmateSupport:
            case Custom_RoleType.CovenUtility:
            case Custom_RoleType.ImpostorSupport:
            case Custom_RoleType.NeutralEvil:
            case Custom_RoleType.NeutralChaos:
                result += "Dedicated";
                break;
            case Custom_RoleType.Madmate:
            case Custom_RoleType.CovenTrickery:
            case Custom_RoleType.ImpostorHindering:
            case Custom_RoleType.ImpostorConcealing:
                result += "Secret";
                break;
            case Custom_RoleType.NeutralKilling:
            case Custom_RoleType.CovenKilling:
            case Custom_RoleType.ImpostorKilling:
            case Custom_RoleType.CrewmateKilling:
                result += "Skilled";
                break;
            default:
                result += "Unknown";
                break;
        }
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
