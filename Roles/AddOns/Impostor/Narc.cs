using AmongUs.GameOptions;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Impostor;

public class Narc : IAddon
{
    private const int Id = 31200;
    public AddonTypes Type => AddonTypes.Misc;

    public static OptionItem MeetingsNeededForWin;
    public static OptionItem NarcCanSeeTeammates;
    public static OptionItem BecomeSheriffOnAllImpDead;
    public static OptionItem NarcCanUseSabotage;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Narc, canSetNum: false, tab: TabGroup.Addons);
        MeetingsNeededForWin = IntegerOptionItem.Create(Id + 10, "MeetingsNeededForWin", new(0, 10, 1), 5, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Narc])
            .SetValueFormat(OptionFormat.Times);
        NarcCanSeeTeammates = BooleanOptionItem.Create(Id + 11, "NarcCanSeeTeammates", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Narc]);
        BecomeSheriffOnAllImpDead = BooleanOptionItem.Create(Id + 12, "BecomeSheriffOnAllImpDead", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Narc]);
        NarcCanUseSabotage = BooleanOptionItem.Create(Id + 13, "NarcCanUseSabotage", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Narc]);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
    public static bool CancelMurder(PlayerControl killer, PlayerControl target)
    {
        var ShouldCancel = false;
        bool FirstTrigger = DoubleTrigger.FirstTriggerTimer.TryGetValue(killer.PlayerId, out _);
        if (target.Is(CustomRoles.Sheriff))
        {
            ShouldCancel = true;
        }
        else if ((!FirstTrigger || killer.Is(CustomRoles.Witch)) && BecomeSheriff(killer))
        {
            if (!Sheriff.CanBeKilledBySheriff(target))
            {
                killer.SetDeathReason(PlayerState.DeathReason.Misfire);
                killer.RpcMurderPlayer(killer);
                ShouldCancel = true;
            }
        }
        return ShouldCancel;
    }
    public static bool BecomeSheriff(PlayerControl pc)//Narc doesn't actually become a Sheriff.They just act like a Sheriff(like misfiring on Crewmates)
    {
        int impnum = Main.AllAlivePlayerControls.Count(x => !IsCrewAlignedAddon(x) && 
                                                      (x.GetCustomRole().IsImpostor() 
                                                      || (x.Is(CustomRoles.Crewpostor) && Crewpostor.KnowsAllies.GetBool())
                                                      ));
        return pc.CanUseKillButton() && (impnum == 0) && BecomeSheriffOnAllImpDead.GetBool();
    }
    private static bool IsCrewAlignedAddon(PlayerControl pc)
        => pc.Is(CustomRoles.Narc) || pc.Is(CustomRoles.Admired);
    public static bool CantUseSabotage(PlayerControl pc) => (pc.Is(CustomRoles.Narc) && !NarcCanUseSabotage.GetBool()) || BecomeSheriff(pc);
// Note:Narc Parasite and Narc Crewpostor are still shown as neutral to some roles
}
