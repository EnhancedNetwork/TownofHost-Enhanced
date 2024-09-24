﻿using AmongUs.GameOptions;
using TOHE.Roles.Core;

namespace TOHE.Roles.Crewmate;

internal class Altruist : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 29800;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Altruist);
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem RevivedDeadBodyCannotBeReported;
    //private static OptionItem KillerAlwaysCanGetAlertAndArrow;
    private static OptionItem ImpostorsCanGetsAlert;
    private static OptionItem ImpostorsCanGetsArrow;
    private static OptionItem NeutralKillersCanGetsAlert;
    private static OptionItem NeutralKillersCanGetsArrow;

    private bool IsRevivingMode = true;
    private byte RevivedPlayerId = byte.MaxValue;
    //private readonly static HashSet<byte> AllRevivedPlayerId = [];

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Altruist);
        RevivedDeadBodyCannotBeReported = BooleanOptionItem.Create(Id + 10, "Altruist_RevivedDeadBodyCannotBeReported_Option", true, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Altruist]);
        ImpostorsCanGetsAlert = BooleanOptionItem.Create(Id + 11, "Altruist_ImpostorsCanGetsAlert", true, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Altruist]);
        ImpostorsCanGetsArrow = BooleanOptionItem.Create(Id + 12, "Altruist_ImpostorsCanGetsArrow", true, TabGroup.CrewmateRoles, false)
            .SetParent(ImpostorsCanGetsAlert);
        NeutralKillersCanGetsAlert = BooleanOptionItem.Create(Id + 13, "Altruist_NeutralKillersCanGetsAlert", true, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Altruist]);
        NeutralKillersCanGetsArrow = BooleanOptionItem.Create(Id + 14, "Altruist_NeutralKillersCanGetsArrow", true, TabGroup.CrewmateRoles, false)
            .SetParent(NeutralKillersCanGetsAlert);
    }

    public override void Init()
    {
        RevivedPlayerId = byte.MaxValue;
        //AllRevivedPlayerId.Clear();
        IsRevivingMode = true;
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = 1f;
        AURoleOptions.EngineerInVentMaxTime = 1f;
    }
    public override void OnCoEnterVent(PlayerPhysics physics, int ventId)
    {
        IsRevivingMode = !IsRevivingMode;
        Utils.NotifyRoles(SpecifySeer: physics.myPlayer, ForceLoop: false);
    }
    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody, PlayerControl killer)
    {
        if (deadBody == null || deadBody.Object == null) return true;

        if (reporter.Is(CustomRoles.Altruist) && _Player?.PlayerId == reporter.PlayerId)
        {
            if (!IsRevivingMode) return true;

            var deadPlayer = deadBody.Object;
            var deadPlayerId = deadPlayer.PlayerId;
            var deadBodyObject = deadBody.GetDeadBody();

            RevivedPlayerId = deadPlayerId;
            //AllRevivedPlayerId.Add(deadPlayerId);

            deadPlayer.RpcTeleport(deadBodyObject.transform.position);
            deadPlayer.RpcRevive();

            _Player.SetDeathReason(PlayerState.DeathReason.Sacrificed);
            _Player.Data.IsDead = true;
            _Player.RpcExileV2();
            Main.PlayerStates[_Player.PlayerId].SetDead();

            if (ImpostorsCanGetsAlert.GetBool() || NeutralKillersCanGetsAlert.GetBool())
            {
                foreach (var pc in Main.AllAlivePlayerControls)
                {
                    if (pc.GetCustomRole().IsCrewmate()) continue;

                    var getAlert = false;
                    var getArrow = false;

                    if (ImpostorsCanGetsAlert.GetBool() && pc.Is(Custom_Team.Impostor) && pc.PlayerId != RevivedPlayerId)
                    {
                        getAlert = true;

                        if (ImpostorsCanGetsArrow.GetBool())
                            getArrow = true;
                    }
                    else if (NeutralKillersCanGetsAlert.GetBool() && (pc.IsNeutralKiller() || pc.IsNeutralApocalypse()) && pc.PlayerId != RevivedPlayerId)
                    {
                        getAlert = true;

                        if (NeutralKillersCanGetsArrow.GetBool())
                            getArrow = true;
                    }
                    
                    if (getAlert)
                    {
                        pc.KillFlash(playKillSound: false);
                        pc.Notify(Translator.GetString("Altruist_DeadPlayerHasBeenRevived"));
                    }
                    if (getArrow)
                        TargetArrow.Add(pc.PlayerId, deadPlayerId);
                }
            }
            Utils.NotifyRoles();
            return false;
        }
        else if ((RevivedDeadBodyCannotBeReported.GetBool() || reporter.PlayerId == RevivedPlayerId) && deadBody.PlayerId == RevivedPlayerId)
        {
            var countDeadBody = UnityEngine.Object.FindObjectsOfType<DeadBody>().Count(bead => bead.ParentId == deadBody.PlayerId);
            if (countDeadBody >= 2) return true;

            reporter.Notify(Translator.GetString("Altruist_YouTriedReportRevivedDeadBody"));
            return false;
        }

        return true;
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl target, bool isForMeeting = false, bool isForHud = false)
    {
        if (seer.PlayerId != target.PlayerId || isForMeeting) return string.Empty;
        return string.Format(Translator.GetString("AltruistSuffix"), Translator.GetString(IsRevivingMode ? "AltruistReviveMode" : "AltruistReportMode"));
    }
    public override string GetSuffixOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (RevivedPlayerId == byte.MaxValue || isForMeeting || seer.PlayerId != target.PlayerId) return string.Empty;
        if (seer.Is(Custom_Team.Impostor) || seer.IsNeutralKiller() || seer.IsNeutralApocalypse())
        {
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Altruist), TargetArrow.GetArrows(seer));
        }
        return string.Empty;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (!(ImpostorsCanGetsArrow.GetBool() || NeutralKillersCanGetsArrow.GetBool()) || RevivedPlayerId == byte.MaxValue) return;

        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (ImpostorsCanGetsArrow.GetBool() && pc.Is(Custom_Team.Impostor))
            {
                TargetArrow.Remove(pc.PlayerId, RevivedPlayerId);
                continue;
            }
            if (NeutralKillersCanGetsArrow.GetBool() && (pc.IsNeutralKiller() || pc.IsNeutralApocalypse()))
            {
                TargetArrow.Remove(pc.PlayerId, RevivedPlayerId);
                continue;
            }
        }
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud?.AbilityButton?.OverrideText(Translator.GetString("AltruistAbilityButton"));

        if (IsRevivingMode)
            hud?.ReportButton?.OverrideText(Translator.GetString("AltruistReviveMode"));
    }
}
