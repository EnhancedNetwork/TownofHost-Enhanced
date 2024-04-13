using AmongUs.GameOptions;
using UnityEngine;
using TOHE.Modules;
using TOHE.Roles.Crewmate;

namespace TOHE.Roles.Impostor;

internal class Bomber : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 700;

    private static readonly HashSet<byte> Playerids = [];
    public static bool HasEnabled => Playerids.Any();
    public override bool IsEnable => HasEnabled;
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
    public static OptionItem NukerChance;
    public static OptionItem NukeRadius;
    public static OptionItem NukeCooldown;

    public override void SetupCustomOption()
    {
        //
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
        Playerids.Clear();
    }
    public override void Add(byte playerId)
    {
        Playerids.Add(playerId);
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
    public override bool OnCheckShapeshift(PlayerControl shapeshifter, PlayerControl targetSS, ref bool resetCooldown, ref bool shouldAnimate)
    {
        if (shapeshifter.PlayerId == targetSS.PlayerId) return true;

        var playerRole = shapeshifter.GetCustomRole();

        Logger.Info("The bomb went off", playerRole.ToString());
        CustomSoundsManager.RPCPlayCustomSoundAll("Boom");

        foreach (var target in Main.AllPlayerControls)
        {
            if (!target.IsModClient()) target.KillFlash();
            if (target.PlayerId == shapeshifter.PlayerId) continue;

            if (!target.IsAlive() || Medic.ProtectList.Contains(target.PlayerId) || (target.Is(Custom_Team.Impostor) && ImpostorsSurviveBombs.GetBool()) || target.inVent || target.Is(CustomRoles.Pestilence) || target.Is(CustomRoles.Solsticer)) continue;

            var pos = shapeshifter.transform.position;
            var dis = Vector2.Distance(pos, target.transform.position);
            
            if (playerRole is CustomRoles.Bomber)
            {
                if (dis > BomberRadius.GetFloat()) continue;
            }
            else if (playerRole is CustomRoles.Nuker)
            {
                if (dis > NukeRadius.GetFloat()) continue;
            }

            Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
            target.SetRealKiller(shapeshifter);
            target.RpcMurderPlayer(target);
        }

        if (BomberDiesInExplosion.GetBool() && playerRole is CustomRoles.Bomber)
        {
            _ = new LateTask(() =>
            {
                var totalAlive = Main.AllAlivePlayerControls.Length;
                if (totalAlive > 0 && !GameStates.IsEnded)
                {
                    Main.PlayerStates[shapeshifter.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                    shapeshifter.RpcMurderPlayer(shapeshifter);
                }
                Utils.NotifyRoles();
            }, 0.3f, $"{playerRole} was suicide");
        }

        return false;
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(Translator.GetString("BomberShapeshiftText"));
    }
}
