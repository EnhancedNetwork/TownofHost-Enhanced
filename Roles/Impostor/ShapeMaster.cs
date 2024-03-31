using AmongUs.GameOptions;
using System.Collections.Generic;
using System.Linq;

namespace TOHE.Roles.Impostor;

internal class ShapeMaster : RoleBase // Should be deleted tbh, because it's litteraly vanilla shapeshifter
{
    //===========================SETUP================================\\
    private const int Id = 4500;
    private static readonly HashSet<byte> Playerids = [];
    public static bool HasEnabled => Playerids.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    //==================================================================\\

    private static OptionItem ShapeMasterShapeshiftDuration;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.ShapeMaster);
        ShapeMasterShapeshiftDuration = FloatOptionItem.Create(Id + 2, "ShapeshiftDuration", new(1, 60, 1), 10, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.ShapeMaster])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        Playerids.Clear();
    }
    public override void Add(byte playerId)
    {
        Playerids.Add     (playerId);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = 1f;
        AURoleOptions.ShapeshifterLeaveSkin = false;
        AURoleOptions.ShapeshifterDuration = ShapeMasterShapeshiftDuration.GetFloat();
    }
}
