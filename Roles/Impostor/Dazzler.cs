using AmongUs.GameOptions;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Neutral;
using TOHE.Roles.AddOns.Common;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor;

internal class Dazzler : RoleBase
{
    private const int Id = 5400;
    public static bool On;
    public override bool IsEnable => On;

    private static OptionItem KillCooldown;
    private static OptionItem ShapeshiftCooldown;
    private static OptionItem CauseVision;
    private static OptionItem DazzleLimit;
    private static OptionItem ResetDazzledVisionOnDeath;

    private static Dictionary<byte, List<byte>> PlayersDazzled = [];

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Dazzler);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Dazzler])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 11, "DazzleCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Dazzler])
            .SetValueFormat(OptionFormat.Seconds);
        CauseVision = FloatOptionItem.Create(Id + 13, "DazzlerCauseVision", new(0f, 5f, 0.05f), 0.65f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Dazzler])
            .SetValueFormat(OptionFormat.Multiplier);
        DazzleLimit = IntegerOptionItem.Create(Id + 14, "DazzlerDazzleLimit", new(1, 15, 1), 3, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Dazzler])
            .SetValueFormat(OptionFormat.Times);
        ResetDazzledVisionOnDeath = BooleanOptionItem.Create(Id + 15, "DazzlerResetDazzledVisionOnDeath", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Dazzler]);
    }

    public override void Init()
    {
        PlayersDazzled = [];
        On = false;
    }

    public override void Add(byte playerId)
    {
        PlayersDazzled.TryAdd(playerId, []);
        On = true;
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public override void OnShapeshift(PlayerControl shapeshifter, PlayerControl target, bool shapeshifting, bool shapeshiftIsHidden)
    {
        if (!shapeshifting && !shapeshiftIsHidden) return;
        if (!shapeshifter.IsAlive() || Pelican.IsEaten(shapeshifter.PlayerId)) return;

        if (!PlayersDazzled[shapeshifter.PlayerId].Contains(target.PlayerId) && PlayersDazzled[shapeshifter.PlayerId].Count < DazzleLimit.GetInt())
        {
            Tired.Remove(shapeshifter.PlayerId);
            target.Notify(ColorString(GetRoleColor(CustomRoles.Dazzler), GetString("DazzlerDazzled")));
            PlayersDazzled[shapeshifter.PlayerId].Add(target.PlayerId);
            MarkEveryoneDirtySettings();
        }

        if (shapeshiftIsHidden)
            shapeshifter.Notify(GetString("RejectShapeshift.AbilityWasUsed"), time: 2f);
    }

    public static void SetDazzled(PlayerControl player, IGameOptions opt)
    {
        if (PlayersDazzled.Any(a => a.Value.Contains(player.PlayerId) &&
           (!ResetDazzledVisionOnDeath.GetBool() || Main.AllAlivePlayerControls.Any(b => b.PlayerId == a.Key))))
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, CauseVision.GetFloat());
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, CauseVision.GetFloat());
        }
    }
}