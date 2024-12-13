using AmongUs.GameOptions;
using System;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

internal class Tracefinder : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Tracefinder;
    private const int Id = 7300;
    public override CustomRoles ThisRoleBase => CustomRoles.Scientist;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateBasic;
    //==================================================================\\

    private static OptionItem VitalsDuration;
    private static OptionItem VitalsCooldown;
    private static OptionItem ArrowDelayMin;
    private static OptionItem ArrowDelayMax;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Tracefinder);
        VitalsCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.ScientistBase_BatteryCooldown, new(1f, 60f, 1f), 5f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Tracefinder])
            .SetValueFormat(OptionFormat.Seconds);
        VitalsDuration = FloatOptionItem.Create(Id + 11, GeneralOption.ScientistBase_BatteryDuration, new(1f, 30f, 1f), 25f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Tracefinder])
            .SetValueFormat(OptionFormat.Seconds);
        ArrowDelayMin = FloatOptionItem.Create(Id + 12, "ArrowDelayMin", new(0f, 30f, 1f), 2f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Tracefinder])
            .SetValueFormat(OptionFormat.Seconds);
        ArrowDelayMax = FloatOptionItem.Create(Id + 13, "ArrowDelayMax", new(0f, 30f, 1f), 7f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Tracefinder])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add(byte playerId)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            CustomRoleManager.CheckDeadBodyOthers.Add(CheckDeadBody);
        }
    }
    public override void Remove(byte playerId)
    {
        CustomRoleManager.CheckDeadBodyOthers.Remove(CheckDeadBody);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerid)
    {
        AURoleOptions.ScientistCooldown = VitalsCooldown.GetFloat();
        AURoleOptions.ScientistBatteryCharge = VitalsDuration.GetFloat();
    }

    public override void OnReportDeadBody(PlayerControl GODZILLA_VS, NetworkedPlayerInfo KINGKONG)
    {
        if (_Player)
            LocateArrow.RemoveAllTarget(_Player.PlayerId);
    }

    public void CheckDeadBody(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        if (inMeeting || target.IsDisconnected()) return;

        var pos = target.GetCustomPosition();

        float delay;
        if (ArrowDelayMax.GetFloat() < ArrowDelayMin.GetFloat()) delay = 0f;
        else delay = IRandom.Instance.Next((int)ArrowDelayMin.GetFloat(), (int)ArrowDelayMax.GetFloat() + 1);
        delay = Math.Max(delay, 0.15f);

        var tempPositionTarget = target.transform.position;

        _ = new LateTask(() =>
        {
            if (!GameStates.IsMeeting && GameStates.IsInTask)
            {
                var player = _Player;
                if (player == null || !player.IsAlive()) return;
                LocateArrow.Add(player.PlayerId, tempPositionTarget);
            }
        }, delay, "Get Arrow Tracefinder");
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (isForMeeting || seer.PlayerId != target.PlayerId) return string.Empty;
        return Utils.ColorString(Color.white, LocateArrow.GetArrows(seer));
    }
}
