using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Crewmate;
using UnityEngine;

namespace TOHE.Roles.Impostor;

internal class Bomber : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Bomber;
    private const int Id = 700;

    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Bomb");

    public static OptionItem BomberRadius;
    public static OptionItem BomberCanKill;
    public static OptionItem BomberKillCD;
    public static OptionItem BombCooldown;
    public static OptionItem ImpostorsSurviveBombs;
    public static OptionItem BomberDiesInExplosion;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Bomber);
        BomberRadius = FloatOptionItem.Create(Id + 2, "BomberRadius", new(0.5f, 100f, 0.5f), 2f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Bomber])
            .SetValueFormat(OptionFormat.Multiplier);
        BomberCanKill = BooleanOptionItem.Create(Id + 3, GeneralOption.CanKill, false, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Bomber]);
        BomberKillCD = FloatOptionItem.Create(Id + 4, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false)
            .SetParent(BomberCanKill)
            .SetValueFormat(OptionFormat.Seconds);
        BombCooldown = FloatOptionItem.Create(Id + 5, "BombCooldown", new(5f, 180f, 2.5f), 60f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Bomber])
            .SetValueFormat(OptionFormat.Seconds);
        ImpostorsSurviveBombs = BooleanOptionItem.Create(Id + 6, "ImpostorsSurviveBombs", true, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Bomber]);
        BomberDiesInExplosion = BooleanOptionItem.Create(Id + 7, "BomberDiesInExplosion", true, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Bomber]);
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
        AURoleOptions.ShapeshifterCooldown = BombCooldown.GetFloat();
    }
    public override void UnShapeShiftButton(PlayerControl shapeshifter)
    {
        var playerRole = shapeshifter.GetCustomRole();

        Logger.Info("The bomb went off", playerRole.ToString());
        CustomSoundsManager.RPCPlayCustomSoundAll("Boom");

        _ = new Explosion(5f, 0.5f, shapeshifter.GetCustomPosition());

        foreach (var target in Main.AllPlayerControls)
        {
            if (!target.IsModded()) target.KillFlash();
            if (target.PlayerId == shapeshifter.PlayerId) continue;

            if (!target.IsAlive() || Medic.IsProtected(target.PlayerId) || (target.Is(Custom_Team.Impostor) && ImpostorsSurviveBombs.GetBool()) || target.inVent || target.IsTransformedNeutralApocalypse() || target.Is(CustomRoles.Solsticer)) continue;

            var pos = shapeshifter.transform.position;
            var dis = Utils.GetDistance(pos, target.transform.position);
            if (dis > BomberRadius.GetFloat()) continue;

            target.SetDeathReason(PlayerState.DeathReason.Bombed);
            target.RpcMurderPlayer(target);
            target.SetRealKiller(shapeshifter);
        }

        if (BomberDiesInExplosion.GetBool() && playerRole is CustomRoles.Bomber)
        {
            _ = new LateTask(() =>
            {
                var totalAlive = Main.AllAlivePlayerControls.Length;
                if (totalAlive > 0 && !GameStates.IsEnded)
                {
                    shapeshifter.SetDeathReason(PlayerState.DeathReason.Bombed);
                    shapeshifter.RpcMurderPlayer(shapeshifter);
                }
            }, 0.3f, $"{playerRole} was suicide");
        }
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(Translator.GetString("BomberShapeshiftText"));
    }
}
