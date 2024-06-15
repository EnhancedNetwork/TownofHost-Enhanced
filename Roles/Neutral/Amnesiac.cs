using UnityEngine;
using static TOHE.Translator;
using static TOHE.Options;
using static TOHE.Roles.Core.CustomRoleManager;
using Hazel;

namespace TOHE.Roles.Neutral;

internal class Amnesiac : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 12700;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled = playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\

    private static OptionItem IncompatibleNeutralMode;
    private static OptionItem ShowArrows;

    private enum AmnesiacIncompatibleNeutralModeSelectList
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
        IncompatibleNeutralMode = StringOptionItem.Create(Id + 10, "IncompatibleNeutralMode", EnumHelper.GetAllNames<AmnesiacIncompatibleNeutralModeSelectList>(), 0, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Amnesiac]);
        ShowArrows = BooleanOptionItem.Create(Id + 11, "ShowArrows", false, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Amnesiac]);
    }
    public override void Init()
    {
        playerIdList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);

        if (!AmongUsClient.Instance.AmHost) return;
        if (ShowArrows.GetBool())
        {
            CheckDeadBodyOthers.Add(CheckDeadBody);
        }
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    
    public override bool CanUseImpostorVentButton(PlayerControl pc) => true;
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.ReportButton.OverrideText(GetString("RememberButtonText"));
    }
    public override Sprite ReportButtonSprite => CustomButton.Get("Amnesiac");

    private static void SendRPC(byte playerId, bool add, Vector3 loc = new())
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetAmnesaicArrows, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(add);
        if (add)
        {
            writer.Write(loc.x);
            writer.Write(loc.y);
            writer.Write(loc.z);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    
    public static void ReceiveRPC(MessageReader reader)
    {
        byte playerId = reader.ReadByte();
        bool add = reader.ReadBoolean();
        if (add)
            LocateArrow.Add(playerId, new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
        else
            LocateArrow.RemoveAllTarget(playerId);
    }
    private void CheckDeadBody(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        if (inMeeting) return;
        foreach (var pc in playerIdList.ToArray())
        {
            var player = Utils.GetPlayerById(pc);
            if (player == null || !player.IsAlive()) continue;
            LocateArrow.Add(pc, target.transform.position);
            SendRPC(pc, true, target.transform.position);
        }
    }

    public override string GetSuffix(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false)
    {
        if (isForMeeting) return string.Empty;

        if (ShowArrows.GetBool())
        {
            if (!seer.Is(CustomRoles.Amnesiac)) return "";
            if (target != null && seer.PlayerId != target.PlayerId) return "";
            if (GameStates.IsMeeting) return "";
            return Utils.ColorString(Color.white, LocateArrow.GetArrows(seer));
        }
        else return "";
    }

    public override bool OnCheckReportDeadBody(PlayerControl __instance, GameData.PlayerInfo deadBody, PlayerControl killer)
    {
        var tar = deadBody.Object;
        foreach (var apc in playerIdList.ToArray())
        {
            LocateArrow.RemoveAllTarget(apc);
            SendRPC(apc, false);
        }
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
                    __instance.GetRoleClass().OnAdd(__instance.PlayerId);
                    __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                    tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                    Main.TasklessCrewmate.Add(__instance.PlayerId);
                }
                else
                {
                    __instance.RpcSetCustomRole(CustomRoles.EngineerTOHE);
                    __instance.GetRoleClass().OnAdd(__instance.PlayerId);
                    __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                    tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                    Main.TasklessCrewmate.Add(__instance.PlayerId);
                }
            }
            if (tar.GetCustomRole().IsAmneNK())
            {
                __instance.RpcSetCustomRole(tar.GetCustomRole());
                __instance.GetRoleClass().OnAdd(__instance.PlayerId);
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
                    __instance.GetRoleClass().OnAdd(__instance.PlayerId);
            }         
            return false;
        }
        return true;
    }
}
