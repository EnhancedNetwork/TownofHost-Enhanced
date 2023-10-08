using System.Collections.Generic;
using System.Linq;
using TOHE.Modules;
using TOHE.Roles.Neutral;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor
{
    public static class Twister
    {
        private static readonly int Id = 4400;

        private static OptionItem ShapeshiftCooldown;
        private static OptionItem ShapeshiftDuration;
        private static OptionItem HideTwistedPlayerNames;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Twister);
            ShapeshiftCooldown = FloatOptionItem.Create(Id + 10, "TwisterCooldown", new(1f, 180f, 1f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Twister])
                .SetValueFormat(OptionFormat.Seconds);
            ShapeshiftDuration = FloatOptionItem.Create(Id + 11, "ShapeshiftDuration", new(1f, 999f, 1f), 10f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Twister])
                    .SetValueFormat(OptionFormat.Seconds);
            HideTwistedPlayerNames = BooleanOptionItem.Create(Id + 12, "TwisterHideTwistedPlayerNames", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Twister]);
        }
        public static void ApplyGameOptions()
        {
            AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
            AURoleOptions.ShapeshifterDuration = ShapeshiftDuration.GetFloat();
        }
        public static void TwistPlayers(PlayerControl shapeshifter)
        {
            List<byte> changePositionPlayers = new List<byte> { shapeshifter.PlayerId };

            var rd = IRandom.Instance;
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (changePositionPlayers.Contains(pc.PlayerId) || Pelican.IsEaten(pc.PlayerId) || !pc.IsAlive() || pc.onLadder || pc.inVent || GameStates.IsMeeting)
                {
                    continue;
                }

                var filtered = Main.AllAlivePlayerControls.Where(a =>
                    pc.IsAlive() && !Pelican.IsEaten(pc.PlayerId) && a.PlayerId != pc.PlayerId && !changePositionPlayers.Contains(a.PlayerId)).ToList();
                
                if (filtered.Count == 0) break;

                PlayerControl target = filtered[rd.Next(0, filtered.Count)];
                changePositionPlayers.Add(target.PlayerId);
                changePositionPlayers.Add(pc.PlayerId);

                pc.RPCPlayCustomSound("Teleport");

                var originPs = target.transform.position;
                target.RpcTeleport(pc.transform.position);
                pc.RpcTeleport(originPs);

                if (!HideTwistedPlayerNames.GetBool())
                {
                    target.Notify(ColorString(GetRoleColor(CustomRoles.Twister), string.Format(GetString("TeleportedByTransporter"), pc.GetRealName())));
                    pc.Notify(ColorString(GetRoleColor(CustomRoles.Twister), string.Format(GetString("TeleportedByTransporter"), target.GetRealName())));
                }
            }
        }
    }
}