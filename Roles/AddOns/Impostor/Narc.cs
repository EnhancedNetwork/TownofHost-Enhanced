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

    public static OptionItem MeetingsNeededForWin;
    public static OptionItem NarcCanSeeTeammates;
    public static OptionItem NarcCanKillMadmate;
    private static OptionItem NarcCanUseSabotage;
    private static OptionItem NarcHasCrewVision;
    public static OptionItem VisionaryCanBeNarc;
    public static OptionItem DoubleAgentCanBeNarc;
    public static OptionItem ZombieAndKMCanBeNarc;
    private static OptionItem MadmateCanBeNarc;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Narc, canSetNum: false, tab: TabGroup.Addons, canSetChance: false);
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
        VisionaryCanBeNarc = BooleanOptionItem.Create(Id + 15, "VisionaryCanBeNarc", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Narc]);
        DoubleAgentCanBeNarc = BooleanOptionItem.Create(Id + 16, "DoubleAgentCanBeNarc", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Narc]);
        ZombieAndKMCanBeNarc = BooleanOptionItem.Create(Id + 17, "ZombieAndKMCanBeNarc", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Narc]);
        MadmateCanBeNarc = BooleanOptionItem.Create(Id + 18, "MadmateCanBeNarc", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Narc]);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }

///----------------------------------------Check Narc Assign----------------------------------------///
    private static bool CheckMadmateCanBeNarc()
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
        => CheckMadmateCanBeNarc() ? 1 : 0;
        
    public static int ExtraMadSpotNarc
        => CheckMadmateCanBeNarc() ? 0 : 1;
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
        bool lightsout = Utils.IsActive(SystemTypes.Electrical);
        float crewvision = lightsout? Main.DefaultCrewmateVision / 5 : Main.DefaultCrewmateVision;
        if (!player.Is(CustomRoles.KillingMachine) && !player.Is(CustomRoles.Zombie) && !player.Is(CustomRoles.Crewpostor)
           && NarcHasCrewVision.GetBool())
        {
            opt.SetVision(true);
            opt.SetFloat(FloatOptionNames.CrewLightMod, crewvision);
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, crewvision);
        }
    }

    public static bool CantUseSabotage(PlayerControl pc) => pc.Is(CustomRoles.Narc) && !NarcCanUseSabotage.GetBool();
/**/
}
