using AmongUs.GameOptions;
using TOHE.Roles.Core;

namespace TOHE.Roles.Crewmate;

internal class Altruist : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 29800;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Altruist);
    public override bool IsExperimental => true;
    public override CustomRoles ThisRoleBase => CanHaveAccessToVitals.GetBool() ? CustomRoles.Scientist : CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem RevivedDeadBodyCannotBeReported;
    private static OptionItem CanHaveAccessToVitals;
    private static OptionItem BatteryCooldown;
    private static OptionItem BatteryDuration;

    private byte RevivedPlayerId = byte.MaxValue;
    private readonly static HashSet<byte> AllRevivedPlayerId = [];

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Altruist);
        RevivedDeadBodyCannotBeReported = BooleanOptionItem.Create(Id + 10, "Altruist_RevivedDeadBodyCannotBeReported_Option", true, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Altruist]);
        CanHaveAccessToVitals = BooleanOptionItem.Create(Id + 11, GeneralOption.CanHaveAccessToVitals, true, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Altruist]);
        BatteryCooldown = IntegerOptionItem.Create(Id + 12, GeneralOption.ScientistBase_BatteryCooldown, new(1, 250, 1), 15, TabGroup.CrewmateRoles, false)
            .SetParent(CanHaveAccessToVitals)
            .SetValueFormat(OptionFormat.Seconds);
        BatteryDuration = IntegerOptionItem.Create(Id + 13, GeneralOption.ScientistBase_BatteryDuration, new(1, 250, 1), 5, TabGroup.CrewmateRoles, false)
            .SetParent(CanHaveAccessToVitals)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Init()
    {
        RevivedPlayerId = byte.MaxValue;
        AllRevivedPlayerId.Clear();
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ScientistCooldown = BatteryCooldown.GetInt();
        AURoleOptions.ScientistBatteryCharge = BatteryDuration.GetInt();
    }

    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody, PlayerControl killer)
    {
        if (deadBody != null && deadBody.Object != null)
        {
            if (reporter.Is(CustomRoles.Altruist) && _Player?.PlayerId == reporter.PlayerId)
            {
                var deadPlayer = deadBody.Object;
                var deadPlayerId = deadPlayer.PlayerId;
                var deadBodyObject = deadBody.GetDeadBody();

                RevivedPlayerId = deadPlayerId;
                AllRevivedPlayerId.Add(deadPlayerId);

                deadPlayer.RpcTeleport(deadBodyObject.transform.position);
                deadPlayer.RpcRevive();

                if (deadPlayer.HasGhostRole())
                {
                    deadPlayer.GetRoleClass().Remove(deadPlayerId);
                    deadPlayer.RpcSetCustomRole(Utils.GetRoleMap(deadPlayerId).customRole);
                    deadPlayer.GetRoleClass().Add(deadPlayerId);
                }

                _Player.SetDeathReason(PlayerState.DeathReason.Sacrificed);
                _Player.Data.IsDead = true;
                _Player.RpcExileV2();
                Main.PlayerStates[_Player.PlayerId].SetDead();

                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.Is(Custom_Team.Impostor) && pc.PlayerId != RevivedPlayerId)
                    {
                        TargetArrow.Add(pc.PlayerId, deadPlayerId);
                        pc.KillFlash(playKillSound: false);
                        pc.Notify(Translator.GetString("Altruist_DeadPlayerHasBeenRevived"));
                    }
                }
                Utils.NotifyRoles();
                return false;
            }
            else if ((RevivedDeadBodyCannotBeReported.GetBool() || reporter.PlayerId == RevivedPlayerId) && deadBody.PlayerId == RevivedPlayerId)
            {
                reporter.Notify(Translator.GetString("Altruist_YouTriedReportRevivedDeadBody"));
                return false;
            }
        }

        return true;
    }

    public override string GetSuffixOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (RevivedPlayerId == byte.MaxValue || isForMeeting || seer.PlayerId != target.PlayerId || !seer.Is(Custom_Team.Impostor)) return string.Empty;
        Logger.Info($"{TargetArrow.GetArrows(seer)}", "Altruist");
        return Utils.ColorString(Utils.HexToColor("9b0202"), TargetArrow.GetArrows(seer, RevivedPlayerId));
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (RevivedPlayerId != byte.MaxValue)
        {
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (pc.Is(Custom_Team.Impostor))
                {
                    TargetArrow.Remove(pc.PlayerId, RevivedPlayerId);
                    continue;
                }
            }
        }
    }
}
