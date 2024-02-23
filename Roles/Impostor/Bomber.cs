using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using TOHE.Modules;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Neutral;
using AmongUs.GameOptions;

namespace TOHE.Roles.Impostor;

internal class Bomber : RoleBase
{
    private const int Id = 700;

    public static bool On;
    public override bool IsEnable => On;

    public static OptionItem BomberRadius;
    public static OptionItem BomberCanKill;
    public static OptionItem BomberKillCD;
    public static OptionItem BombCooldown;
    public static OptionItem ImpostorsSurviveBombs;
    public static OptionItem BomberDiesInExplosion;
    public static OptionItem NukerChance;
    public static OptionItem NukeRadius;
    public static OptionItem NukeCooldown;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Bomber);
        BomberRadius = FloatOptionItem.Create(702, "BomberRadius", new(0.5f, 5f, 0.5f), 2f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Bomber])
            .SetValueFormat(OptionFormat.Multiplier);
        BomberCanKill = BooleanOptionItem.Create(703, "CanKill", false, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Bomber]);
        BomberKillCD = FloatOptionItem.Create(704, "KillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false)
            .SetParent(BomberCanKill)
            .SetValueFormat(OptionFormat.Seconds);
        BombCooldown = FloatOptionItem.Create(705, "BombCooldown", new(5f, 180f, 2.5f), 60f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Bomber])
            .SetValueFormat(OptionFormat.Seconds);
        ImpostorsSurviveBombs = BooleanOptionItem.Create(706, "ImpostorsSurviveBombs", true, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Bomber]);
        BomberDiesInExplosion = BooleanOptionItem.Create(707, "BomberDiesInExplosion", true, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Bomber]);
        NukerChance = IntegerOptionItem.Create(708, "NukerChance", new(0, 100, 5), 0, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Bomber])
            .SetValueFormat(OptionFormat.Percent);
        NukeCooldown = FloatOptionItem.Create(709, "NukeCooldown", new(5f, 180f, 2.5f), 60f, TabGroup.ImpostorRoles, false)
            .SetParent(NukerChance)
            .SetValueFormat(OptionFormat.Seconds);
        NukeRadius = FloatOptionItem.Create(710, "NukeRadius", new(1f, 100f, 1f), 25f, TabGroup.ImpostorRoles, false)
            .SetParent(NukerChance)
            .SetValueFormat(OptionFormat.Multiplier);
    }
    public override void Init()
    {
        On = false;
    }
    public override void Add(byte playerId)
    {
        On = true;
    }
    public static bool CheckSpawnNuker()
    {
        var Rand = IRandom.Instance;
        return Rand.Next(0, 100) < NukerChance.GetInt();
    }
    public override bool CanUseKillButton(PlayerControl pc) => BomberCanKill.GetBool() && pc.IsAlive();
    public override void SetKillCooldown(byte id)
    {
        if (BomberCanKill.GetBool())
            Main.AllPlayerKillCooldown[id] = BomberKillCD.GetFloat();
        else
            Main.AllPlayerKillCooldown[id] = 300f;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = Utils.GetPlayerById(playerId).Is(CustomRoles.Bomber) ? BombCooldown.GetFloat() : NukeCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 2f;
    }
    public override void OnShapeshift(PlayerControl shapeshifter, PlayerControl target, bool shapeshifting, bool shapeshiftIsHidden)
    {
        if (!shapeshifting && !shapeshiftIsHidden) return;

        var playerRole = shapeshifter.GetCustomRole();

        Logger.Info("The bomb went off", playerRole.ToString());
        CustomSoundsManager.RPCPlayCustomSoundAll("Boom");
        
        foreach (var tg in Main.AllPlayerControls)
        {
            if (!tg.IsModClient()) tg.KillFlash();
            if (tg.PlayerId == shapeshifter.PlayerId) continue;

            if (!tg.IsAlive() || Pelican.IsEaten(tg.PlayerId) || Medic.ProtectList.Contains(tg.PlayerId) || (tg.Is(CustomRoleTypes.Impostor) && ImpostorsSurviveBombs.GetBool()) || tg.inVent || tg.Is(CustomRoles.Pestilence) || tg.Is(CustomRoles.Solsticer)) continue;

            var pos = shapeshifter.transform.position;
            var dis = Vector2.Distance(pos, tg.transform.position);
            
            if (playerRole.Is(CustomRoles.Bomber))
            {
                if (dis > BomberRadius.GetFloat()) continue;
            }
            else if (playerRole.Is(CustomRoles.Nuker))
            {
                if (dis > NukeRadius.GetFloat()) continue;
            }

            Main.PlayerStates[tg.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
            tg.SetRealKiller(shapeshifter);
            tg.RpcMurderPlayerV3(tg);
            Utils.AfterPlayerDeathTasks(tg);
        }

        var timer = shapeshiftIsHidden ? 0.3f : 1.5f;
        if (BomberDiesInExplosion.GetBool() && playerRole.Is(CustomRoles.Bomber))
        {
            _ = new LateTask(() =>
            {
                var totalAlive = Main.AllAlivePlayerControls.Length;
                if (totalAlive > 0 && !GameStates.IsEnded)
                {
                    Main.PlayerStates[shapeshifter.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                    shapeshifter.RpcMurderPlayerV3(shapeshifter);
                }
                Utils.NotifyRoles();
            }, timer, $"{playerRole} was suicide");
        }
    }
}
