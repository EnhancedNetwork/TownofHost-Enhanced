using Hazel;
using TOHE.Roles.Core;
using InnerNet;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;
using System;
using UnityEngine;
using AmongUs.GameOptions;

namespace TOHE.Roles.Coven;

internal class Conjurer : CovenManager
{
    private enum ConjState
    {
        NormalMark,
        NormalBomb,
        NecroMark,
        NecroBomb
    }
    //===========================SETUP================================\\
    private const int Id = 30300;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Conjurer);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenKilling;
    //==================================================================\\
    private static OptionItem ConjureCooldown;
    private static OptionItem ConjureRadius;
    private static OptionItem NecroRadius;
    private static OptionItem CovenDiesInBlast;

    public static byte NecroBombHolder = byte.MaxValue;
    private static readonly Dictionary<byte, List<Vector3>> ConjPosition = [];
    private static readonly Dictionary<byte, ConjState> state = [];


    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, CustomRoles.Conjurer, 1, zeroOne: false);
        ConjureCooldown = FloatOptionItem.Create(Id + 10, "ConjurerCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Conjurer])
            .SetValueFormat(OptionFormat.Seconds);
        ConjureRadius = FloatOptionItem.Create(Id + 11, "ConjurerRadius", new(0.5f, 100f, 0.5f), 2f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Conjurer])
            .SetValueFormat(OptionFormat.Multiplier);
        NecroRadius = FloatOptionItem.Create(Id + 12, "ConjurerNecroRadius", new(0.5f, 100f, 0.5f), 3f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Conjurer])
            .SetValueFormat(OptionFormat.Multiplier);
        CovenDiesInBlast = BooleanOptionItem.Create(Id + 13, "ConjurerCovenDies", false, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Conjurer]);
    }
    public override void Init()
    {
        NecroBombHolder = byte.MaxValue;
        ConjPosition.Clear();
    }
    public override void Add(byte playerId)
    {
        ConjPosition[playerId] = [];
        state.TryAdd(playerId, ConjState.NormalMark);
    }
    public override bool CanUseKillButton(PlayerControl pc) => HasNecronomicon(pc);
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = ConjureCooldown.GetFloat();
    }
    public override bool OnCheckShapeshift(PlayerControl shapeshifter, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate)
    {
        resetCooldown = true;
        Logger.Info($"Conjurer ShapeShift", "Conjurer");
        if (shapeshifter.PlayerId == target.PlayerId) return false;
        if (state[shapeshifter.PlayerId] != ConjState.NecroBomb && state[shapeshifter.PlayerId] != ConjState.NormalBomb)
            state[shapeshifter.PlayerId] = HasNecronomicon(shapeshifter) ? ConjState.NecroMark : ConjState.NormalMark;
        switch (state[shapeshifter.PlayerId])
        {
            case ConjState.NormalMark:
                ConjPosition[shapeshifter.PlayerId].Add(shapeshifter.transform.position);
                state[shapeshifter.PlayerId] = ConjState.NormalBomb;
                shapeshifter.Notify(GetString("ConjurerMark"));
                break;

            case ConjState.NormalBomb:
                foreach (var player in Main.AllAlivePlayerControls)
                {
                    foreach (var pos in ConjPosition[shapeshifter.PlayerId].ToArray())
                    {
                        var dis = GetDistance(pos, player.transform.position);
                        if (dis > ConjureRadius.GetFloat()) continue;
                        if (player.IsPlayerCoven() && !CovenDiesInBlast.GetBool()) continue;
                        else
                        {
                            player.SetDeathReason(PlayerState.DeathReason.Bombed);
                            player.RpcMurderPlayer(player);
                            player.SetRealKiller(shapeshifter);
                        }
                    }
                }
                shapeshifter.Notify(GetString("ConjurerMeteor"));
                state[shapeshifter.PlayerId] = ConjState.NormalMark;
                break;
            case ConjState.NecroMark:
                NecroBombHolder = target.PlayerId;
                state[shapeshifter.PlayerId] = ConjState.NecroBomb;
                shapeshifter.Notify(GetString("ConjurerNecroMark"));
                break;
            case ConjState.NecroBomb:
                foreach (var player in Main.AllAlivePlayerControls)
                {
                    var dis = GetDistance(GetPlayerById(NecroBombHolder).transform.position, player.transform.position);
                    if (dis > NecroRadius.GetFloat()) continue;
                    if (player.IsPlayerCoven() && !CovenDiesInBlast.GetBool()) continue;
                    else
                    {
                        player.SetDeathReason(PlayerState.DeathReason.Bombed);
                        player.RpcMurderPlayer(player);
                        player.SetRealKiller(shapeshifter);
                    }
                    
                }
                shapeshifter.Notify(GetString("ConjurerMeteor"));
                state[shapeshifter.PlayerId] = ConjState.NecroMark;
                NecroBombHolder = byte.MaxValue;
                break;
        }
        return false;
    }
}