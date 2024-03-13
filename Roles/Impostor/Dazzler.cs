using AmongUs.GameOptions;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Neutral;
using TOHE.Roles.AddOns.Common;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor;

public static class Dazzler
{
    private static readonly int Id = 5400;
    public static List<byte> playerIdList = [];
    public static bool IsEnable = false;

    public static Dictionary<byte, List<byte>> PlayersDazzled = [];

    private static OptionItem KillCooldown;
    private static OptionItem ShapeshiftCooldown;
    //    private static OptionItem ShapeshiftDuration;
    private static OptionItem CauseVision;
    private static OptionItem DazzleLimit;
    private static OptionItem ResetDazzledVisionOnDeath;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Dazzler);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Dazzler])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 11, "DazzleCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Dazzler])
            .SetValueFormat(OptionFormat.Seconds);
        //     ShapeshiftDuration = FloatOptionItem.Create(Id + 12, "ShapeshiftDuration", new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Dazzler])
        //       .SetValueFormat(OptionFormat.Seconds);
        CauseVision = FloatOptionItem.Create(Id + 13, "DazzlerCauseVision", new(0f, 5f, 0.05f), 0.65f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Dazzler])
            .SetValueFormat(OptionFormat.Multiplier);
        DazzleLimit = IntegerOptionItem.Create(Id + 14, "DazzlerDazzleLimit", new(1, 15, 1), 3, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Dazzler])
            .SetValueFormat(OptionFormat.Times);
        ResetDazzledVisionOnDeath = BooleanOptionItem.Create(Id + 15, "DazzlerResetDazzledVisionOnDeath", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Dazzler]);
    }

    public static void Init()
    {
        playerIdList = [];
        PlayersDazzled = [];
        IsEnable = false;
    }

    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        PlayersDazzled.TryAdd(playerId, []);
        IsEnable = true;
    }

    public static void ApplyGameOptions()
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }

    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public static void OnShapeshift(PlayerControl pc, PlayerControl target)
    {
        if (!pc.IsAlive() || Pelican.IsEaten(pc.PlayerId)) return;

        if (!PlayersDazzled[pc.PlayerId].Contains(target.PlayerId) && PlayersDazzled[pc.PlayerId].Count < DazzleLimit.GetInt())
        {
            Tired.Remove(pc.PlayerId);
            target.Notify(ColorString(GetRoleColor(CustomRoles.Dazzler), GetString("DazzlerDazzled")));
            PlayersDazzled[pc.PlayerId].Add(target.PlayerId);
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
}