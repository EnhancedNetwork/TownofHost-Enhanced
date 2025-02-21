using AmongUs.GameOptions;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Terrorist : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Terrorist;
    private const int id = 15400;

    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralChaos;
    //==================================================================\\
    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute) => !ForRecompute;

    public static OptionItem CanTerroristSuicideWin;
    public static OptionItem TerroristCanGuess;

    public override void SetupCustomOption()
    {

        SetupRoleOptions(15400, TabGroup.NeutralRoles, CustomRoles.Terrorist);
        CanTerroristSuicideWin = BooleanOptionItem.Create(15402, "CanTerroristSuicideWin", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Terrorist]);
        TerroristCanGuess = BooleanOptionItem.Create(15403, GeneralOption.CanGuess, true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Terrorist]);
        OverrideTasksData.Create(15404, TabGroup.NeutralRoles, CustomRoles.Terrorist);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = 1f;
        AURoleOptions.EngineerInVentMaxTime = 0f;
    }
    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (target.IsDisconnected()) return;

        Logger.Info(target?.Data?.PlayerName + " was Terrorist", "AfterPlayerDeathTasks");
        CheckTerroristWin(target.Data);
    }
    public override void CheckExile(NetworkedPlayerInfo exiled, ref bool DecidedWinner, bool isMeetingHud, ref string name)
    {
        CheckTerroristWin(exiled);
    }
    private static void CheckTerroristWin(NetworkedPlayerInfo terroristData)
    {
        var terrorist = terroristData.Object;
        if (terrorist == null) return;

        var state = Main.PlayerStates[terrorist.PlayerId];
        var taskState = terrorist.GetPlayerTaskState();
        if (taskState.IsTaskFinished && (!state.IsSuicide || CanTerroristSuicideWin.GetBool()) && (state.deathReason != PlayerState.DeathReason.Armageddon))
        {
            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return;

            if (!CustomWinnerHolder.CheckForConvertedWinner(terrorist.PlayerId))
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Terrorist);
                CustomWinnerHolder.WinnerIds.Add(terrorist.PlayerId);
            }
            foreach (var pc in Main.AllPlayerControls)
            {
                if (pc.Is(CustomRoles.Terrorist))
                {
                    if (Main.PlayerStates[pc.PlayerId].deathReason == PlayerState.DeathReason.Vote)
                    {
                        pc.SetDeathReason(PlayerState.DeathReason.etc);
                    }
                    else
                    {
                        pc.SetDeathReason(PlayerState.DeathReason.Suicide);
                    }
                }
                else if (pc.IsAlive())
                {
                    pc.SetDeathReason(PlayerState.DeathReason.Bombed);
                    Main.PlayerStates[pc.PlayerId].SetDead();
                    pc.RpcMurderPlayer(pc);
                    pc.SetRealKiller(terrorist);
                }
            }
        }
    }
    public override bool GuessCheck(bool isUI, PlayerControl guesser, PlayerControl pc, CustomRoles role, ref bool guesserSuicide)
    {
        if (!TerroristCanGuess.GetBool())
        {
            Logger.Info($"Guess Disabled for this player {guesser.PlayerId}", "GuessManager");
            guesser.ShowInfoMessage(isUI, GetString("GuessDisabled"));
            return true;
        }
        return false;
    }
}
