using TOHE.Modules;
using static TOHE.Options;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Transporter : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Transporter;
    private const int Id = 7400;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateBasic;
    //==================================================================\\

    private static OptionItem TransporterTeleportMax;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Transporter);
        TransporterTeleportMax = IntegerOptionItem.Create(7402, "TransporterTeleportMax", new(1, 100, 1), 5, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Transporter])
            .SetValueFormat(OptionFormat.Times);
        OverrideTasksData.Create(Id + 10, TabGroup.CrewmateRoles, CustomRoles.Transporter);
    }
    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (player.IsAlive() && (completedTaskCount + 1) <= TransporterTeleportMax.GetInt())
        {
            Logger.Info($"Transporter: {player.GetNameWithRole().RemoveHtmlTags()} completed the task", "Transporter");

            var rd = IRandom.Instance;
            List<PlayerControl> AllAlivePlayer = Main.AllAlivePlayerControls.Where(x => x.CanBeTeleported()).ToList();

            if (AllAlivePlayer.Count >= 2)
            {
                var target1 = AllAlivePlayer.RandomElement();
                var positionTarget1 = target1.GetCustomPosition();

                AllAlivePlayer.Remove(target1);

                var target2 = AllAlivePlayer.RandomElement();
                var positionTarget2 = target2.GetCustomPosition();

                target1.RpcTeleport(positionTarget2);
                target2.RpcTeleport(positionTarget1);

                AllAlivePlayer.Clear();

                target1.RPCPlayCustomSound("Teleport");
                target2.RPCPlayCustomSound("Teleport");

                target1.Notify(ColorString(GetRoleColor(CustomRoles.Transporter), string.Format(Translator.GetString("TeleportedByTransporter"), target2.GetRealName())));
                target2.Notify(ColorString(GetRoleColor(CustomRoles.Transporter), string.Format(Translator.GetString("TeleportedByTransporter"), target1.GetRealName())));
            }
            else
            {
                player.Notify(ColorString(GetRoleColor(CustomRoles.Impostor), Translator.GetString("ErrorTeleport")));
            }
        }

        return true;
    }
}
