using Hazel;
using UnityEngine;
using static TOHE.Translator;
using static TOHE.Options;
using static TOHE.Roles.Core.CustomRoleManager;
using AmongUs.GameOptions;

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

    private static readonly Dictionary<byte, bool> CanUseVent = [];
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
        CanUseVent.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        CanUseVent[playerId] = true;

        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);

        if (!AmongUsClient.Instance.AmHost) return;
        if (ShowArrows.GetBool())
        {
            CheckDeadBodyOthers.Add(CheckDeadBody);
        }
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);

        if (!AmongUsClient.Instance.AmHost) return;
        if (ShowArrows.GetBool())
        {
            CheckDeadBodyOthers.Remove(CheckDeadBody);
        }
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        var player = Utils.GetPlayerById(playerId);
        if (player == null) return;

        if (player.Is(Custom_Team.Crewmate))
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, opt.GetFloat(FloatOptionNames.CrewLightMod));
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, opt.GetFloat(FloatOptionNames.CrewLightMod));
        }
        else
        {
            opt.SetVision(true);
            opt.SetFloat(FloatOptionNames.CrewLightMod, opt.GetFloat(FloatOptionNames.ImpostorLightMod));
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, opt.GetFloat(FloatOptionNames.ImpostorLightMod));
        }
    }
    public override bool CanUseImpostorVentButton(PlayerControl pc) => true;
    public static bool PreviousAmnesiacCanVent(PlayerControl pc) => CanUseVent.TryGetValue(pc.PlayerId, out var canUse) && canUse;
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

    public override string GetSuffix(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (isForMeeting || seer.PlayerId != target.PlayerId) return string.Empty;

        if (ShowArrows.GetBool())
        {
            return Utils.ColorString(Color.white, LocateArrow.GetArrows(seer));
        }
        else return string.Empty;
    }

    public override bool OnCheckReportDeadBody(PlayerControl __instance, NetworkedPlayerInfo deadBody, PlayerControl killer)
    {
        var tar = deadBody.Object;
        foreach (var apc in playerIdList.ToArray())
        {
            LocateArrow.RemoveAllTarget(apc);
            SendRPC(apc, false);
        }
        if (__instance.Is(CustomRoles.Amnesiac))
        {
            var tempRole = CustomRoles.Amnesiac;
            if (tar.GetCustomRole().IsImpostor() || tar.GetCustomRole().IsMadmate() || tar.Is(CustomRoles.Madmate))
            {
                tempRole = CustomRoles.Refugee;
            }
            if (tar.GetCustomRole().IsCrewmate() && !tar.Is(CustomRoles.Madmate))
            {
                if (tar.IsAmneCrew())
                {
                    tempRole = tar.GetCustomRole();
                }
                else
                {
                    tempRole = CustomRoles.EngineerTOHE;
                }
                Main.TasklessCrewmate.Add(__instance.PlayerId);
            }
            if (tar.GetCustomRole().IsAmneNK())
            {
                tempRole = tar.GetCustomRole();
            }
            if (tar.GetCustomRole().IsAmneMaverick())
            {
                switch (IncompatibleNeutralMode.GetValue())
                {
                    case 0: // Amnesiac
                        tempRole = CustomRoles.Amnesiac;
                        break;
                    case 1: // Pursuer
                        tempRole = CustomRoles.Pursuer;
                        break;
                    case 2: // Follower
                        tempRole = CustomRoles.Follower;
                        break;
                    case 3: // Maverick
                        tempRole = CustomRoles.Maverick;
                        break;
                    case 4: // Imitator..........................................................................kill me
                        tempRole = CustomRoles.Imitator;
                        break;
                }
            }
            if (tempRole != CustomRoles.Amnesiac)
            {
                __instance.GetRoleClass().OnRemove(__instance.PlayerId);
                __instance.RpcSetCustomRole(tempRole);
                __instance.GetRoleClass().OnAdd(__instance.PlayerId);
                __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));

                __instance.SyncSettings();

                var roleClass = tar.GetRoleClass();
                CanUseVent[__instance.PlayerId] = (roleClass?.ThisRoleBase) switch
                {
                    CustomRoles.Engineer => true,
                    CustomRoles.Impostor or CustomRoles.Shapeshifter or CustomRoles.Phantom => roleClass.CanUseImpostorVentButton(tar),
                    _ => false,
                };
                Logger.Info($"player id: {__instance.PlayerId}, Can use vent: {CanUseVent[__instance.PlayerId]}", "Previous Amne Vent");
            }
            return false;
        }
        return true;
    }
}
