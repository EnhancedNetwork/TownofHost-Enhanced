using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Masochist : RoleBase// bad roll, plz don't use this hosts
{
    //===========================SETUP================================\\
    private const int Id = 14500;
    private static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    //==================================================================\\

    private static OptionItem MasochistKillMax;
    
    private static readonly Dictionary<byte, int> MasochistMax = [];

    public static void SetupCustomOptions()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Masochist);
        MasochistKillMax = IntegerOptionItem.Create(Id + 2, "MasochistKillMax", new(1, 30, 1), 5, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Masochist])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        PlayerIds.Clear();
        MasochistMax.Clear();
    }
    public override void Add(byte playerId)
    {
        PlayerIds.Add(playerId);
        MasochistMax.Add(playerId, 0);
    }
    public override string GetProgressText(byte playerId, bool comms)
        => ColorString(GetRoleColor(CustomRoles.Masochist).ShadeColor(0.25f), $"({(MasochistMax.TryGetValue(playerId, out var count3) ? count3 : 0)}/{MasochistKillMax.GetInt()})");
    
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        killer.SetKillCooldown(target: target, forceAnime: true);
        MasochistMax[target.PlayerId]++;
        //    killer.RPCPlayCustomSound("DM");
        target.Notify(string.Format(GetString("MasochistKill"), MasochistMax[target.PlayerId]));
        if (MasochistMax[target.PlayerId] >= MasochistKillMax.GetInt())
        {
            if (!CustomWinnerHolder.CheckForConvertedWinner(target.PlayerId))
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Masochist);
                CustomWinnerHolder.WinnerIds.Add(target.PlayerId);
            }
        }
        return false;
    }
    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl pc, CustomRoles role, ref bool guesserSuicide)
    {

        if (target.Is(CustomRoles.Masochist))
        {
            if (!isUI) SendMessage(GetString("GuessMasochist"), pc.PlayerId);
            else pc.ShowPopUp(GetString("GuessMasochist"));
            MasochistMax[target.PlayerId]++;

            if (MasochistMax[target.PlayerId] >= MasochistKillMax.GetInt())
            {
                if (!CustomWinnerHolder.CheckForConvertedWinner(target.PlayerId))
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Masochist);
                    CustomWinnerHolder.WinnerIds.Add(target.PlayerId);
                }
            }
            return true;
        }
        if (pc.Is(CustomRoles.Masochist) && target.PlayerId == pc.PlayerId)
        {
            if (!isUI) SendMessage(GetString("SelfGuessMasochist"), pc.PlayerId);
            else pc.ShowPopUp(GetString("SelfGuessMasochist"));
            guesserSuicide = true;
            Logger.Msg($"Is Active: {guesserSuicide}", "guesserSuicide - Masochist");
        }

        return false;
    }
    public override bool GuessCheck(bool isUI, PlayerControl pc, PlayerControl target, CustomRoles role, ref bool guesserSuicide)
    {
        if (pc.Is(CustomRoles.Masochist))
        {
            if (!isUI) SendMessage(GetString("GuessMasochistBlocked"), pc.PlayerId);
            else pc.ShowPopUp(GetString("GuessMasochistBlocked"));
            return true;
        }
        return false;
    }
}
