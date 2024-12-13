using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor;

internal class Twister : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Twister;
    private const int Id = 5700;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Twister);
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorHindering;
    //==================================================================\\

    private static OptionItem ShapeshiftCooldown;
    private static OptionItem ShapeshiftDuration;
    private static OptionItem HideTwistedPlayerNames;

    private static HashSet<byte> changePositionPlayers = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Twister);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 10, "TwisterCooldown", new(1f, 180f, 1f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Twister])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftDuration = FloatOptionItem.Create(Id + 11, GeneralOption.ShapeshifterBase_ShapeshiftDuration, new(1f, 999f, 1f), 10f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Twister])
                .SetValueFormat(OptionFormat.Seconds);
        HideTwistedPlayerNames = BooleanOptionItem.Create(Id + 12, "TwisterHideTwistedPlayerNames", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Twister]);
    }
    public override void Init()
    {
        changePositionPlayers.Clear();
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = ShapeshiftDuration.GetFloat();
    }
    public override void OnShapeshift(PlayerControl shapeshifter, PlayerControl targetSS, bool IsAnimate, bool shapeshifting)
    {
        // When is force revert shapeshift
        if (shapeshifter.PlayerId == targetSS.PlayerId && !IsAnimate) return;

        changePositionPlayers = [shapeshifter.PlayerId];

        var rd = IRandom.Instance;
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (changePositionPlayers.Contains(pc.PlayerId) || !pc.CanBeTeleported())
            {
                continue;
            }

            var filtered = Main.AllAlivePlayerControls.Where(a =>
                a.CanBeTeleported() && a.PlayerId != pc.PlayerId && !changePositionPlayers.Contains(a.PlayerId)).ToList();

            if (filtered.Count == 0) return;

            var target = filtered.RandomElement();
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

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton?.OverrideText(GetString("TwisterButtonText"));
    }
}
