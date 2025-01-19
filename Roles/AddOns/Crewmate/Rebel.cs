using AmongUs.GameOptions;
using static TOHE.Options;
using static TOHE.Utils;

namespace TOHE.Roles.AddOns.Crewmate;

public class Rebel : IAddon
{
    public CustomRoles Role => CustomRoles.Rebel;
    private const int Id = 31600;
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
    private static OptionItem HasImpostorVision;

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
    {
        int optnnknum = NonNeutralKillingRolesMinPlayer.GetInt() + NonNeutralKillingRolesMaxPlayer.GetInt();
        int assignvalue = IRandom.Instance.Next(1, optnnknum);

        if (optnnknum == 0) return false;
        else if (assignvalue > optnnknum) return false;

        return true;
    }

    public static int ExtraNNKSpotRebel
        => CheckRebelAssign() ? 0 : 1;

    public static bool RemoveTheseRoles(CustomRoles role) => role is
        CustomRoles.Ghoul or
        CustomRoles.Bloodthirst or
        CustomRoles.Torch or
        CustomRoles.Madmate or
        CustomRoles.Egoist or
        CustomRoles.Rascal or
        CustomRoles.Paranoia or
        CustomRoles.Loyal or
        CustomRoles.Hurried or
        CustomRoles.Youtuber;
    ///-------------------------------------------------------------------------------------------------///

    public static void ApplyGameOptions(IGameOptions opt, PlayerControl player)
    {
        bool lightsOut = IsActive(SystemTypes.Electrical);
        float impVision = lightsOut ? Main.DefaultImpostorVision * 5 : Main.DefaultImpostorVision;
        if (!player.Is(CustomRoles.Lighter) && HasImpostorVision.GetBool())
        {
            opt.SetVision(true);
            opt.SetFloat(FloatOptionNames.CrewLightMod, impVision);
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, impVision);
        }
    }
}
