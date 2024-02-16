using TOHE.Modules;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor;

public static class Disperser
{
    private static readonly int Id = 24400;

    private static OptionItem DisperserShapeshiftCooldown;
    private static OptionItem DisperserShapeshiftDuration;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.Disperser);
        DisperserShapeshiftCooldown = FloatOptionItem.Create(Id + 5, "ShapeshiftCooldown", new(1f, 180f, 1f), 20f, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Disperser])
            .SetValueFormat(OptionFormat.Seconds);
        DisperserShapeshiftDuration = FloatOptionItem.Create(Id + 7, "ShapeshiftDuration", new(1f, 60f, 1f), 15f, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Disperser])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public static void ApplyGameOptions()
    {
        AURoleOptions.ShapeshifterCooldown = DisperserShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = DisableShapeshiftAnimations.GetBool() ? 1f : DisperserShapeshiftDuration.GetFloat();
    }
    public static void DispersePlayers(PlayerControl shapeshifter, bool shapeshiftIsHidden = false)
    {
        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if ((!shapeshiftIsHidden && shapeshifter.PlayerId == pc.PlayerId) || !pc.CanBeTeleported())
            {
                if (!pc.Is(CustomRoles.Disperser) && pc.IsAlive())
                    pc.Notify(ColorString(GetRoleColor(CustomRoles.Disperser), string.Format(GetString("ErrorTeleport"), pc.GetRealName())));
                
                continue;
            }

            pc.RPCPlayCustomSound("Teleport");
            pc.RpcRandomVentTeleport();
            pc.Notify(ColorString(GetRoleColor(CustomRoles.Disperser), string.Format(GetString("TeleportedInRndVentByDisperser"), pc.GetRealName())));
        }
    }
}