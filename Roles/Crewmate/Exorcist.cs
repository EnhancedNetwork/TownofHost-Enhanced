using static TOHE.Options;
using static TOHE.Translator;
using TOHE.Roles.Double;

namespace TOHE.Roles.Crewmate;

internal class Exorcist : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 36300;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override CustomRoles Role => CustomRoles.Exorcist;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    private static OptionItem VaporizeRadius;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Exorcist);
        VaporizeRadius = FloatOptionItem.Create(Id + 10, "VaporizeRadius363", new(0.5f, 1.5f, 0.1f), 1f,
                TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Exorcist])
            .SetValueFormat(OptionFormat.Multiplier);
        OverrideTasksData.Create(Id + 11, TabGroup.CrewmateRoles, CustomRoles.Exorcist);
    }

    public override bool OnTaskComplete(PlayerControl priest, int completedTaskCount, int totalTaskCount)
    {
        _ = new LateTask(() =>
        {
            foreach (var player in Main.AllAlivePlayerControls)
            {
                if (player.IsPlayerCrewmateTeam()) continue;

                if (player.IsTransformedNeutralApocalypse()) continue;
                else if ((player.Is(CustomRoles.NiceMini) || player.Is(CustomRoles.EvilMini)) && Mini.Age < 18)
                    continue;

                if (Utils.GetDistance(priest.transform.position, player.transform.position) <=
                    VaporizeRadius.GetFloat())
                {
                    Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Vaporized;
                    player.RpcExileV2();
                    Main.PlayerStates[player.PlayerId].SetDead();
                    player.Data.IsDead = true;
                    player.SetRealKiller(priest);
                    priest.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Exorcist),
                        GetString("ExorcisedTargets")));
                }
            }
        }, 0.1f, "Exorcist Bug Fix");
        return true;
    }
}
