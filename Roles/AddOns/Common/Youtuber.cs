using TOHE.Modules;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Youtuber
{
    private const int Id = 25500;

    public static OptionItem KillerWinsWithYouTuber;

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Youtuber, canSetNum: true, tab: TabGroup.Addons);
        KillerWinsWithYouTuber = BooleanOptionItem.Create(Id + 10, "Youtuber_KillerWinsWithYouTuber", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Youtuber]);
    }
    public static void OnMurderPlayer(PlayerControl killer, PlayerControl target)
    {
        target.SetDeathReason(PlayerState.DeathReason.Kill);
        target.SetRealKiller(killer, true);
        Main.PlayerStates[target.PlayerId].SetDead();

        CustomSoundsManager.RPCPlayCustomSoundAll("Congrats");

        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Youtuber);
        CustomWinnerHolder.WinnerIds.Add(target.PlayerId);
    }
}
