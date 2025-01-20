using AmongUs.GameOptions;
using TOHE.Roles.Core;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.AddOns.Impostor;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Impostor;

public class Narc : IAddon
{
    public CustomRoles Role => CustomRoles.Narc;
    private const int Id = 31200;
    public AddonTypes Type => AddonTypes.Experimental;

    public static OptionItem NarcSpawnChance;
    public static OptionItem MeetingsNeededForWin;
    public static OptionItem NarcCanSeeTeammates;
    public static OptionItem NarcCanKillMadmate;
    private static OptionItem NarcCanUseSabotage;
    private static OptionItem NarcHasCrewVision;
    public static OptionItem MadmateCanBeNarc;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Narc, canSetNum: false, tab: TabGroup.Addons, canSetChance: false);
        NarcSpawnChance = IntegerOptionItem.Create(Id + 9, "ChanceToSpawn", new(0, 100, 5), 65, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Narc])
            .SetValueFormat(OptionFormat.Percent);
        MeetingsNeededForWin = IntegerOptionItem.Create(Id + 10, "MeetingsNeededForWin", new(0, 10, 1), 5, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Narc])
            .SetValueFormat(OptionFormat.Times);
        NarcCanSeeTeammates = BooleanOptionItem.Create(Id + 11, "NarcCanSeeTeammates", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Narc]);
        NarcCanKillMadmate = BooleanOptionItem.Create(Id + 12, "NarcCanKillMadmate", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Narc]);
        NarcCanUseSabotage = BooleanOptionItem.Create(Id + 13, "NarcCanUseSabotage", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Narc]);
        NarcHasCrewVision = BooleanOptionItem.Create(Id + 14, "NarcHasCrewVision", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Narc]);
        MadmateCanBeNarc = BooleanOptionItem.Create(Id + 15, "MadmateCanBeNarc", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Narc]);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }

///----------------------------------------Check Narc Assign----------------------------------------///
    public static bool CheckNarcAssign()
        => IRandom.Instance.Next(1, 100) <= NarcSpawnChance.GetInt() && CustomRoles.Narc.IsEnable();

    private static bool CheckAddMMSpot()
    {
        int optimpnum = Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors);
        int optmmnum = Options.NumberOfMadmates.GetInt();
        int totalimpnum = optimpnum + optmmnum;
        int assignvalue = IRandom.Instance.Next(1, totalimpnum);

        bool mmisnarc = true;
        if (optmmnum == 0) mmisnarc = false;
        else if (!MadmateCanBeNarc.GetBool()) mmisnarc = false;
        else if (assignvalue > optmmnum) mmisnarc = false;
    
        return mmisnarc;
    }

    public static int ExtraImpSpotNarc
        => (CheckNarcAssign() && !CheckAddMMSpot()) ? 1 : 0;
        
    public static int ExtraMadSpotNarc
        => (CheckNarcAssign() && CheckAddMMSpot()) ? 1 : 0;

    public static bool RemoveTheseRoles(CustomRoles role)
        => role is CustomRoles.Egoist or CustomRoles.Mare or CustomRoles.Mimic;
///-------------------------------------------------------------------------------------------------///

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

    public static void ApplyGameOptions(IGameOptions opt, PlayerControl player)
    {
        bool lightsout = Utils.IsActive(SystemTypes.Electrical) && player.GetCustomRole().IsImpostor();
        float crewvision = lightsout? Main.DefaultCrewmateVision / 5 : Main.DefaultCrewmateVision;
        if (!player.Is(CustomRoles.KillingMachine) && !player.Is(CustomRoles.Zombie)
           && NarcHasCrewVision.GetBool())
        {
            opt.SetVision(true);
            opt.SetFloat(FloatOptionNames.CrewLightMod, crewvision);
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, crewvision);
        }
    }

    public static bool CheckWinCondition(CustomWinner winner, GameOverReason reason)
        => winner is CustomWinner.Crewmate 
        && (Main.MeetingsPassed >= MeetingsNeededForWin.GetInt() || reason == GameOverReason.HumansByTask);
    public static bool CantUseSabotage(PlayerControl pc) => pc.Is(CustomRoles.Narc) && !NarcCanUseSabotage.GetBool();
/**/
}
