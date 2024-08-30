using AmongUs.GameOptions;
using TOHE.Roles.Core;

namespace TOHE.Roles.Crewmate;

internal class Altruist : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 29800;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Altruist);

    public override CustomRoles ThisRoleBase => CanHaveAccessToVitals.GetBool() ? CustomRoles.Scientist : CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem CanHaveAccessToVitals;
    private static OptionItem BatteryCooldown;
    private static OptionItem BatteryDuration;

    private byte reviverPlayerId = byte.MaxValue;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Altruist);
        CanHaveAccessToVitals = BooleanOptionItem.Create(Id + 10, GeneralOption.CanHaveAccessToVitals, true, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Altruist]);
        BatteryCooldown = IntegerOptionItem.Create(Id + 11, GeneralOption.ScientistBase_BatteryCooldown, new(1, 250, 1), 15, TabGroup.CrewmateRoles, false)
            .SetParent(CanHaveAccessToVitals)
            .SetValueFormat(OptionFormat.Seconds);
        BatteryDuration = IntegerOptionItem.Create(Id + 12, GeneralOption.ScientistBase_BatteryDuration, new(1, 250, 1), 5, TabGroup.CrewmateRoles, false)
            .SetParent(CanHaveAccessToVitals)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Init()
    {
        reviverPlayerId = byte.MaxValue;
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ScientistCooldown = BatteryCooldown.GetInt();
        AURoleOptions.ScientistBatteryCharge = BatteryDuration.GetInt();
    }

    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody, PlayerControl killer)
    {
        if (reporter.Is(CustomRoles.Altruist) && _Player?.PlayerId == reporter.PlayerId && deadBody != null && deadBody.Object != null)
        {
            var deadPlayer = deadBody.Object;
            var deadPlayerId = deadPlayer.PlayerId;
            var deadBodyObject = deadBody.GetDeadBody();
            reviverPlayerId = deadPlayerId;

            deadPlayer.RpcTeleport(deadBodyObject.transform.position);
            deadPlayer.RpcRevive();

            if (deadPlayer.GetCustomRole().IsGhostRole() || deadPlayer.IsAnySubRole(sub => sub.IsGhostRole()))
            {
                deadPlayer.GetRoleClass().Remove(deadPlayerId);
                deadPlayer.RpcSetCustomRole(Utils.GetRoleMap(deadPlayerId).Item2);
                deadPlayer.GetRoleClass().Add(deadPlayerId);
            }

            _Player.SetDeathReason(PlayerState.DeathReason.Sacrificed);
            _Player.Data.IsDead = true;
            _Player.RpcExileV2();
            Main.PlayerStates[_Player.PlayerId].SetDead();

            _ = new LateTask(() =>
            {
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.Is(Custom_Team.Impostor))
                    {
                        TargetArrow.Add(pc.PlayerId, deadPlayerId);
                        pc.KillFlash(playKillSound: false);
                        pc.Notify(Translator.GetString("Altruist_DeadPlayerHasBeenRevived"), time: 2f);
                    }
                }
                Utils.NotifyRoles();
            }, 1f, "Notify Impostor about revive");
            return false;
        }
        
        return true;
    }

    public override string GetSuffixOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (reviverPlayerId == byte.MaxValue || isForMeeting || seer.PlayerId != target.PlayerId || !seer.Is(Custom_Team.Impostor)) return string.Empty;
        return Utils.ColorString(Utils.HexToColor("#9b0202"), TargetArrow.GetArrows(seer));
    }

    //public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    //{
    //    if (reviverPlayerId != byte.MaxValue)
    //    {
    //        foreach (var pc in Main.AllPlayerControls)
    //        {
    //            if (pc.Is(Custom_Team.Impostor))
    //            {
    //                TargetArrow.Remove(pc.PlayerId, reviverPlayerId);
    //                continue;
    //            }
    //        }
    //    }
    //}
}
