using AmongUs.GameOptions;
using System;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Magician : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 28400;
    private static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    //==================================================================\\

    private static OptionItem DefaultKillCooldown;
    private static OptionItem ReduceKillCooldown;
    private static OptionItem ShapeshiftCooldown;
    public static OptionItem ResetShapeshiftCooldown;

    //   private static OptionItem ShapeshiftDuration;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Magician);
        DefaultKillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Magician])
            .SetValueFormat(OptionFormat.Seconds);
        ReduceKillCooldown = FloatOptionItem.Create(Id + 11, "MagicianSwapKillCooldown", new(0f, 180f, 2.5f), 10f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Magician])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 12, "MagicianSwapCooldown", new(0f, 180f, 2.5f), 15f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Magician])
            .SetValueFormat(OptionFormat.Seconds);
        ResetShapeshiftCooldown = BooleanOptionItem.Create(Id + 13, "MagicianResetShapeshiftCooldown", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Magician]);
        //     ShapeshiftDuration = FloatOptionItem.Create(Id + 15, "ShapeshiftDuration", new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Magician])
        //       .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        PlayerIds.Clear();
    }
    public override void Add(byte playerId)
    {
        PlayerIds.Add(playerId);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 0f;

        int KillDistanceSetting = opt.GetInt(Int32OptionNames.KillDistance);
        KillDistanceSetting += 1;
        opt.SetInt(Int32OptionNames.KillDistance, KillDistanceSetting);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = DefaultKillCooldown.GetFloat();

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!DisableShieldAnimations.GetBool()) {   // Play shield animation for Killer & Target only.
            PlayerControl[] objects = { target, killer };
            foreach (PlayerControl obj1 in objects)
            {
                foreach (PlayerControl obj2 in objects)
                {
                    obj1.RpcGuardAndKill(obj2);
                }
            }
        }

        target.SetRealKiller(killer);
        target.RpcMurderPlayer(target);
        killer.SetKillCooldown(DefaultKillCooldown.GetFloat());
        RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);

        if (ResetShapeshiftCooldown.GetBool()) {
            killer.RpcResetAbilityCooldown();
        }
        return false;
    }

    public override bool OnCheckShapeshift(PlayerControl pc, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate)
    {
        shouldAnimate = false;
        resetCooldown = false;
        // Get player positions.
        Vector2 TargetPos = target.GetCustomPosition();
        Vector2 MagicianPos = pc.GetCustomPosition();

        // If players can be swapped.
        if (!target.IsAlive() || !target.CanBeTeleported() || Pelican.IsEaten(pc.PlayerId) || Pelican.IsEaten(target.PlayerId)) {
            AURoleOptions.ShapeshifterCooldown = 0;
            pc.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Magician), target.IsAlive() ? GetString("CouldNotSwap") : GetString("CanNotSwapWithDead")));
            return false;
        }

        // Play shield animation for all Players.
        for (int i = 0; i <= Main.AllPlayerControls.Length; i++)
        {
            try
            {
                {
                    Main.AllPlayerControls[i].RpcGuardAndKill(target);
                    Main.AllPlayerControls[i].RpcGuardAndKill(pc);
                }
            }
        catch (Exception) {break;}
        }

        // Swap location with Target.
        pc.SetKillCooldown(ReduceKillCooldown.GetFloat());
        target.RpcTeleport(MagicianPos);
        pc.RpcTeleport(TargetPos);
        pc.RpcResetAbilityCooldown();
        pc.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Magician), GetString("SwappedWithPlayer")));
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
        return false;
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("MagicianKillButtonText"));
        hud.AbilityButton.OverrideText(GetString("MagicianSwapButtonText"));
    }
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("prophecies");
}
