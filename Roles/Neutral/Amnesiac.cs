using UnityEngine;
using static TOHE.Translator;
using static TOHE.Options;
using static TOHE.Roles.Core.CustomRoleManager;

namespace TOHE.Roles.Neutral;

internal class Amnesiac : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 12700;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled = playerIdList.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\

    private static OptionItem IncompatibleNeutralMode;

    private enum AmnesiacIncompatibleNeutralModeSelect
    {
        Role_Amnesiac,
        Role_Pursuer,
        Role_Follower,
        Role_Maverick,
        Role_Imitator,
    }
    
    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Amnesiac);
        IncompatibleNeutralMode = StringOptionItem.Create(Id + 12, "IncompatibleNeutralMode", EnumHelper.GetAllNames<AmnesiacIncompatibleNeutralModeSelect>(), 0, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Amnesiac]);
    }
    public override void Init()
    {
        playerIdList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    
    public override bool CanUseImpostorVentButton(PlayerControl pc) => true;
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.ReportButton.OverrideText(GetString("RememberButtonText"));
    }
    public override Sprite ReportButtonSprite => CustomButton.Get("Amnesiac");
    public override bool OnCheckReportDeadBody(PlayerControl __instance, GameData.PlayerInfo deadBody, PlayerControl killer)
    {
        var tar = deadBody.Object;
        if (__instance.Is(CustomRoles.Amnesiac))
        {
            if (tar.GetCustomRole().IsImpostor() || tar.GetCustomRole().IsMadmate() || tar.Is(CustomRoles.Madmate))
            {
                __instance.RpcSetCustomRole(CustomRoles.Refugee);
                __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
            }
            if (tar.GetCustomRole().IsCrewmate() && !tar.Is(CustomRoles.Madmate))
            {
                if (tar.IsAmneCrew())
                {
                    __instance.RpcSetCustomRole(tar.GetCustomRole());
                    __instance.GetRoleClass().Add(__instance.PlayerId);
                    __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                    tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                    Main.TasklessCrewmate.Add(__instance.PlayerId);
                }
                else
                {
                    __instance.RpcSetCustomRole(CustomRoles.EngineerTOHE);
                    __instance.GetRoleClass().Add(__instance.PlayerId);
                    __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                    tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                    Main.TasklessCrewmate.Add(__instance.PlayerId);
                }
            }
            if (tar.GetCustomRole().IsAmneNK())
            {
                __instance.RpcSetCustomRole(tar.GetCustomRole());
                __instance.GetRoleClass().Add(__instance.PlayerId);
                __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
            }
            if (tar.GetCustomRole().IsAmneMaverick())
            {
                switch (IncompatibleNeutralMode.GetValue())
                {
                    case 0: // Amnesiac
                        __instance.RpcSetCustomRole(CustomRoles.Amnesiac);
                        __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                        tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        break;
                    case 1: // Pursuer
                        __instance.RpcSetCustomRole(CustomRoles.Pursuer);
                        __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                        tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        break;
                    case 2: // Follower
                        __instance.RpcSetCustomRole(CustomRoles.Follower);
                        __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                        tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        break;
                    case 3: // Maverick
                        __instance.RpcSetCustomRole(CustomRoles.Maverick);
                        __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                        tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        break;
                    case 4: // Imitator..........................................................................kill me
                        __instance.RpcSetCustomRole(CustomRoles.Imitator);
                        __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                        tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        break;
                }
                if (__instance.GetCustomRole() != CustomRoles.Amnesiac) 
                    __instance.GetRoleClass().Add(__instance.PlayerId);
            }
            return false;
        }
        return true;
    }
}
