using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Crewmate;

public class Rebel : IAddon
{
    public CustomRoles Role => CustomRoles.Rebel;
    private const int Id = 31600;
    public AddonTypes Type => AddonTypes.Misc;

    public static OptionItem CanWinAfterDeath;
    public static OptionItem SheriffCanBeRebel;
    public static OptionItem MarshallCanBeRebel;
    public static OptionItem OverseerCanBeRebel;
    public static OptionItem DictatorCanBeRebel;
    public static OptionItem RetributionistCanBeRebel;
    public static OptionItem SwapperCanBeRebel;
    public static OptionItem CleanserCanBeRebel;
    public static OptionItem ReverieCanBeRebel;
    private static OptionItem HasImpostorVision;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Rebel, canSetNum: true, tab: TabGroup.Addons);
        CanWinAfterDeath = BooleanOptionItem.Create(Id + 10, "CanWinAfterDeath", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rebel]);
        SheriffCanBeRebel = BooleanOptionItem.Create(Id + 11, "SheriffCanBeRebel", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rebel]);
        MarshallCanBeRebel = BooleanOptionItem.Create(Id + 12, "MarshallCanBeRebel", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rebel]);
        OverseerCanBeRebel = BooleanOptionItem.Create(Id + 13, "OverseerCanBeRebel", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rebel]);
        DictatorCanBeRebel = BooleanOptionItem.Create(Id + 14, "DictatorCanBeRebel", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rebel]);
        RetributionistCanBeRebel = BooleanOptionItem.Create(Id + 15, "RetributionistCanBeRebel", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rebel]);
        SwapperCanBeRebel = BooleanOptionItem.Create(Id + 16, "SwapperCanBeRebel", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rebel]);
        CleanserCanBeRebel = BooleanOptionItem.Create(Id + 17, "CleanserCanBeRebel", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rebel]);
        ReverieCanBeRebel = BooleanOptionItem.Create(Id + 18, "ReverieCanBeRebel", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rebel]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 19, "ImpostorVision", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rebel]);
    }

    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }

    ///----------------------------------------Check Rebel Assign----------------------------------------///
    private static bool CheckCanBeRebel()
    {
        int playerCount = Main.AllAlivePlayerControls.Length;
        int optnnknum = NonNeutralKillingRolesMinPlayer.GetInt() + NonNeutralKillingRolesMaxPlayer.GetInt();
        int assignvalue = IRandom.Instance.Next(1, optnnknum);

        if (optnnknum == 0) return false;
        else if (assignvalue > optnnknum) return false;

        return true;
    }

    public static int ExtraNNKSpotRebel
        => CheckCanBeRebel() ? 0 : 1;
    ///-------------------------------------------------------------------------------------------------///

    public static void ApplyGameOptions(IGameOptions opt, PlayerControl player)
    {
        bool lightsOut = Utils.IsActive(SystemTypes.Electrical);
        float impVision = lightsOut ? Main.DefaultImpostorVision * 5 : Main.DefaultImpostorVision;
        if (!player.Is(CustomRoles.Lighter) && HasImpostorVision.GetBool())
        {
            opt.SetVision(true);
            opt.SetFloat(FloatOptionNames.CrewLightMod, impVision);
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, impVision);
        }
    }
}
