using AmongUs.GameOptions;
using TOHE.Modules;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Coven;

internal class Sorceress : CovenManager
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Sorceress;
    private const int Id = 32600;
    public override bool IsDesyncRole => true;
    public override bool IsExperimental => true;
    public override bool UsesCNOs => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenTrickery;
    //==================================================================\\
    private static OptionItem MirageCooldown;
    private static OptionItem MirageVision;
    private static OptionItem MirageDuration;
    private static OptionItem MirageCount;
    private static OptionItem MirageRadius;
    private static OptionItem MirageSpeed;

    private static readonly Dictionary<byte, DeathMirageData> Mirages = [];

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, Role, 1, zeroOne: false);
        MirageCooldown = FloatOptionItem.Create(Id + 10, "Sorceress.MirageCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[Role])
            .SetValueFormat(OptionFormat.Seconds);
        MirageVision = FloatOptionItem.Create(Id + 11, "Sorceress.MirageVision", new(0f, 5f, 0.25f), 0.5f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[Role])
            .SetValueFormat(OptionFormat.Multiplier);
        MirageDuration = FloatOptionItem.Create(Id + 12, "Sorceress.MirageDuration", new(2.5f, 180f, 2.5f), 30f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[Role])
            .SetValueFormat(OptionFormat.Seconds);
        MirageCount = IntegerOptionItem.Create(Id + 13, "Sorceress.MirageCount", new(1, 10, 1), 3, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[Role]);
        MirageRadius = FloatOptionItem.Create(Id + 14, "Sorceress.MirageRadius", new(0.5f, 10f, 0.1f), 1f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[Role]);
        MirageSpeed = FloatOptionItem.Create(Id + 15, "Sorceress.MirageSpeed", new(0.05f, 10f, 0.05f), 0.2f, TabGroup.CovenRoles, false)
            .SetParent(CustomRoleSpawnChances[Role]);
    }
    public override void Init()
    {
        Mirages.Clear();
    }
    public override void Add(byte playerId)
    {
        
    }
    public override bool CanUseKillButton(PlayerControl pc) => false;
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = MirageCooldown.GetFloat();
    }
    public override bool OnCheckShapeshift(PlayerControl shapeshifter, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate)
    {
        resetCooldown = true;
        var shapeshifterId = shapeshifter.PlayerId;
        if (target != null && shapeshifterId == target.PlayerId) return false;

        if (Mirages.TryGetValue(shapeshifterId, out var mirages))
        {
            RemoveMirages();
        }

        CreateMirages(target);

        return false;
    }

    public static void SetBlinded(PlayerControl player, IGameOptions opt)
    {
        if (Mirages.Any(a => a.Value.TargetId == player.PlayerId && 
            Main.AllAlivePlayerControls.Any(b => b.PlayerId == a.Key)))
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, MirageVision.GetFloat());
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, MirageVision.GetFloat());
        }
    }

    private void CreateMirages(PlayerControl target)
    {
        target.MarkDirtySettings();
        Vector2 targetPos = target.GetCustomPosition();
        var positions = GetMiragePositions(targetPos);

        List<byte> visList = [target.PlayerId, _Player.PlayerId]; // Only Sorceress and target can see mirages

        List<DeathMirage> mirages = [];
        foreach (var pos in positions)
        {
            mirages.Add(new(pos, visList));
        }

        Mirages.Add(_Player.PlayerId, new(mirages, TimeStamp, target.PlayerId));
        SendCreateMiragesRPC();
    }

    private static List<Vector2> GetMiragePositions(Vector2 center)
    {
        var count = MirageCount.GetInt();
        var radius = MirageRadius.GetFloat();
        List<Vector2> points = new(count);

        float offset = IRandom.Instance.Next(0, 1000) / 1000 * Mathf.PI * 2f;

        for (int i = 0; i < count; i++)
        {
            // Convert index to angle in radians
            float angle = offset + (i / (float)count) * Mathf.PI * 2f;

            // Compute point position
            float x = center.x + Mathf.Cos(angle) * radius;
            float y = center.y + Mathf.Sin(angle) * radius;

            points.Add(new Vector2(x, y));
        }

        return points;
    }

    private void RemoveMirages()
    {
        var id = _Player.PlayerId;
        if (Mirages.TryGetValue(id, out var mirages))
        {
            mirages.TargetId.GetPlayer()?.MarkDirtySettings();
            foreach (var mirage in mirages.NetObjects)
            {
                mirage.Despawn();
            }
            Mirages.Remove(id);
            SendRemoveMiragesRPC();
        }
    }

    private void SendRemoveMiragesRPC()
    {
        
    }
    private void SendCreateMiragesRPC()
    {
        
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        var sorc = _Player;
        var id = sorc.PlayerId;

        if (!Mirages.TryGetValue(id, out var mirages)) return;

        if (TimeStamp - mirages.PlaceTimeStamp > MirageDuration.GetInt() && !HasNecronomicon(id))
        {
            RemoveMirages();
            return;
        }

        if (MeetingHud.Instance || Main.LastMeetingEnded + 2 > nowTime) return;

        var target = mirages.TargetId.GetPlayer();

        if (target == null) return;

        var targetPos = target.GetCustomPosition();

        foreach (var mirage in mirages.NetObjects)
        {
            if (GameStates.IsInTask && !ExileController.Instance)
            {
                var direction = (targetPos - mirage.Position).normalized;
                var newPosition = mirage.Position + direction * MirageSpeed.GetFloat() * Time.fixedDeltaTime;
                mirage.TP(newPosition);
            }

            if (!target.IsAlive()) continue;

            var KillRange = ExtendedPlayerControl.GetKillDistances();

            if (Vector2.Distance(targetPos, mirage.Position) <= KillRange)
            {
                RPC.PlaySoundRPC(Sounds.KillSound, id);
                target.RpcMurderPlayer(target);
                target.SetRealKiller(_Player);
                target.SetDeathReason(PlayerState.DeathReason.Drained);
                Main.PlayerStates[target.PlayerId].SetDead();
                MurderPlayerPatch.AfterPlayerDeathTasks(_Player, target, inMeeting: false, fromRole: true);
                mirage.TP(target.GetCustomPosition());
                
                _ = new LateTask(RemoveMirages, 0.5f, "Remove Death Mirages");
            }
        }
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(GetString("Sorceress.MirageButtonText"));
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        // Remove and Despawn Mirages on Meeting
        RemoveMirages();
    }

    class DeathMirageData(List<DeathMirage> NetObjects, long PlaceTimeStamp, byte TargetId)
    {
        public List<DeathMirage> NetObjects { get; } = NetObjects;
        public long PlaceTimeStamp { get; } = PlaceTimeStamp;
        // public List<Vector2> Position { get; set; } = Positions;
        public byte TargetId { get; set; } = TargetId;
    }

    internal sealed class DeathMirage : CustomNetObject
    {
        internal DeathMirage(Vector2 position, List<byte> visibleList)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            CreateNetObject(position: position, visible: true, pOutfit: Main.AllAlivePlayerControls.RandomElement()?.Data.Outfits[PlayerOutfitType.Default]);
            Main.AllAlivePlayerControls.ExceptBy(visibleList, x => x.PlayerId).Do(Hide);
        }
    }
}
