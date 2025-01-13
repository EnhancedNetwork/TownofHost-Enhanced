using AmongUs.GameOptions;
using TOHE.Roles.Core;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Bane : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 32700;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();

    public override CustomRoles Role => CustomRoles.Bane;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorHindering;
    //==================================================================\\

    private static OptionItem ShapeshiftCooldown;
    private static OptionItem MaxTrapCount;
    private static OptionItem TrapMaxPlayerCountOpt;
    private static OptionItem TrapDurationOpt;
    private static OptionItem TrapRadius;

    private static HashSet<BaneTrap> Traps = [];
    private static readonly HashSet<byte> ReducedVisionPlayers = [];

    private static float DefaultSpeed = new();
    public static float TrapMaxPlayerCount = new();
    public static float TrapDuration = new();

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Bane);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 10, "BaneTrapCooldown", new(1f, 180f, 1f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bane])
            .SetValueFormat(OptionFormat.Seconds);
        MaxTrapCount = FloatOptionItem.Create(Id + 11, "BaneMaxTrapCount", new(1f, 5f, 1f), 1f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bane])
            .SetValueFormat(OptionFormat.Times);
        TrapMaxPlayerCountOpt = FloatOptionItem.Create(Id + 12, "BaneTrapMaxPlayerCount", new(1f, 15f, 1f), 3f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bane])
            .SetValueFormat(OptionFormat.Times);
        TrapDurationOpt = FloatOptionItem.Create(Id + 13, "BaneTrapDuration", new(5f, 180f, 1f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bane])
            .SetValueFormat(OptionFormat.Seconds);
        TrapRadius = FloatOptionItem.Create(Id + 14, "BaneTrapRadius", new(0.5f, 3f, 0.5f), 1f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bane])
            .SetValueFormat(OptionFormat.Multiplier);
    }
    public override void Init()
    {
        playerIdList.Clear();
        Traps.Clear();
        ReducedVisionPlayers.Clear();
        DefaultSpeed = new();
        TrapMaxPlayerCount = new();
        TrapDuration = new();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        DefaultSpeed = Main.AllPlayerSpeed[playerId];

        TrapMaxPlayerCount = TrapMaxPlayerCountOpt.GetFloat();
        TrapDuration = TrapDurationOpt.GetFloat();

        if (AmongUsClient.Instance.AmHost)
        {
            CustomRoleManager.OnFixedUpdateLowLoadOthers.Add(OnFixedUpdateOthers);
        }
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }

    public override void AfterMeetingTasks()
    {
        Traps.Clear();
    }

    public override bool OnCheckShapeshift(PlayerControl shapeshifter, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate)
    {
        if (shapeshifter.PlayerId == target.PlayerId) return false;

        // Remove inactive traps so there is room for new traps
        Traps = Traps.Where(a => a.IsActive).ToHashSet();

        Vector2 position = shapeshifter.transform.position;
        var playerTraps = Traps.Where(a => a.BanePlayerId == shapeshifter.PlayerId).ToArray();
        if (playerTraps.Length >= MaxTrapCount.GetInt())
        {
            var trap = playerTraps.First();
            trap.Location = position;
            trap.PlayersTrapped = [];
            trap.Timer = 0;
        }
        else
        {
            Traps.Add(new BaneTrap
            {
                BanePlayerId = shapeshifter.PlayerId,
                Location = position,
                PlayersTrapped = [],
                Timer = 0
            });
        }

        shapeshifter.Notify(GetString("RejectShapeshift.AbilityWasUsed"), time: 2f);

        return false;
    }

    private void OnFixedUpdateOthers(PlayerControl player)
    {
        if (Pelican.IsEaten(player.PlayerId) || !player.IsAlive()) return;

        if (player.GetCustomRole().IsImpostor())
        {
            var traps = Traps.Where(a => a.BanePlayerId == player.PlayerId && a.IsActive).ToArray();
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

            var dis = Vector2.Distance(trap.Location, position);
            if (dis > TrapRadius.GetFloat()) continue;

            player.RpcMurderPlayer(player);
            player.SetDeathReason(PlayerState.DeathReason.Toxined);

            player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Bane), GetString("BaneTrap")));
        }
    }
}

public class BaneTrap
{
    public int BanePlayerId;
    public Vector2 Location;
    public float Timer;
    public List<int> PlayersTrapped;
    public bool IsActive
    {
        get
        {
            return Timer <= Bane.TrapDuration && PlayersTrapped.Count < Bane.TrapMaxPlayerCount;
        }
    }
}
