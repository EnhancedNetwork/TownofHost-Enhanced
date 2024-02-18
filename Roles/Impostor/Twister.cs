using System.Collections.Generic;
using System.Linq;
using TOHE.Modules;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor;

public static class Twister
{
    private static readonly int Id = 5700;

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
    public static void TwistPlayers(PlayerControl shapeshifter, bool shapeshiftIsHidden = false)
    {
        List<byte> changePositionPlayers = [shapeshifter.PlayerId];

        var rd = IRandom.Instance;
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if ((changePositionPlayers.Contains(pc.PlayerId) && !shapeshiftIsHidden) || !pc.CanBeTeleported())
            {
                continue;
            }

            var filtered = Main.AllAlivePlayerControls.Where(a =>
                pc.CanBeTeleported() && a.PlayerId != pc.PlayerId && !changePositionPlayers.Contains(a.PlayerId)).ToList();
            
            if (filtered.Count == 0) break;

            var target = filtered[rd.Next(0, filtered.Count)];
            changePositionPlayers.Add(target.PlayerId);
            changePositionPlayers.Add(pc.PlayerId);

            pc.RPCPlayCustomSound("Teleport");

            var originPs = target.GetCustomPosition();
            target.RpcTeleport(pc.GetCustomPosition());
            pc.RpcTeleport(originPs);

            if (!HideTwistedPlayerNames.GetBool())
            {
                target.Notify(ColorString(GetRoleColor(CustomRoles.Twister), string.Format(GetString("TeleportedByTransporter"), pc.GetRealName())));
                pc.Notify(ColorString(GetRoleColor(CustomRoles.Twister), string.Format(GetString("TeleportedByTransporter"), target.GetRealName())));
            }
        }
    }
}