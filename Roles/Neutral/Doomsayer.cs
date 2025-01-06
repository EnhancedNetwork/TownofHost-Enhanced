using Hazel;
using UnityEngine;
using static TOHE.Utils;
using static TOHE.Translator;
using TOHE.Roles.Core;
using InnerNet;
using AmongUs.GameOptions;

namespace TOHE.Roles.Neutral;

internal class Doomsayer : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 14100;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Doomsayer);
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralEvil;
    //==================================================================\\

    private readonly HashSet<CustomRoles> GuessedRoles = [];
    private readonly Dictionary<byte, int> GuessingToWin = [];

    private int GuessesCount = 0;
    private int GuessesCountPerMeeting = 0;
    private static bool CantGuess = false;

    private static OptionItem DoomsayerAmountOfGuessesToWin;
    private static OptionItem DCanGuessImpostors;
    private static OptionItem DCanGuessCrewmates;
    private static OptionItem DCanGuessNeutrals;
    private static OptionItem DCanGuessAdt;
    private static OptionItem AdvancedSettings;
    private static OptionItem MaxNumberOfGuessesPerMeeting;
    private static OptionItem KillCorrectlyGuessedPlayers;
    public static OptionItem DoesNotSuicideWhenMisguessing;
    private static OptionItem MisguessRolePrevGuessRoleUntilNextMeeting;
    private static OptionItem DoomsayerTryHideMsg;
    private static OptionItem ImpostorVision;

    public override void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Doomsayer);
        DoomsayerAmountOfGuessesToWin = IntegerOptionItem.Create(Id + 10, "DoomsayerAmountOfGuessesToWin", new(1, 10, 1), 3, TabGroup.NeutralRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Doomsayer])
            .SetValueFormat(OptionFormat.Times);
        DCanGuessImpostors = BooleanOptionItem.Create(Id + 12, "DCanGuessImpostors", true, TabGroup.NeutralRoles, true)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Doomsayer]);
        DCanGuessCrewmates = BooleanOptionItem.Create(Id + 13, "DCanGuessCrewmates", true, TabGroup.NeutralRoles, true)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Doomsayer]);
        DCanGuessNeutrals = BooleanOptionItem.Create(Id + 14, "DCanGuessNeutrals", true, TabGroup.NeutralRoles, true)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Doomsayer]);
        DCanGuessAdt = BooleanOptionItem.Create(Id + 15, "DCanGuessAdt", false, TabGroup.NeutralRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Doomsayer]);

        AdvancedSettings = BooleanOptionItem.Create(Id + 16, "DoomsayerAdvancedSettings", true, TabGroup.NeutralRoles, true)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Doomsayer]);
        MaxNumberOfGuessesPerMeeting = IntegerOptionItem.Create(Id + 23, "DoomsayerMaxNumberOfGuessesPerMeeting", new(1, 10, 1), 3, TabGroup.NeutralRoles, false)
            .SetParent(AdvancedSettings);
        KillCorrectlyGuessedPlayers = BooleanOptionItem.Create(Id + 18, "DoomsayerKillCorrectlyGuessedPlayers", true, TabGroup.NeutralRoles, true)
            .SetParent(AdvancedSettings);
        DoesNotSuicideWhenMisguessing = BooleanOptionItem.Create(Id + 24, "DoomsayerDoesNotSuicideWhenMisguessing", true, TabGroup.NeutralRoles, false)
            .SetParent(AdvancedSettings);
        MisguessRolePrevGuessRoleUntilNextMeeting = BooleanOptionItem.Create(Id + 20, "DoomsayerMisguessRolePrevGuessRoleUntilNextMeeting", true, TabGroup.NeutralRoles, true)
            .SetParent(DoesNotSuicideWhenMisguessing);

        ImpostorVision = BooleanOptionItem.Create(Id + 25, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Doomsayer]);
        DoomsayerTryHideMsg = BooleanOptionItem.Create(Id + 21, "DoomsayerTryHideMsg", true, TabGroup.NeutralRoles, true)
            .SetColor(Color.green)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Doomsayer]);
    }
    public override void Init()
    {
        CantGuess = false;
    }
    public override void Add(byte playerId)
    {
        GuessingToWin.TryAdd(playerId, GuessesCount);
    }
    public void SendRPC(PlayerControl player)
    {
        MessageWriter writer;
        writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
        writer.Write(player.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte DoomsayerId = reader.ReadByte();
        GuessingToWin[DoomsayerId]++;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(ImpostorVision.GetBool());
    private (int, int) GuessedPlayerCount(byte doomsayerId)
    {
        int GuessesToWin = GuessingToWin[doomsayerId], AmountOfGuessesToWin = DoomsayerAmountOfGuessesToWin.GetInt();

        return (GuessesToWin, AmountOfGuessesToWin);
    }
    public override string GetProgressText(byte playerId, bool comms)
    {
        var (GuessingToWin, AmountOfGuessesToWin) = GuessedPlayerCount(playerId);
        return ColorString(GetRoleColor(CustomRoles.Doomsayer).ShadeColor(0.25f), $"({GuessingToWin}/{AmountOfGuessesToWin})");
        
    }

    public static bool CheckCantGuess = CantGuess;
    public static bool NeedHideMsg(PlayerControl pc) => pc.Is(CustomRoles.Doomsayer) && DoomsayerTryHideMsg.GetBool();
    
    private void CheckCountGuess(PlayerControl doomsayer)
    {
        if (!(GuessingToWin[doomsayer.PlayerId] >= DoomsayerAmountOfGuessesToWin.GetInt())) return;

        GuessingToWin[doomsayer.PlayerId] = DoomsayerAmountOfGuessesToWin.GetInt();
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
    public override string PVANameText(PlayerVoteArea pva, PlayerControl seer, PlayerControl target)
        => seer.IsAlive() && target.IsAlive() ? ColorString(GetRoleColor(CustomRoles.Doomsayer), target.PlayerId.ToString()) + " " + pva.NameText.text : string.Empty;


    public static bool HideTabInGuesserUI(int TabId)
    {
        if (!DCanGuessCrewmates.GetBool() && TabId == 0) return true;
        if (!DCanGuessImpostors.GetBool() && TabId == 1) return true;
        if (!DCanGuessNeutrals.GetBool() && TabId == 2) return true;
        if (!DCanGuessAdt.GetBool() && TabId == 3) return true;

        return false;
    }

    public override bool GuessCheck(bool isUI, PlayerControl guesser, PlayerControl target, CustomRoles role, ref bool guesserSuicide)
    {
        if (CheckCantGuess)
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
                    GuessingToWin[guesser.PlayerId]++;
                    SendRPC(guesser);
                    GuessedRoles.Add(role);

                    _ = new LateTask(() =>
                    {
                        SendMessage(string.Format(GetString("DoomsayerGuessCountMsg"), GuessingToWin[guesser.PlayerId]), guesser.PlayerId, ColorString(GetRoleColor(CustomRoles.Doomsayer), GetString("DoomsayerGuessCountTitle")));
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
            GuessingToWin[guesser.PlayerId]++;
            SendRPC(guesser);

            if (!GuessedRoles.Contains(role))
                GuessedRoles.Add(role);

            CheckCountGuess(guesser);

            _ = new LateTask(() =>
            {
                SendMessage(string.Format(GetString("DoomsayerGuessCountMsg"), GuessingToWin[guesser.PlayerId]), guesser.PlayerId, ColorString(GetRoleColor(CustomRoles.Doomsayer), GetString("DoomsayerGuessCountTitle")));
            }, 0.7f, "Doomsayer Guess Msg 2");
        }
    }
}