using TOHE.Modules;
using TOHE.Roles.Core;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.AddOns.Common;

public class Oiiai : IAddon
{
    public CustomRoles Role => CustomRoles.Oiiai;
    private const int Id = 25700;
    private readonly static List<byte> playerIdList = [];
    public static bool IsEnable = false;
    public AddonTypes Type => AddonTypes.Mixed;


    private static OptionItem CanPassOn;
    private static OptionItem ChangeNeutralRole;

    [Obfuscation(Exclude = true)]
    private enum ChangeRolesSelectList
    {
        Role_NoChange,
        Role_Amnesiac,
        Role_Imitator
    }

    public static readonly CustomRoles[] NRoleChangeRoles =
    [
        CustomRoles.Amnesiac,
        CustomRoles.Imitator,
    ]; //Just -1 to use this LOL

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Oiiai, canSetNum: true, tab: TabGroup.Addons, teamSpawnOptions: true);
        CanPassOn = BooleanOptionItem.Create(Id + 14, "OiiaiCanPassOn", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Oiiai]);
        ChangeNeutralRole = StringOptionItem.Create(Id + 15, "NeutralChangeRolesForOiiai", EnumHelper.GetAllNames<ChangeRolesSelectList>(), 1, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Oiiai]);
    }
    public void Init()
    {
        IsEnable = false;
        playerIdList.Clear();
    }
    public void Add(byte playerId, bool gameIsLoading = true)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);

        IsEnable = true;
    }
    public static void PassOnKiller(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);
        IsEnable = true;
    }
    public void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);

        if (!playerIdList.Any())
            IsEnable = false;
    }

    public static void OnMurderPlayer(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return;
        if (killer.PlayerId == target.PlayerId) return;
        if (killer.Is(CustomRoles.KillingMachine) || killer.IsTransformedNeutralApocalypse()) return;
        if ((killer.Is(CustomRoles.Ghoul) || killer.Is(CustomRoles.Burst)) && !killer.IsAlive()) return;
        if (!target.Is(CustomRoles.Oiiai)) return;
        if (!CanGetOiiaied(killer)) return;

        if (CanPassOn.GetBool() && !playerIdList.Contains(killer.PlayerId))
        {
            PassOnKiller(killer.PlayerId);
            killer.RpcSetCustomRole(CustomRoles.Oiiai);
            Logger.Info(killer.GetNameWithRole() + " gets Oiiai addon by " + target.GetNameWithRole(), "Oiiai");
        }

        if (!Eraser.ErasedRoleStorage.ContainsKey(killer.PlayerId))
        {
            Eraser.ErasedRoleStorage.Add(killer.PlayerId, killer.GetCustomRole());
            Logger.Info($"Added {killer.GetNameWithRole()} to ErasedRoleStorage", "Oiiai");
        }
        else
        {
            Logger.Info($"Canceled {killer.GetNameWithRole()} Oiiai bcz already erased.", "Oiiai");
            return;
        }

        var killerRole = killer.GetCustomRole();
        if (killer.HasGhostRole() || CopyCat.playerIdList.Contains(killer.PlayerId) || killer.Is(CustomRoles.Stubborn))
        {
            Logger.Info($"Oiiai {killer.GetNameWithRole().RemoveHtmlTags()} cannot eraser crew imp-based role", "Oiiai");
            return;
        }
        else if (killerRole.IsCoven() && !CovenManager.HasNecronomicon(killer))
        {
            killer.GetRoleClass().OnRemove(killer.PlayerId);
            killer.RpcChangeRoleBasis(CustomRoles.Amnesiac);
            killer.RpcSetCustomRole(CustomRoles.Amnesiac);
            Main.DesyncPlayerList.Remove(killer.PlayerId);
            killer.GetRoleClass().OnAdd(killer.PlayerId);
            killer.RpcSetCustomRole(CustomRoles.Enchanted);
            Logger.Info($"Oiiai {killer.GetNameWithRole().RemoveHtmlTags()} with Coven without Necronomicon.", "Oiiai");
        }
        else if (CovenManager.HasNecronomicon(killer))
        {
            // Necronomicon holder immune to Oiiai
            Logger.Info($"Oiiai {killer.GetNameWithRole().RemoveHtmlTags()} with Coven with Necronomicon.", "Oiiai");
        }
        else if (killerRole.IsMadmate())
        {
            killer.GetRoleClass().OnRemove(killer.PlayerId);
            killer.RpcChangeRoleBasis(CustomRoles.Amnesiac);
            killer.RpcSetCustomRole(CustomRoles.Amnesiac);
            Main.DesyncPlayerList.Remove(killer.PlayerId);
            killer.GetRoleClass().OnAdd(killer.PlayerId);
            killer.RpcSetCustomRole(CustomRoles.Madmate);
            Logger.Info($"Oiiai {killer.GetNameWithRole().RemoveHtmlTags()} with Madmates assign.", "Oiiai");
        }
        else if (killer.Is(CustomRoles.Sidekick))
        {
            killer.GetRoleClass().OnRemove(killer.PlayerId);
            killer.RpcChangeRoleBasis(CustomRoles.Amnesiac);
            killer.RpcSetCustomRole(CustomRoles.Amnesiac);
            Main.DesyncPlayerList.Remove(killer.PlayerId);
            killer.GetRoleClass().OnAdd(killer.PlayerId);
            killer.RpcSetCustomRole(CustomRoles.Recruit);
            Logger.Info($"Oiiai {killer.GetNameWithRole().RemoveHtmlTags()} with Sidekicks assign.", "Oiiai");
        }
        else if (!killerRole.IsNeutral())
        {
            var readyrole = Eraser.GetErasedRole(killer.GetCustomRole().GetRoleTypes(), killer.GetCustomRole());
            //Use eraser here LOL
            killer.GetRoleClass()?.OnRemove(killer.PlayerId);
            killer.RpcChangeRoleBasis(readyrole);
            killer.RpcSetCustomRole(readyrole);
            Main.DesyncPlayerList.Remove(killer.PlayerId);
            killer.GetRoleClass()?.OnAdd(killer.PlayerId);
            Logger.Info($"Oiiai {killer.GetNameWithRole().RemoveHtmlTags()} with eraser assign.", "Oiiai");
        }
        else
        {
            int changeValue = ChangeNeutralRole.GetValue();

            if (changeValue != 0)
            {
                killer.GetRoleClass().OnRemove(killer.PlayerId);
                killer.RpcChangeRoleBasis(NRoleChangeRoles[changeValue - 1]);
                killer.RpcSetCustomRole(NRoleChangeRoles[changeValue - 1]);
                Main.DesyncPlayerList.Remove(killer.PlayerId);
                killer.GetRoleClass().OnAdd(killer.PlayerId);

                killer.SyncSettings();

                Logger.Info($"Oiiai {killer.GetNameWithRole().RemoveHtmlTags()} with Neutrals assign.", "Oiiai");
            }
        }
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        killer.Notify(GetString("LostRoleByOiiai"));
        killer.RPCPlayCustomSound("Oiiai");
        Logger.Info($"{killer.GetRealName()} was OIIAIed", "Oiiai");
    }

    private static bool CanGetOiiaied(PlayerControl player)
    {
        if (player.GetCustomRole().IsNeutral() && ChangeNeutralRole.GetValue() == 0) return false;
        if (player.Is(CustomRoles.Loyal) || player.Is(CustomRoles.Stubborn)) return false;

        return true;
    }
}
