using AmongUs.GameOptions;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
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
    public static OptionItem NarcCanUseSabotage;
    public static OptionItem VisionaryCanBeNarc;
    public static OptionItem DoubleAgentCanBeNarc;
    public static OptionItem ZombieAndKMCanBeNarc;

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
        VisionaryCanBeNarc = BooleanOptionItem.Create(Id + 14, "VisionaryCanBeNarc", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Narc]);
        DoubleAgentCanBeNarc = BooleanOptionItem.Create(Id + 15, "DoubleAgentCanBeNarc", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Narc]);
        ZombieAndKMCanBeNarc = BooleanOptionItem.Create(Id + 16, "ZombieAndKMCanBeNarc", false, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Narc]);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }

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

    private static bool IsImpostorAligned(PlayerControl pc)
        => (CustomRolesHelper.IsNarcImpV3(pc) && !pc.Is(CustomRoles.Admired))
        || pc.Is(CustomRoles.Madmate);

    public static bool CantUseSabotage(PlayerControl pc) => pc.Is(CustomRoles.Narc) && !NarcCanUseSabotage.GetBool();
/*I originally planned to make it so Overseer sees Narc Impostor as Sheriff.
But in the testing stage,it didn't work for modded Overseer.
So I made Overseer able to see the Narc Add-On.
When Overseer sees an Impostor having Narc Add-On,they should know that Narc is crew-aligned.
And that made it pointless that Overseer gets random crewmate role for revealing Narc Trickster.
So I made it that Overseer won't get random Crewmate role result for Narc Trickster.*/
}
