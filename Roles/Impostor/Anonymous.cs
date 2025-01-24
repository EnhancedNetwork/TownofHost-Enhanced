using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Core;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Anonymous : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Anonymous;
    private const int Id = 5300;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Anonymous);
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorHindering;
    //==================================================================\\
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Hack");

    private static OptionItem HackLimitOpt;
    private static OptionItem KillCooldown;

    private static readonly List<byte> DeadBodyList = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Anonymous);
        KillCooldown = FloatOptionItem.Create(Id + 2, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Anonymous])
            .SetValueFormat(OptionFormat.Seconds);
        HackLimitOpt = IntegerOptionItem.Create(Id + 4, "HackLimit", new(1, 15, 1), 3, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Anonymous])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        DeadBodyList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(HackLimitOpt.GetInt());
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = 1f;
        AURoleOptions.ShapeshifterDuration = 1f;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));

        var abilityUse = playerId.GetAbilityUseLimit();
        if (abilityUse > 0)
        {
            hud.AbilityButton.OverrideText(GetString("AnonymousShapeshiftText"));
        }
        hud.AbilityButton.SetUsesRemaining((int)abilityUse);
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target) => DeadBodyList.Clear();
    public override void OnMurderPlayerAsKiller(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (inMeeting || isSuicide) return;

        if (target != null && !DeadBodyList.Contains(target.PlayerId))
            DeadBodyList.Add(target.PlayerId);
    }
    public override void OnShapeshift(PlayerControl shapeshifter, PlayerControl ssTarget, bool IsAnimate, bool shapeshifting)
    {
        if (!shapeshifting || shapeshifter.GetAbilityUseLimit() <= 0 || ssTarget == null || ssTarget.Is(CustomRoles.LazyGuy) || ssTarget.Is(CustomRoles.Lazy) || ssTarget.Is(CustomRoles.NiceMini) && Mini.Age < 18) return;

        shapeshifter.RpcRemoveAbilityUse();

        var targetId = byte.MaxValue;

        // Finding real killer
        foreach (var db in DeadBodyList)
        {
            var dp = Utils.GetPlayerById(db);
            if (dp == null || dp.GetRealKiller() == null) continue;
            if (dp.GetRealKiller().PlayerId == shapeshifter.PlayerId) targetId = db;
        }

        // No body found. Look for another body
        if (targetId == byte.MaxValue && DeadBodyList.Any())
            targetId = DeadBodyList.RandomElement();

        // Anonymous report Self
        if (targetId == byte.MaxValue)
            _ = new LateTask(() => ssTarget?.NoCheckStartMeeting(ssTarget?.Data), 0.15f, "Anonymous Hacking Report Self");
        else
            _ = new LateTask(() => ssTarget?.NoCheckStartMeeting(Utils.GetPlayerById(targetId)?.Data), 0.15f, "Anonymous Hacking Report");
    }
}
