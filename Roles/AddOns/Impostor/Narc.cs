using AmongUs.GameOptions;
using TOHE.Roles.Core;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Crewmate;
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
    public static OptionItem NarcHasCrewVision;
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

    public static void AssignNarcToPlayer(CustomRoles role, PlayerControl narc)
    {
        Logger.Info($"{narc.GetRealName()}({narc.PlayerId}) Role Change: {narc.GetCustomRole().ToString()} => {role.ToString()} + {CustomRoles.Narc.ToString()}", "Narc:Assign");
        narc.GetRoleClass()?.OnRemove(narc.PlayerId);
        narc.RpcChangeRoleBasis(role);
        narc.RpcSetCustomRole(role);
        narc.GetRoleClass()?.OnAdd(narc.PlayerId); 
        Main.PlayerStates[narc.PlayerId].SetSubRole(CustomRoles.Narc);       
    }
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
        bool lightsout = Utils.IsActive(SystemTypes.Electrical) && player.GetCustomRole().IsImpostor() && !(player.Is(CustomRoles.Torch) && !Torch.TorchAffectedByLights.GetBool());
        float narcVision = player.Is(CustomRoles.Bewilder) ? Bewilder.BewilderVision.GetFloat() : player.Is(CustomRoles.Torch) ? Torch.TorchVision.GetFloat() : Main.DefaultCrewmateVision;
        if (!player.Is(CustomRoles.KillingMachine) && !player.Is(CustomRoles.Zombie)
            && NarcHasCrewVision.GetBool())
        {
            opt.SetVision(true);
            opt.SetFloat(FloatOptionNames.CrewLightMod, narcVision);
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, narcVision);
        }
    }

    public static bool CheckWinCondition(CustomWinner winner, GameOverReason reason)
        => winner is CustomWinner.Crewmate 
        && (Main.MeetingsPassed >= MeetingsNeededForWin.GetInt() || reason == GameOverReason.HumansByTask);
    public static bool CantUseSabotage(PlayerControl pc) => pc.Is(CustomRoles.Narc) && !NarcCanUseSabotage.GetBool();
/**/
}
