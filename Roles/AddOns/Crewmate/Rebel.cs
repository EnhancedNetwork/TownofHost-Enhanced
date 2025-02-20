using AmongUs.GameOptions;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Utils;

namespace TOHE.Roles.AddOns.Crewmate;

public class Rebel : IAddon
{
    public CustomRoles Role => CustomRoles.Rebel;
    private const int Id = 31700;
    public AddonTypes Type => AddonTypes.Misc;

    public static OptionItem SheriffCanBeRebel;
    public static OptionItem MarshallCanBeRebel;
    public static OptionItem OverseerCanBeRebel;
    public static OptionItem DictatorCanBeRebel;
    public static OptionItem RetributionistCanBeRebel;
    public static OptionItem SwapperCanBeRebel;
    public static OptionItem CleanserCanBeRebel;
    public static OptionItem ReverieCanBeRebel;
    public static OptionItem CanWinAfterDeath;
    public static OptionItem HasImpostorVision;

    public void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.Addons, CustomRoles.Rebel, 1, zeroOne: false);
        SheriffCanBeRebel = BooleanOptionItem.Create(Id + 10, "SheriffCanBeRebel", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rebel]);
        MarshallCanBeRebel = BooleanOptionItem.Create(Id + 11, "MarshallCanBeRebel", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rebel]);
        OverseerCanBeRebel = BooleanOptionItem.Create(Id + 12, "OverseerCanBeRebel", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rebel]);
        DictatorCanBeRebel = BooleanOptionItem.Create(Id + 13, "DictatorCanBeRebel", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rebel]);
        RetributionistCanBeRebel = BooleanOptionItem.Create(Id + 14, "RetributionistCanBeRebel", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rebel]);
        SwapperCanBeRebel = BooleanOptionItem.Create(Id + 15, "SwapperCanBeRebel", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rebel]);
        CleanserCanBeRebel = BooleanOptionItem.Create(Id + 16, "CleanserCanBeRebel", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rebel]);
        ReverieCanBeRebel = BooleanOptionItem.Create(Id + 17, "ReverieCanBeRebel", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rebel]);
        CanWinAfterDeath = BooleanOptionItem.Create(Id + 18, "CanWinAfterDeath", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rebel]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 19, "ImpostorVision", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rebel]);
    }

    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }

    ///----------------------------------------Check Rebel Assign----------------------------------------///
    public static bool CheckRebelAssign()
        => CustomRoles.Rebel.IsEnable();

    public static void AssignRebelToPlayer(CustomRoles role, PlayerControl rebel)
    {
        Logger.Info($"{rebel.GetRealName()}({rebel.PlayerId}) Role Change: {rebel.GetCustomRole().ToString()} => {role.ToString()} + {CustomRoles.Rebel.ToString()}", "Assign Rebel");
        rebel.GetRoleClass()?.OnRemove(rebel.PlayerId);
        rebel.RpcChangeRoleBasis(role);
        rebel.RpcSetCustomRole(role);
        rebel.GetRoleClass()?.OnAdd(rebel.PlayerId);
        Main.PlayerStates[rebel.PlayerId].SetSubRole(CustomRoles.Rebel);
    }
    ///-------------------------------------------------------------------------------------------------///

    public static void ApplyGameOptions(IGameOptions opt, PlayerControl player)
    {
        bool lightsOut = IsActive(SystemTypes.Electrical) && player.GetCustomRole().IsCrewmate() && !(player.Is(CustomRoles.Torch) && !Torch.TorchAffectedByLights.GetBool());
        float initVision = player.Is(CustomRoles.Bewilder) ? Bewilder.BewilderVision.GetFloat() : (player.Is(CustomRoles.Torch) ? Torch.TorchVision.GetFloat() : Main.DefaultImpostorVision);
        float rebelVision = lightsOut ? initVision * 5 : initVision;
        if (!player.Is(CustomRoles.Lighter) && HasImpostorVision.GetBool())
        {
            opt.SetVision(true);
            opt.SetFloat(FloatOptionNames.CrewLightMod, rebelVision);
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, rebelVision);
        }
    }
    public static bool CheckWinCondition(CustomWinner winner, PlayerControl pc)
        => winner is not CustomWinner.Crewmate && (CanWinAfterDeath.GetBool() || (pc.IsAlive() && !CanWinAfterDeath.GetBool()));
}
