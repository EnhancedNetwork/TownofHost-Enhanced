using AmongUs.GameOptions;
using TOHE.Modules;
using UnityEngine;

namespace TOHE.Roles.Impostor;

internal class Escapist : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 4000;




    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("abscond");

    private static OptionItem ShapeshiftDuration;
    private static OptionItem ShapeshiftCooldown;

    private static readonly Dictionary<byte, Vector2> EscapeLocation = [];

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Escapist);
        ShapeshiftDuration = FloatOptionItem.Create(Id + 2, GeneralOption.ShapeshifterBase_ShapeshiftDuration, new(1f, 180f, 1f), 1, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Escapist])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 3, GeneralOption.ShapeshifterBase_ShapeshiftCooldown, new(1f, 180f, 1f), 5f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Escapist])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        EscapeLocation.Clear();

    }
    public override void Add(byte playerId)
    {

    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterDuration = ShapeshiftDuration.GetFloat();
        AURoleOptions.ShapeshifterCooldown = EscapeLocation.ContainsKey(playerId) ? ShapeshiftCooldown.GetFloat() : 1f;
    }

    public override bool OnCheckShapeshift(PlayerControl shapeshifter, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate)
    {
        if (shapeshifter.PlayerId == target.PlayerId) return false;

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

        return false;
    }
}
