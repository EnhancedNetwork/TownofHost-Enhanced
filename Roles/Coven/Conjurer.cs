using AmongUs.GameOptions;
using TOHE.Modules;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Coven;

internal class Conjurer : CovenManager
{
    [Obfuscation(Exclude = true)]
    private enum ConjState
    {
        NormalMark,
        NormalBomb,
        NecroMark,
        NecroBomb
    }
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Conjurer;
    private const int Id = 30300;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenKilling;
    //==================================================================\\
    private static OptionItem ConjureCooldown;
    private static OptionItem ConjureRadius;
    private static OptionItem NecroRadius;
    private static OptionItem CovenDiesInBlast;
    private static OptionItem KillCooldown;
    private static OptionItem ResetTargetAfterMeeting;

    public static byte NecroBombHolder = byte.MaxValue;
    private static readonly Dictionary<byte, List<Vector3>> ConjPosition = [];
    private static readonly Dictionary<byte, ConjState> state = [];


    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, CustomRoles.Conjurer, 1, zeroOne: false);
        ConjureCooldown = FloatOptionItem.Create(Id + 10, "ConjurerCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Conjurer])
            .SetValueFormat(OptionFormat.Seconds);
        KillCooldown = FloatOptionItem.Create(Id + 14, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Conjurer])
            .SetValueFormat(OptionFormat.Seconds);
        ConjureRadius = FloatOptionItem.Create(Id + 11, "ConjurerRadius", new(0.5f, 100f, 0.5f), 2f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Conjurer])
            .SetValueFormat(OptionFormat.Multiplier);
        NecroRadius = FloatOptionItem.Create(Id + 12, "ConjurerNecroRadius", new(0.5f, 100f, 0.5f), 3f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Conjurer])
            .SetValueFormat(OptionFormat.Multiplier);
        CovenDiesInBlast = BooleanOptionItem.Create(Id + 13, "ConjurerCovenDies", false, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Conjurer]);
        ResetTargetAfterMeeting = BooleanOptionItem.Create(Id + 15, "ConjurerResetTarget", false, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Conjurer]);
    }
    public override void Init()
    {
        NecroBombHolder = byte.MaxValue;
        ConjPosition.Clear();
    }
    public override void Add(byte playerId)
    {
        ConjPosition[playerId] = [];
        state[playerId] = ConjState.NormalMark;
    }
    public override bool CanUseKillButton(PlayerControl pc) => HasNecronomicon(pc);
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = ConjureCooldown.GetFloat();
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!CanUseKillButton(killer)) return false;
        if (HasNecronomicon(killer) && !target.GetCustomRole().IsCovenTeam())
        {
            return true;
        }
        killer.Notify(GetString("CovenDontKillOtherCoven"));
        return false;
    }
    public override bool OnCheckShapeshift(PlayerControl shapeshifter, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate)
    {
        resetCooldown = true;
        var shapeshifterId = shapeshifter.PlayerId;
        if (target != null && shapeshifterId == target.PlayerId) return false;

        if (state[shapeshifterId] != ConjState.NecroBomb && state[shapeshifterId] != ConjState.NormalBomb)
            state[shapeshifterId] = HasNecronomicon(shapeshifterId) ? ConjState.NecroMark : ConjState.NormalMark;

        Logger.Info($"Conjurer ShapeShift, current state: {state[shapeshifterId]}", "Conjurer");

        switch (state[shapeshifterId])
        {
            case ConjState.NormalMark:
                ConjPosition[shapeshifterId].Add(shapeshifter.transform.position);
                state[shapeshifterId] = ConjState.NormalBomb;
                shapeshifter.Notify(GetString("ConjurerMark"));
                break;

            case ConjState.NormalBomb:
                foreach (var player in Main.AllAlivePlayerControls)
                {
                    foreach (var pos in ConjPosition[shapeshifterId].ToArray())
                    {
                        var dis = GetDistance(pos, player.transform.position);
                        if (dis > ConjureRadius.GetFloat()) continue;
                        if (player.GetCustomRole().IsCovenTeam() && !CovenDiesInBlast.GetBool()) continue;
                        if (player.IsTransformedNeutralApocalypse()) continue;
                        else
                        {
                            player.SetDeathReason(PlayerState.DeathReason.Bombed);
                            player.RpcMurderPlayer(player);
                            player.SetRealKiller(shapeshifter);
                        }
                    }
                }
                shapeshifter.Notify(GetString("ConjurerMeteor"));
                state[shapeshifterId] = ConjState.NormalMark;
                ConjPosition[shapeshifterId].Clear();
                CustomSoundsManager.RPCPlayCustomSoundAll("Boom");
                break;
            case ConjState.NecroMark:
                if (target == null)
                {
                    Logger.Info("target is null", "ConjState.NecroMark");
                    return false;
                }
                NecroBombHolder = target.PlayerId;
                state[shapeshifterId] = ConjState.NecroBomb;
                shapeshifter.Notify(GetString("ConjurerNecroMark"));
                break;
            case ConjState.NecroBomb:
                var necroBombHolder = NecroBombHolder.GetPlayer();
                if (necroBombHolder == null)
                {
                    Logger.Info("NecroBombHolder is null", "ConjState.NecroBomb");
                    return false;
                }

                foreach (var player in Main.AllAlivePlayerControls)
                {
                    var dis = GetDistance(necroBombHolder.transform.position, player.transform.position);
                    if (dis > NecroRadius.GetFloat()) continue;
                    if (player.GetCustomRole().IsCovenTeam() && !CovenDiesInBlast.GetBool()) continue;
                    if (player.IsTransformedNeutralApocalypse()) continue;
                    else
                    {
                        player.SetDeathReason(PlayerState.DeathReason.Bombed);
                        player.RpcMurderPlayer(player);
                        player.SetRealKiller(shapeshifter);
                    }

                }
                shapeshifter.Notify(GetString("ConjurerMeteor"));
                state[shapeshifterId] = ConjState.NecroMark;
                NecroBombHolder = byte.MaxValue;
                CustomSoundsManager.RPCPlayCustomSoundAll("Boom");
                break;
        }
        return false;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        if (!state.TryGetValue(playerId, out var conjState)) return;

        if (conjState is ConjState.NormalMark or ConjState.NecroMark)
        {
            hud.AbilityButton.OverrideText(GetString("MarkButtonText"));
        }
        else if (conjState is ConjState.NormalBomb or ConjState.NecroBomb)
        {
            hud.AbilityButton.OverrideText(GetString("ConjurerConjureShapeshift"));
        }
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (!ResetTargetAfterMeeting.GetBool()) return;
        if (!state.TryGetValue(_Player.PlayerId, out var conjState)) return;
        if (conjState == ConjState.NormalBomb)
        {
            state[_Player.PlayerId] = ConjState.NormalMark;
            ConjPosition[_Player.PlayerId].Clear();
        }
        else if (conjState == ConjState.NecroBomb)
        {
            state[_Player.PlayerId] = ConjState.NecroMark;
            NecroBombHolder = byte.MaxValue;
        }
    }
}
