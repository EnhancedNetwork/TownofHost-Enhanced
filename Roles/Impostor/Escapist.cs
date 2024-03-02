using AmongUs.GameOptions;
using System.Collections.Generic;
using TOHE.Modules;
using UnityEngine;

namespace TOHE.Roles.Impostor;

internal class Escapist : RoleBase
{
    private const int Id = 4000;

    public static bool On;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;

    private static OptionItem ShapeshiftDuration;
    private static OptionItem ShapeshiftCooldown;

    private static Dictionary<byte, Vector2> EscapeLocation = [];

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Escapist);
        ShapeshiftDuration = FloatOptionItem.Create(Id + 2, "ShapeshiftDuration", new(1f, 180f, 1f), 1, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Escapist])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 3, "ShapeshiftCooldown", new(1f, 180f, 1f), 5f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Escapist])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        EscapeLocation = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        On = true;
    }

    public override Sprite AbilityButtonSprite => CustomButton.Get("abscond");
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterDuration = ShapeshiftDuration.GetFloat();
        AURoleOptions.ShapeshifterCooldown = EscapeLocation.ContainsKey(playerId) ? ShapeshiftCooldown.GetFloat() : 1f;
    }

    public override void OnShapeshift(PlayerControl shapeshifter, PlayerControl target, bool shapeshifting, bool shapeshiftIsHidden)
    {
        if (!shapeshifting && !shapeshiftIsHidden) return;

        if (EscapeLocation.TryGetValue(shapeshifter.PlayerId, out var position))
        {
            EscapeLocation.Remove(shapeshifter.PlayerId);
            Logger.Info($"{shapeshifter.GetNameWithRole()}:{position}", "Escapist Teleport");
            shapeshifter.RpcTeleport(position);
            shapeshifter.RPCPlayCustomSound("Teleport");
        }
        else
        {
            EscapeLocation.Add(shapeshifter.PlayerId, shapeshifter.GetCustomPosition());
            shapeshifter.SyncSettings();
            shapeshifter.Notify(Translator.GetString("EscapisMtarkedPosition"));
        }
    }
}
