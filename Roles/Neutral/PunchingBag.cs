using System.Text;
using TOHE.Modules;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Neutral;

internal class PunchingBag : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.PunchingBag;
    private const int Id = 14500;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralEvil;
    //==================================================================\\

    private static OptionItem PunchingBagKillMax;

    private readonly HashSet<byte> BlockGuess = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.PunchingBag);
        PunchingBagKillMax = IntegerOptionItem.Create(Id + 2, "PunchingBagKillMax", new(1, 30, 1), 5, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.PunchingBag])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        BlockGuess.Clear();
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(0);
    }
    public override string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        Color TextColor = GetRoleColor(CustomRoles.PunchingBag).ShadeColor(0.25f);

        ProgressText.Append(ColorString(TextColor, $"({playerId.GetAbilityUseLimit()}/{PunchingBagKillMax.GetInt()})"));
        return ProgressText.ToString();
    }
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        killer.SetKillCooldown(target: target, forceAnime: true);
        target.RpcIncreaseAbilityUseLimitBy(1);

        target.Notify(string.Format(GetString("PunchingBagKill"), target.GetAbilityUseLimit()));
        CheckWin();
        return false;
    }
    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl pc, CustomRoles role, ref bool guesserSuicide)
    {
        if (role != CustomRoles.PunchingBag) return false;
        if (BlockGuess.Contains(pc.PlayerId))
        {
            pc.ShowInfoMessage(isUI, GetString("GuessPunchingBagAgain"));
            return true;
        }

        pc.ShowInfoMessage(isUI, GetString("GuessPunchingBag"));
        BlockGuess.Add(pc.PlayerId);

        target.RpcIncreaseAbilityUseLimitBy(1);

        CheckWin();
        return true;
    }
    private void CheckWin()
    {
        var punchingBagId = _Player.PlayerId;

        if (punchingBagId.GetAbilityUseLimit() >= PunchingBagKillMax.GetInt())
        {
            if (!CustomWinnerHolder.CheckForConvertedWinner(punchingBagId))
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.PunchingBag);
                CustomWinnerHolder.WinnerIds.Add(punchingBagId);
            }
        }
    }
    public override bool GuessCheck(bool isUI, PlayerControl pc, PlayerControl target, CustomRoles role, ref bool guesserSuicide)
    {
        pc.ShowInfoMessage(isUI, GetString("GuessPunchingBagBlocked"));
        return true;
    }
}
