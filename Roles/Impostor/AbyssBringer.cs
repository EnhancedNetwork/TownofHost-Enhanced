using AmongUs.GameOptions;
using Hazel;
using System;
using TOHE.Modules;
using UnityEngine;
using static TOHE.Modules.HazelExtensions;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

//EHR - https://github.com/Gurge44/EndlessHostRoles/blob/main/Roles/Impostor/Abyssbringer.cs
internal class AbyssBringer : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Abyssbringer;
    const int Id = 31300;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\
    
    private static OptionItem BlackHoleCountLimit;
    private static OptionItem BlackHolePlaceCooldown;
    private static OptionItem BlackHoleDespawnMode;
    private static OptionItem BlackHoleDespawnTime;
    private static OptionItem BlackHoleMovesTowardsNearestPlayer;
    private static OptionItem BlackHoleMoveSpeed;
    private static OptionItem BlackHoleRadius;
    private static OptionItem CanKillImpostors;
    private static OptionItem CanKillTNA;

    private readonly Dictionary<byte, BlackHoleData> BlackHoles = [];

    public override void SetupCustomOption()
    {
        const TabGroup tab = TabGroup.ImpostorRoles;
        const CustomRoles role = CustomRoles.Abyssbringer;
        SetupRoleOptions(Id, tab, role);
        BlackHoleCountLimit = IntegerOptionItem.Create(Id + 16, "BlackHoleCountLimit", new(1, 15, 1), 1, tab, false)
            .SetParent(CustomRoleSpawnChances[role]);
        BlackHolePlaceCooldown = IntegerOptionItem.Create(Id + 10, "BlackHolePlaceCooldown", new(1, 180, 1), 30, tab, false)
            .SetParent(CustomRoleSpawnChances[role])
            .SetValueFormat(OptionFormat.Seconds);
        BlackHoleDespawnMode = StringOptionItem.Create(Id + 11, "BlackHoleDespawnMode", Enum.GetNames<DespawnMode>(), 0, tab, false)
            .SetParent(CustomRoleSpawnChances[role]);
        BlackHoleDespawnTime = IntegerOptionItem.Create(Id + 12, "BlackHoleDespawnTime", new(1, 60, 1), 15, tab, false)
            .SetParent(BlackHoleDespawnMode)
            .SetValueFormat(OptionFormat.Seconds);
        BlackHoleMovesTowardsNearestPlayer = BooleanOptionItem.Create(Id + 13, "BlackHoleMovesTowardsNearestPlayer", true, tab, false)
            .SetParent(CustomRoleSpawnChances[role]);
        BlackHoleMoveSpeed = FloatOptionItem.Create(Id + 14, "BlackHoleMoveSpeed", new(0.25f, 10f, 0.25f), 1f, tab, false)
            .SetParent(BlackHoleMovesTowardsNearestPlayer);
        BlackHoleRadius = FloatOptionItem.Create(Id + 15, "BlackHoleRadius", new(0.1f, 5f, 0.1f), 1.2f, tab, false)
            .SetParent(CustomRoleSpawnChances[role])
            .SetValueFormat(OptionFormat.Multiplier);
        CanKillImpostors = BooleanOptionItem.Create(Id + 19, "CanKillImpostors", false, tab, false).SetParent(CustomRoleSpawnChances[role]);
        CanKillTNA = BooleanOptionItem.Create(Id + 20, "CanKillTNA", false, tab, false).SetParent(CustomRoleSpawnChances[role]);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = BlackHolePlaceCooldown.GetInt();
        AURoleOptions.ShapeshifterDuration = 1f;
    }

    public override void Init()
    {
        lastBlackHoleId = 0;

        if (BlackHoles.Count > 0)
        {
            foreach (var blackHole in BlackHoles)
            {
                if (blackHole.Value.NetObject != null && AmongUsClient.Instance.AmHost)
                    blackHole.Value.NetObject.Despawn();
            }
            BlackHoles.Clear();
        }
    }

    private byte lastBlackHoleId = 0;

    private byte GetNextBlackHoleId()
    {
        for (byte i = 0; i < byte.MaxValue; i++)
        {
            lastBlackHoleId++;
            if (lastBlackHoleId == byte.MaxValue)
            {
                lastBlackHoleId = 0;
            }
            if (!BlackHoles.ContainsKey(lastBlackHoleId))
            {
                return lastBlackHoleId;
            }
        }
        throw new InvalidOperationException("No available BlackHole ID.");
    }

    public override void UnShapeShiftButton(PlayerControl shapeshifter)
    {
        if (!Main.AllAlivePlayerControls.Where(x => x.PlayerId != shapeshifter.PlayerId).Any())
        {
            return;
        }
        // When no player exists, Instantly spawm and despawn networked object will cause error spam

        if (BlackHoles.Count >= BlackHoleCountLimit.GetInt())
        {
            return;
        }

        var pos = shapeshifter.GetCustomPosition();
        var room = shapeshifter.GetPlainShipRoom();
        var roomName = room == null ? string.Empty : Translator.GetString($"{room.RoomId}");
        var blackHoleId = GetNextBlackHoleId();
        BlackHoles.Add(blackHoleId, new(new(pos, _state.PlayerId), Utils.TimeStamp, pos, roomName, 0));
        Utils.SendRPC(CustomRPC.SyncRoleSkill, _Player, 1, blackHoleId, pos, roomName);
    }
    public override void SetAbilityButtonText(HudManager hud, byte id) => hud.AbilityButton.OverrideText(Translator.GetString("AbyssbringerButtonText"));
    public override void OnFixedUpdate(PlayerControl pc, bool lowLoad, long nowTime, int timerLowLoad)
    {
        var abyssbringer = _Player;

        foreach (var item in BlackHoles)
        {
            var blackHole = item.Value;
            var id = item.Key;

            var despawnMode = (DespawnMode)BlackHoleDespawnMode.GetValue();
            switch (despawnMode)
            {
                case DespawnMode.AfterTime when Utils.TimeStamp - blackHole.PlaceTimeStamp > BlackHoleDespawnTime.GetInt():
                case DespawnMode.AfterTime when Utils.TimeStamp - blackHole.PlaceTimeStamp > BlackHoleDespawnTime.GetInt():
                    RemoveBlackHole();
                    continue;
                case DespawnMode.AfterMeeting when Main.MeetingIsStarted:
                    RemoveBlackHole();
                    continue;
            }

            if (MeetingHud.Instance || Main.LastMeetingEnded + 2 > nowTime) continue;

            var nearestPlayer = Main.AllAlivePlayerControls.Where(x => x != pc).MinBy(x => Vector2.Distance(x.GetCustomPosition(), blackHole.Position));
            if (nearestPlayer != null)
            {
                var pos = nearestPlayer.GetCustomPosition();

                if (BlackHoleMovesTowardsNearestPlayer.GetBool() && GameStates.IsInTask && !ExileController.Instance)
                {
                    var direction = (pos - blackHole.Position).normalized;
                    var newPosition = blackHole.Position + direction * BlackHoleMoveSpeed.GetFloat() * Time.fixedDeltaTime;
                    blackHole.Position = newPosition;
                    blackHole.NetObject.Position = newPosition;
                }

                if (Vector2.Distance(pos, blackHole.Position) <= BlackHoleRadius.GetFloat())
                {
                    if (nearestPlayer.Is(Custom_Team.Impostor) && !CanKillImpostors.GetBool() || nearestPlayer.IsTransformedNeutralApocalypse() && !CanKillTNA.GetBool()) continue;
                    blackHole.PlayersConsumed++;
                    Utils.SendRPC(CustomRPC.SyncRoleSkill, _Player, 2, id, (byte)blackHole.PlayersConsumed);
                    Notify();

                    nearestPlayer.RpcExileV2();
                    nearestPlayer.SetRealKiller(_Player);
                    nearestPlayer.SetDeathReason(PlayerState.DeathReason.Consumed);
                    Main.PlayerStates[nearestPlayer.PlayerId].SetDead();
                    MurderPlayerPatch.AfterPlayerDeathTasks(_Player, nearestPlayer, inMeeting: false, fromRole: true);

                    if (despawnMode == DespawnMode.After1PlayerEaten)
                    {
                        RemoveBlackHole();
                    }
                }
            }
            else
            {
                // No players to follow, despawn
                RemoveBlackHole();
                Notify();
            }

            continue;

            void RemoveBlackHole()
            {
                BlackHoles.Remove(id);
                blackHole.NetObject.Despawn();
                Utils.SendRPC(CustomRPC.SyncRoleSkill, _Player, 3, id);
                Notify();
            }

            void Notify() => Utils.NotifyRoles(SpecifySeer: abyssbringer, SpecifyTarget: abyssbringer);
        }
    }

    public override void AfterMeetingTasks()
    {
        var abyssbringer = _Player;
        int count = BlackHoles.Count;
        foreach (var item in BlackHoles)
        {
            var blackHole = item.Value;
            var despawnMode = (DespawnMode)BlackHoleDespawnMode.GetValue();

            if (despawnMode == DespawnMode.AfterMeeting)
            {
                RemoveBlackHole();
            }

            continue;

            void RemoveBlackHole()
            {
                BlackHoles.Remove(item.Key);
                blackHole.NetObject.Despawn();
                Utils.SendRPC(CustomRPC.SyncRoleSkill, _Player, 3, item.Key);
                Notify();
            }

            void Notify() => Utils.NotifyRoles(SpecifySeer: abyssbringer, SpecifyTarget: abyssbringer);
        }
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        switch (reader.ReadPackedInt32())
        {
            case 1:
                var id = reader.ReadByte();
                var pos = reader.ReadVector2();
                var roomName = reader.ReadString();
                if (BlackHoles.ContainsKey(id))
                {
                    BlackHoles.Remove(id);
                }
                BlackHoles.Add(id, new(new(pos, _state.PlayerId), Utils.TimeStamp, pos, roomName, 0));
                break;
            case 2:
                var key = reader.ReadByte();
                if (!BlackHoles.ContainsKey(key)) return;

                BlackHoles[key].PlayersConsumed = reader.ReadByte();
                break;
            case 3:
                BlackHoles.Remove(reader.ReadByte());
                break;
        }
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl target = null, bool isMeeting = false, bool isForHud = false)
    {
        if (seer.PlayerId != target.PlayerId || seer.PlayerId != _state.PlayerId || (seer.IsModded() && !isForHud) || isMeeting || BlackHoles.Count == 0) return string.Empty;
        return string.Format(Translator.GetString("Abyssbringer.Suffix"), BlackHoles.Count, string.Join('\n', BlackHoles.Select(x => GetBlackHoleFormatText(x.Value.RoomName, x.Value.PlayersConsumed))));

        static string GetBlackHoleFormatText(string roomName, int playersConsumed)
        {
            var rn = roomName == string.Empty ? Translator.GetString("Outside") : roomName;
            return string.Format(Translator.GetString("Abyssbringer.Suffix.BlackHole"), rn, playersConsumed);
        }
    }

    [Obfuscation(Exclude = true)]
    enum DespawnMode
    {
        None,
        AfterTime,
        After1PlayerEaten,
        AfterMeeting
    }

    class BlackHoleData(BlackHole NetObject, long PlaceTimeStamp, Vector2 Position, string RoomName, int PlayersConsumed)
    {
        public BlackHole NetObject { get; } = NetObject;
        public long PlaceTimeStamp { get; } = PlaceTimeStamp;
        public Vector2 Position { get; set; } = Position;
        public string RoomName { get; } = RoomName;
        public int PlayersConsumed { get; set; } = PlayersConsumed;
    }
}
