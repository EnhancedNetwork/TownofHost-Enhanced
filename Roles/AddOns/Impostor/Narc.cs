using AmongUs.GameOptions;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Impostor;

public class Narc : IAddon
{
    private const int Id = 31200;
    public AddonTypes Type => AddonTypes.Misc;
    private static readonly HashSet<byte> playerIdList = [];
    private static readonly HashSet<byte> ReporterList = [];

    public static OptionItem MeetingsNeededForWin;
    public static OptionItem NarcCanSeeTeammates;
    public static OptionItem NarcCanKillMadmate;
    public static OptionItem NarcCanUseSabotage;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Narc, canSetNum: false, tab: TabGroup.Addons);
        MeetingsNeededForWin = IntegerOptionItem.Create(Id + 10, "MeetingsNeededForWin", new(0, 10, 1), 5, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Narc])
            .SetValueFormat(OptionFormat.Times);
        NarcCanSeeTeammates = BooleanOptionItem.Create(Id + 11, "NarcCanSeeTeammates", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Narc]);
        NarcCanKillMadmate = BooleanOptionItem.Create(Id + 12, "NarcCanKillMadmate", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Narc]);
        NarcCanUseSabotage = BooleanOptionItem.Create(Id + 13, "NarcCanUseSabotage", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Narc]);
    }
    public void Init()
    {
        playerIdList.Clear();
        ReporterList.Clear();
    }
    public void Add(byte playerId, bool gameIsLoading = true)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);
    }
    public void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
    }

    //Narc Checkmurder
    public static bool CancelMurder(PlayerControl killer, PlayerControl target)
    {
        var ShouldCancel = false;
        bool FirstTrigger = DoubleTrigger.FirstTriggerTimer.TryGetValue(killer.PlayerId, out _);
        if (target.Is(CustomRoles.Sheriff))
        {
            ShouldCancel = true;
        }
        else if ((!FirstTrigger || killer.Is(CustomRoles.Witch)) 
        && target.Is(CustomRoles.ChiefOfPolice) && !target.GetCustomRole().IsConverted())
        {
            killer.SetDeathReason(PlayerState.DeathReason.Misfire);
            killer.RpcMurderPlayer(killer);
            ShouldCancel = true;
        }
        return ShouldCancel;
    }

//If Narc starts a meeting and gets an impostor/madmate ejected,set Narc as real killer
//It's the base code of a feature I'm planning to add to Narc
    public static void OnReportDeadBody(PlayerControl reporter)
    {
        foreach (var playerId in playerIdList.ToArray())
        {
            if (reporter.PlayerId == playerId)
                ReporterList.Add(reporter.PlayerId);
            else ReporterList.Remove(playerId);
        }
    }
    public static void OnPlayerExiled(NetworkedPlayerInfo exiled)
    {
        var ejected = exiled.Object;
        foreach (var playerId in ReporterList.ToArray())
        {
            var narc = playerId.GetPlayer();
            if (IsImpostorAligned(ejected) && ejected.GetRealKiller() == null)
            {
                ejected.SetRealKiller(narc);//I used SetRealKiller as a sign for whether the code works well 
                ReporterList.Clear();
            }
        }
    } 
//end of OnPlayerExiled

    private static bool IsImpostorAligned(PlayerControl pc)
        => (CustomRolesHelper.IsNarcImpV3(pc) && !pc.Is(CustomRoles.Admired))
        || pc.Is(CustomRoles.Madmate);

    public static bool CantUseSabotage(PlayerControl pc) => pc.Is(CustomRoles.Narc) && !NarcCanUseSabotage.GetBool();
// Note:Narc Parasite and Narc Crewpostor are still shown as neutral to some roles
}
