using AmongUs.GameOptions;
using TOHE.Modules;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor;

internal class Twister : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 5700;
    private static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    //==================================================================\\

    private static OptionItem ShapeshiftCooldown;
    private static OptionItem ShapeshiftDuration;
    private static OptionItem HideTwistedPlayerNames;

    private static List<byte> changePositionPlayers = [];

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Twister);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 10, "TwisterCooldown", new(1f, 180f, 1f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Twister])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftDuration = FloatOptionItem.Create(Id + 11, "ShapeshiftDuration", new(1f, 999f, 1f), 10f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Twister])
                .SetValueFormat(OptionFormat.Seconds);
        HideTwistedPlayerNames = BooleanOptionItem.Create(Id + 12, "TwisterHideTwistedPlayerNames", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Twister]);
    }
    public override void Init()
    {
        PlayerIds.Clear();
        changePositionPlayers = [];
    }
    public override void Add(byte playerId)
    {
        PlayerIds.Add(playerId);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = ShapeshiftDuration.GetFloat();
    }
    public override void OnShapeshift(PlayerControl shapeshifter, PlayerControl targetSS, bool shapeshifting, bool shapeshiftIsHidden)
    {
        changePositionPlayers = [];

        if (!shapeshiftIsHidden)
            changePositionPlayers.Add(shapeshifter.PlayerId);

        var rd = IRandom.Instance;
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (changePositionPlayers.Contains(pc.PlayerId) || !pc.CanBeTeleported())
            {
                continue;
            }

            var filtered = Main.AllAlivePlayerControls.Where(a =>
                pc.CanBeTeleported() && a.PlayerId != pc.PlayerId && !changePositionPlayers.Contains(a.PlayerId)).ToList();

            if (filtered.Count == 0) return;

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

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton?.OverrideText(GetString("TwisterButtonText"));
    }
}