using AmongUs.GameOptions;
using MS.Internal.Xml.XPath;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;
internal class Slayer : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Slayer;
    private const int Id = 33400;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\
    private static OptionItem MaulRadius;
    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute) => true;
    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Slayer);
        MaulRadius = FloatOptionItem.Create(Id + 10, "MaulRadius", new(0.5f, 1.5f, 0.1f), 1.3f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Slayer])
            .SetValueFormat(OptionFormat.Multiplier);
        OverrideTasksData.Create(Id + 20, TabGroup.NeutralRoles, CustomRoles.Slayer);
    }
    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        Logger.Info("Slayer Kill", "Mauled");
            _ = new LateTask(() =>
            {
                foreach (var killing in Main.AllAlivePlayerControls)
                {
                    if (killing == player) continue;

                    if (killing.IsTransformedNeutralApocalypse()) continue;
                    else if ((killing.Is(CustomRoles.NiceMini) || killing.Is(CustomRoles.EvilMini)) && Mini.Age < 18) continue;

                    if (Utils.GetDistance(player.transform.position, killing.transform.position) <= MaulRadius.GetFloat())
                    {
                        killing.SetDeathReason(PlayerState.DeathReason.Mauled);
                        player.RpcMurderPlayer(killing);
                        killing.SetRealKiller(player);
                    }
                }
            }, 0.1f, "Slayer Maul Bug Fix");
        return true;
    }
}
