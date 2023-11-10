using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOHE.Roles.Impostor;
internal static class Grappler
{
    public static OptionItem ShapeshiftCooldown, ShapeshiftDuration;
    public static List<byte> playerIdList = new();
    public static readonly int Id = 26100;
    public static bool IsEnable = false;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Grappler);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 2, "GrapplerShapeshiftCooldown", new(0f, 180f, 2.5f), 15f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Grappler])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftDuration = FloatOptionItem.Create(Id + 3, "GrapplerShapeshiftDuration", new(0f, 180f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Grappler])
           .SetValueFormat(OptionFormat.Seconds);
    }

    public static void ApplyGameOptions()
    {
        AURoleOptions.ShapeshifterCooldown = Grappler.ShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = Grappler.ShapeshiftDuration.GetFloat();
    }

    public static void Init()
    {
        playerIdList = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;
    }

    public static void OnShapeshift(PlayerControl pc, PlayerControl target, bool shapeshifting)
    {
        if (shapeshifting)
        {
            target.Notify(Translator.GetString("Grappling"));
            _ = new LateTask(() =>
            {
                if (pc.IsAlive() && target.IsAlive() && !pc.inVent && !target.inVent)
                    target.RpcTeleport(pc.transform.position);
                else
                    pc.Notify(Translator.GetString("GrapplingFailed"));
            }, 1.5f, "Grappler TP");
        }
    }
}
