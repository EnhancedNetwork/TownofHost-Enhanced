using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TOHE.Modules;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate
{
    internal class Transporter : RoleBase
    {
        //===========================SETUP================================\\
        private const int Id = 7400;
        private static bool On = false;
        public override bool IsEnable => On;
        public static bool HasEnabled => CustomRoles.Transporter.IsClassEnable();
        public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
        //==================================================================\\


        public static OptionItem TransporterTeleportMax;
        public static OverrideTasksData TransporterTasks;
        public static void SetupCustomOptions()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Transporter);
            TransporterTeleportMax = IntegerOptionItem.Create(7402, "TransporterTeleportMax", new(1, 100, 1), 5, TabGroup.CrewmateRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Transporter])
                .SetValueFormat(OptionFormat.Times);
            TransporterTasks = OverrideTasksData.Create(Id + 10, TabGroup.CrewmateRoles, CustomRoles.Transporter);
        }
        public override void Init()
        {
            On = false;
        }
        public override void Add(byte playerId)
        {
            On = true;
        }
        public override void OnTaskComplete(PlayerControl pc, int CompletedTasksCount, int totalTaskCount)
        {
            if ((CompletedTasksCount + 1) <= Transporter.TransporterTeleportMax.GetInt())
            {
                Logger.Info($"Transporter: {pc.GetNameWithRole().RemoveHtmlTags()} completed the task", "Transporter");

                var rd = IRandom.Instance;
                List<PlayerControl> AllAlivePlayer = Main.AllAlivePlayerControls.Where(x => x.CanBeTeleported()).ToList();

                if (AllAlivePlayer.Count >= 2)
                {
                    var target1 = AllAlivePlayer[rd.Next(0, AllAlivePlayer.Count)];
                    var positionTarget1 = target1.GetCustomPosition();

                    AllAlivePlayer.Remove(target1);

                    var target2 = AllAlivePlayer[rd.Next(0, AllAlivePlayer.Count)];
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
                    pc.Notify(ColorString(GetRoleColor(CustomRoles.Impostor), Translator.GetString("ErrorTeleport")));
                }
            }
        }
    }
}
