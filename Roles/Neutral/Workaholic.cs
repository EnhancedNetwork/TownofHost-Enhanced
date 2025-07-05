using AmongUs.GameOptions;
using static TOHE.MeetingHudStartPatch;
using static TOHE.Options;
using static TOHE.Translator;

//Thanks TOH_Y
namespace TOHE.Roles.Neutral;

internal class Workaholic : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Workaholic;
    private const int Id = 15800;
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralChaos;
    //==================================================================\\
    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute) => !ForRecompute;

    public static OptionItem WorkaholicCannotWinAtDeath;
    public static OptionItem WorkaholicVentCooldown;
    public static OptionItem WorkaholicVisibleToEveryone;
    public static OptionItem WorkaholicGiveAdviceAlive;
    public static OptionItem WorkaholicCanGuess;

    public static readonly HashSet<byte> WorkaholicAlive = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(15700, TabGroup.NeutralRoles, CustomRoles.Workaholic); //TOH_Y
        WorkaholicCannotWinAtDeath = BooleanOptionItem.Create(15702, "WorkaholicCannotWinAtDeath", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Workaholic]);
        WorkaholicVentCooldown = FloatOptionItem.Create(15703, GeneralOption.EngineerBase_VentCooldown, new(0f, 180f, 2.5f), 0f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Workaholic])
            .SetValueFormat(OptionFormat.Seconds);
        WorkaholicVisibleToEveryone = BooleanOptionItem.Create(15704, "WorkaholicVisibleToEveryone", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Workaholic]);
        WorkaholicGiveAdviceAlive = BooleanOptionItem.Create(15705, "WorkaholicGiveAdviceAlive", true, TabGroup.NeutralRoles, false)
            .SetParent(WorkaholicVisibleToEveryone);
        WorkaholicCanGuess = BooleanOptionItem.Create(15706, GeneralOption.CanGuess, true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Workaholic]);
        OverrideTasksData.Create(15707, TabGroup.NeutralRoles, CustomRoles.Workaholic);
    }
    public override void Init()
    {
        WorkaholicAlive.Clear();

    }

    public static bool OthersKnowWorka(PlayerControl target)
        => WorkaholicVisibleToEveryone.GetBool() && target.Is(CustomRoles.Workaholic);

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = WorkaholicVentCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = 0f;
    }
    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        var taskState = player.GetPlayerTaskState();
        if (!taskState.IsTaskFinished || (WorkaholicCannotWinAtDeath.GetBool() && !player.IsAlive())) return true;

        Logger.Info("The Workaholic task is done", "Workaholic");
        if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return true;

        if (!CustomWinnerHolder.CheckForConvertedWinner(player.PlayerId))
        {
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Workaholic); //Workaholic win
            CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
        }

        RPC.PlaySoundRPC(Sounds.KillSound, player.PlayerId);
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.PlayerId != player.PlayerId)
            {
                var deathReason = pc.PlayerId == player.PlayerId ?
                    PlayerState.DeathReason.Overtired : PlayerState.DeathReason.Ashamed;

                pc.SetDeathReason(deathReason);
                pc.RpcMurderPlayer(pc);
                pc.SetRealKiller(player);
            }
        }

        return true;
    }
    public override void OnMeetingHudStart(PlayerControl player)
    {
        if (MeetingStates.FirstMeeting && player.IsAlive() && WorkaholicGiveAdviceAlive.GetBool() && !WorkaholicCannotWinAtDeath.GetBool() && !GhostIgnoreTasks.GetBool())
        {
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Workaholic)).ToArray())
            {
                WorkaholicAlive.Add(pc.PlayerId);
            }
            List<string> workaholicAliveList = [];
            foreach (var whId in WorkaholicAlive.ToArray())
            {
                workaholicAliveList.Add(Main.AllPlayerNames[whId]);
            }
            string separator = TranslationController.Instance.currentLanguage.languageID is SupportedLangs.English or SupportedLangs.Russian ? "], [" : "】, 【";
            AddMsg(string.Format(GetString("WorkaholicAdviceAlive"), string.Join(separator, workaholicAliveList)), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Workaholic), GetString("Workaholic").ToUpper()));
        }
    }
    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl pc, CustomRoles role, ref bool guesserSuicide)
    {
        if (role != CustomRoles.Workaholic) return false;
        if (WorkaholicVisibleToEveryone.GetBool())
        {
            if (!isUI) Utils.SendMessage(GetString("GuessWorkaholic"), pc.PlayerId);
            else pc.ShowPopUp(GetString("GuessWorkaholic"));
            return true;
        }
        return false;
    }
    public override bool GuessCheck(bool isUI, PlayerControl guesser, PlayerControl pc, CustomRoles role, ref bool guesserSuicide)
    {
        if (!WorkaholicCanGuess.GetBool())
        {
            Logger.Info($"Guess Disabled for this player {guesser.PlayerId}", "GuessManager");
            guesser.ShowInfoMessage(isUI, GetString("GuessDisabled"));
            return true;
        }
        return false;
    }
}
