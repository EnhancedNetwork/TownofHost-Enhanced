using AmongUs.GameOptions;
using TOHE.Roles.AddOns.Common;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor;

internal class Dazzler : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Dazzler;
    private const int Id = 5400;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorHindering;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem ShapeshiftCooldown;
    private static OptionItem CauseVision;
    private static OptionItem DazzleLimit;
    private static OptionItem ResetDazzledVisionOnDeath;
    private static OptionItem ShowShapeshiftAnimationsOpt;

    private static Dictionary<byte, HashSet<byte>> PlayersDazzled = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Dazzler);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Dazzler])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 11, "DazzleCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Dazzler])
            .SetValueFormat(OptionFormat.Seconds);
        CauseVision = FloatOptionItem.Create(Id + 13, "DazzlerCauseVision", new(0f, 5f, 0.05f), 0.65f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Dazzler])
            .SetValueFormat(OptionFormat.Multiplier);
        DazzleLimit = IntegerOptionItem.Create(Id + 14, "DazzlerDazzleLimit", new(1, 15, 1), 3, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Dazzler])
            .SetValueFormat(OptionFormat.Times);
        ResetDazzledVisionOnDeath = BooleanOptionItem.Create(Id + 15, "DazzlerResetDazzledVisionOnDeath", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Dazzler]);
        ShowShapeshiftAnimationsOpt = BooleanOptionItem.Create(Id + 16, GeneralOption.ShowShapeshiftAnimations, true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Dazzler]);
    }

    public override void Init()
    {
        PlayersDazzled = [];
    }

    public override void Add(byte playerId)
    {
        PlayersDazzled.TryAdd(playerId, []);
    }

    public override void Remove(byte playerId)
    {
        PlayersDazzled.Remove(playerId);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public override bool OnCheckShapeshift(PlayerControl shapeshifter, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate)
    {
        if (ShowShapeshiftAnimationsOpt.GetBool() || shapeshifter.PlayerId == target.PlayerId) return true;

        DoDazzled(shapeshifter, target);
        shapeshifter.Notify(GetString("RejectShapeshift.AbilityWasUsed"), time: 2f);
        return false;
    }
    public override void OnShapeshift(PlayerControl shapeshifter, PlayerControl target, bool IsAnimate, bool shapeshifting)
    {
        if (!shapeshifting) return;

        DoDazzled(shapeshifter, target);
    }

    private static void DoDazzled(PlayerControl shapeshifter, PlayerControl target)
    {
        if (!PlayersDazzled[shapeshifter.PlayerId].Contains(target.PlayerId) && PlayersDazzled[shapeshifter.PlayerId].Count < DazzleLimit.GetInt())
        {
            Tired.RemoveMidGame(shapeshifter.PlayerId);
            target.Notify(ColorString(GetRoleColor(CustomRoles.Dazzler), GetString("DazzlerDazzled")));
            PlayersDazzled[shapeshifter.PlayerId].Add(target.PlayerId);
            MarkEveryoneDirtySettings();
        }
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

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(GetString("DazzleButtonText"));
    }
}
