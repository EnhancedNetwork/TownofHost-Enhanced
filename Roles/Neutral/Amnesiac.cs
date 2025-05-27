using AmongUs.GameOptions;
using TOHE.Roles.Core.AssignManager;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Roles.Core.CustomRoleManager;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Amnesiac : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Amnesiac;
    private const int Id = 12700;
    public override CustomRoles ThisRoleBase => AmnesiacCanUseVent.GetBool() ? CustomRoles.Engineer : CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\

    private static OptionItem ImpostorVision;
    private static OptionItem ShowArrows;
    private static OptionItem AmnesiacCanUseVent;
    private static OptionItem VentCoolDown;
    private static OptionItem VentDuration;
    private static OptionItem ReportWhenFailedRemember;

    private static readonly Dictionary<byte, bool> CanUseVent = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Amnesiac);
        ImpostorVision = BooleanOptionItem.Create(Id + 13, "ImpostorVision", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Amnesiac]);
        ShowArrows = BooleanOptionItem.Create(Id + 11, "ShowArrows", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Amnesiac]);
        AmnesiacCanUseVent = BooleanOptionItem.Create(Id + 12, GeneralOption.CanVent, false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Amnesiac]);
        VentCoolDown = FloatOptionItem.Create(Id + 14, GeneralOption.EngineerBase_VentCooldown, new(0f, 60f, 2.5f), 10f, TabGroup.NeutralRoles, false).SetParent(AmnesiacCanUseVent);
        VentDuration = FloatOptionItem.Create(Id + 16, GeneralOption.EngineerBase_InVentMaxTime, new(0f, 180f, 2.5f), 15f, TabGroup.NeutralRoles, false).SetParent(AmnesiacCanUseVent);
        ReportWhenFailedRemember = BooleanOptionItem.Create(Id + 15, "ReportWhenFailedRemember", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Amnesiac]).SetHidden(true);
    }
    public override void Init()
    {
        CanUseVent.Clear();
    }
    public override void Add(byte playerId)
    {
        CanUseVent[playerId] = AmnesiacCanUseVent.GetBool();

        if (ShowArrows.GetBool())
        {
            CheckDeadBodyOthers.Add(CheckDeadBody);
        }
    }
    public override void Remove(byte playerId)
    {
        CheckDeadBodyOthers.Remove(CheckDeadBody);

        if (ShowArrows.GetBool())
            LocateArrow.RemoveAllTarget(playerId);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        opt.SetVision(ImpostorVision.GetBool());
        AURoleOptions.EngineerCooldown = VentCoolDown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = VentDuration.GetFloat();
    }
    public static bool PreviousAmnesiacCanVent(PlayerControl pc) => CanUseVent.TryGetValue(pc.PlayerId, out var canUse) && canUse;
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.ReportButton.OverrideText(GetString("RememberButtonText"));
    }
    public override Sprite ReportButtonSprite => CustomButton.Get("Amnesiac");

    private void CheckDeadBody(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        if (inMeeting || Main.MeetingIsStarted) return;
        if (target == null || target.Data.GetDeadBody() == null) return;
        foreach (var playerId in _playerIdList.ToArray())
        {
            var player = playerId.GetPlayer();
            if (player == null || !player.IsAlive()) continue;

            LocateArrow.Add(playerId, target.Data.GetDeadBody().transform.position);
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
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (ShowArrows.GetBool())
            foreach (var apc in _playerIdList.ToArray())
            {
                LocateArrow.RemoveAllTarget(apc);
            }
    }
    public override bool OnCheckReportDeadBody(PlayerControl __instance, NetworkedPlayerInfo deadBody, PlayerControl killer)
    {
        if (__instance.PlayerId != _Player.PlayerId) return true;

        bool isSuccess = false;
        if (Main.PlayerStates.TryGetValue(deadBody.PlayerId, out var targetPlayerStates))
        {
            if (targetPlayerStates.MainRole == CustomRoles.Amnesiac)
            {
                __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedAmnesiac")));
            }

            if (targetPlayerStates.MainRole.IsGhostRole())
            {
                if (GhostRoleAssign.GhostGetPreviousRole.TryGetValue(targetPlayerStates.PlayerId, out var role) && !role.IsGhostRole())
                {
                    __instance.GetRoleClass()?.OnRemove(__instance.PlayerId);
                    __instance.RpcChangeRoleBasis(role);
                    __instance.RpcSetCustomRole(role);
                    __instance.GetRoleClass()?.OnAdd(__instance.PlayerId);
                    if (targetPlayerStates.SubRoles.Contains(CustomRoles.Narc)) __instance.RpcSetCustomRole(CustomRoles.Narc);

                    __instance.RpcGuardAndKill();
                    __instance.ResetKillCooldown();
                    __instance.SetKillCooldown();

                    role.GetActualRoleName(out var rolename);
                    __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), string.Format(GetString("AmnesiacRemembered"), rolename)));
                    isSuccess = true;
                }
                else
                {
                    __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedAmnesiac")));
                }
            }
            else
            {
                var role = targetPlayerStates.MainRole;
                __instance.GetRoleClass()?.OnRemove(__instance.PlayerId);
                __instance.RpcChangeRoleBasis(role);
                __instance.RpcSetCustomRole(role);
                __instance.GetRoleClass()?.OnAdd(__instance.PlayerId);
                if (targetPlayerStates.SubRoles.Contains(CustomRoles.Narc)) __instance.RpcSetCustomRole(CustomRoles.Narc);

                __instance.RpcGuardAndKill();
                __instance.ResetKillCooldown();
                __instance.SetKillCooldown();

                role.GetActualRoleName(out var rolename);
                __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), string.Format(GetString("AmnesiacRemembered"), rolename)));
                isSuccess = true;
            }
        }
        else
        {
            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedAmnesiac")));
        }

        if (!isSuccess)
        {
            return ReportWhenFailedRemember.GetBool();
        }
        else
        {
            return false;
        }
    }
}
