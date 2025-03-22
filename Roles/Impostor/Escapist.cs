using AmongUs.GameOptions;
using TOHE.Modules;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Escapist : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Escapist;
    private const int Id = 4000;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("abscond");

    private static OptionItem ShapeshiftCooldown;

    private static readonly Dictionary<byte, Vector2> EscapeLocation = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Escapist);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 3, GeneralOption.ShapeshifterBase_ShapeshiftCooldown, new(1f, 180f, 1f), 5f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Escapist])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        EscapeLocation.Clear();
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = EscapeLocation.ContainsKey(playerId) ? ShapeshiftCooldown.GetFloat() : 1f;
    }

    public override void UnShapeShiftButton(PlayerControl shapeshifter)
    {
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
            shapeshifter.Notify(GetString("EscapisMtarkedPosition"));
        }
    }
}
