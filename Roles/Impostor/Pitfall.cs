using AmongUs.GameOptions;
using TOHE.Roles.Core;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Pitfall : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Pitfall;
    private const int Id = 5600;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorHindering;
    //==================================================================\\

    private static OptionItem ShapeshiftCooldown;
    private static OptionItem MaxTrapCount;
    private static OptionItem TrapMaxPlayerCountOpt;
    private static OptionItem TrapDurationOpt;
    private static OptionItem TrapRadius;
    private static OptionItem TrapFreezeTime;
    private static OptionItem TrapCauseVision;
    private static OptionItem TrapCauseVisionTime;

    private static HashSet<PitfallTrap> Traps = [];
    private static readonly HashSet<byte> ReducedVisionPlayers = [];

    private static float DefaultSpeed = new();
    public static float TrapMaxPlayerCount = new();
    public static float TrapDuration = new();

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Pitfall);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 10, "PitfallTrapCooldown", new(1f, 180f, 1f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pitfall])
            .SetValueFormat(OptionFormat.Seconds);
        MaxTrapCount = FloatOptionItem.Create(Id + 11, "PitfallMaxTrapCount", new(1f, 5f, 1f), 1f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pitfall])
            .SetValueFormat(OptionFormat.Times);
        TrapMaxPlayerCountOpt = FloatOptionItem.Create(Id + 12, "PitfallTrapMaxPlayerCount", new(1f, 15f, 1f), 3f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pitfall])
            .SetValueFormat(OptionFormat.Times);
        TrapDurationOpt = FloatOptionItem.Create(Id + 13, "PitfallTrapDuration", new(5f, 180f, 1f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pitfall])
            .SetValueFormat(OptionFormat.Seconds);
        TrapRadius = FloatOptionItem.Create(Id + 14, "PitfallTrapRadius", new(0.5f, 5f, 0.5f), 2f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pitfall])
            .SetValueFormat(OptionFormat.Multiplier);
        TrapFreezeTime = FloatOptionItem.Create(Id + 15, "PitfallTrapFreezeTime", new(0f, 30f, 1f), 5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pitfall])
            .SetValueFormat(OptionFormat.Seconds);
        TrapCauseVision = FloatOptionItem.Create(Id + 16, "PitfallTrapCauseVision", new(0f, 5f, 0.05f), 0.2f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pitfall])
            .SetValueFormat(OptionFormat.Multiplier);
        TrapCauseVisionTime = FloatOptionItem.Create(Id + 17, "PitfallTrapCauseVisionTime", new(0f, 45f, 1f), 15f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pitfall])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        Traps.Clear();
        ReducedVisionPlayers.Clear();
        DefaultSpeed = new();
        TrapMaxPlayerCount = new();
        TrapDuration = new();
    }
    public override void Add(byte playerId)
    {
        DefaultSpeed = Main.AllPlayerSpeed[playerId];

        TrapMaxPlayerCount = TrapMaxPlayerCountOpt.GetFloat();
        TrapDuration = TrapDurationOpt.GetFloat();

        if (AmongUsClient.Instance.AmHost)
        {
            CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOthers);
        }
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
    }

    public override void SetAbilityButtonText(HudManager hud, byte id) => hud.AbilityButton.OverrideText(Translator.GetString("PitfallButtonText"));
    // public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Set Trap");

    public override void UnShapeShiftButton(PlayerControl shapeshifter)
    {
        //if (!CheckUnshapeshift) return;
        Logger.Info($"Triggered Pitfall Ability!!!", "Pitfall");


        // Remove inactive traps so there is room for new traps
        Traps = Traps.Where(a => a.IsActive).ToHashSet();

        Vector2 position = shapeshifter.transform.position;
        var playerTraps = Traps.Where(a => a.PitfallPlayerId == shapeshifter.PlayerId).ToArray();
        if (playerTraps.Length >= MaxTrapCount.GetInt())
        {
            var trap = playerTraps.First();
            trap.Location = position;
            trap.PlayersTrapped = [];
            trap.Timer = 0;
        }
        else
        {
            Traps.Add(new PitfallTrap
            {
                PitfallPlayerId = shapeshifter.PlayerId,
                Location = position,
                PlayersTrapped = [],
                Timer = 0
            });
        }

        shapeshifter.Notify(GetString("RejectShapeshift.AbilityWasUsed"), time: 2f);
    }

    private void OnFixedUpdateOthers(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (lowLoad || Pelican.IsEaten(player.PlayerId) || !player.IsAlive()) return;

        if (player.Is(Custom_Team.Impostor))
        {
            var traps = Traps.Where(a => a.PitfallPlayerId == player.PlayerId && a.IsActive).ToArray();
            foreach (var trap in traps)
            {
                trap.Timer += Time.fixedDeltaTime;
            }
            return;
        }

        Vector2 position = player.transform.position;

        foreach (var trap in Traps.Where(a => a.IsActive).ToArray())
        {
            if (trap.PlayersTrapped.Contains(player.PlayerId))
            {
                continue;
            }

            var dis = Utils.GetDistance(trap.Location, position);
            if (dis > TrapRadius.GetFloat()) continue;

            if (TrapFreezeTime.GetFloat() > 0)
            {
                TrapPlayer(player);
            }

            if (TrapCauseVisionTime.GetFloat() > 0)
            {
                ReducePlayerVision(player);
            }

            trap.PlayersTrapped.Add(player.PlayerId);

            player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Pitfall), GetString("PitfallTrap")));
        }
    }

    public static void SetPitfallTrapVision(IGameOptions opt, PlayerControl target)
    {
        if (ReducedVisionPlayers.Contains(target.PlayerId))
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, TrapCauseVision.GetFloat());
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, TrapCauseVision.GetFloat());
        }
    }

    private static void TrapPlayer(PlayerControl player)
    {
        Main.AllPlayerSpeed[player.PlayerId] = Main.MinSpeed;
        ReportDeadBodyPatch.CanReport[player.PlayerId] = false;
        RPC.PlaySoundRPC(Sounds.SabotageSound, player.PlayerId);
        player.MarkDirtySettings();

        _ = new LateTask(() =>
        {
            Main.AllPlayerSpeed[player.PlayerId] = DefaultSpeed;
            ReportDeadBodyPatch.CanReport[player.PlayerId] = true;
            RPC.PlaySoundRPC(Sounds.TaskComplete, player.PlayerId);
            player.MarkDirtySettings();
        }, TrapFreezeTime.GetFloat(), "Pitfall Trap Player Freeze");
    }

    private static void ReducePlayerVision(PlayerControl player)
    {
        if (ReducedVisionPlayers.Contains(player.PlayerId)) return;

        ReducedVisionPlayers.Add(player.PlayerId);
        player.MarkDirtySettings();

        _ = new LateTask(() =>
        {
            ReducedVisionPlayers.Remove(player.PlayerId);
            player.MarkDirtySettings();
        }, TrapCauseVisionTime.GetFloat(), "Pitfall Trap Player Vision");
    }
}

public class PitfallTrap
{
    public int PitfallPlayerId;
    public Vector2 Location;
    public float Timer;
    public List<int> PlayersTrapped;
    public bool IsActive
    {
        get
        {
            return Timer <= Pitfall.TrapDuration && PlayersTrapped.Count < Pitfall.TrapMaxPlayerCount;
        }
    }
}
