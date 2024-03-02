using AmongUs.GameOptions;
using Hazel;
using Il2CppSystem.Text;
using System.Collections.Generic;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class EvilTracker : RoleBase
{
    private static readonly int Id = 1400;
    private static List<byte> playerIdList = [];
    
    public static bool On;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => (TargetMode)OptionTargetMode.GetValue() == TargetMode.Never ? CustomRoles.Impostor : CustomRoles.Shapeshifter;

    private static OptionItem OptionCanSeeKillFlash;
    private static OptionItem OptionTargetMode;
    private static OptionItem OptionCanSeeLastRoomInMeeting;

    private static bool CanSeeKillFlash;
    private static TargetMode CurrentTargetMode;
    private static bool CanSeeLastRoomInMeeting;

    private static Dictionary<byte, byte> Target = [];
    private static Dictionary<byte, bool> CanSetTarget = [];
    private static Dictionary<byte, HashSet<byte>> ImpostorsId = [];

    private enum TargetMode
    {
        Never,
        OnceInGame,
        EveryMeeting,
        Always,
    };
    private static readonly string[] TargetModeText =
    [
        "EvilTrackerTargetMode.Never",
        "EvilTrackerTargetMode.OnceInGame",
        "EvilTrackerTargetMode.EveryMeeting",
        "EvilTrackerTargetMode.Always",
    ];

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.EvilTracker);
        OptionCanSeeKillFlash = BooleanOptionItem.Create(Id + 10, "EvilTrackerCanSeeKillFlash", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.EvilTracker]);
        OptionTargetMode = StringOptionItem.Create(Id + 11, "EvilTrackerTargetMode", TargetModeText, 2, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.EvilTracker]);
        OptionCanSeeLastRoomInMeeting = BooleanOptionItem.Create(Id + 12, "EvilTrackerCanSeeLastRoomInMeeting", false, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.EvilTracker]);
    }
    public override void Init()
    {
        playerIdList = [];
        Target = [];
        CanSetTarget = [];
        ImpostorsId = [];
        On = false;

        CanSeeKillFlash = OptionCanSeeKillFlash.GetBool();
        CurrentTargetMode = (TargetMode)OptionTargetMode.GetValue();
        CanSeeLastRoomInMeeting = OptionCanSeeLastRoomInMeeting.GetBool();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        Target.Add(playerId, byte.MaxValue);
        CanSetTarget.Add(playerId, CurrentTargetMode != TargetMode.Never);

        ImpostorsId[playerId] = [];
        On = true;

        foreach (var target in Main.AllAlivePlayerControls)
        {
            var targetId = target.PlayerId;
            if (targetId != playerId && target.Is(CustomRoleTypes.Impostor))
            {
                ImpostorsId[playerId].Add(targetId);
                TargetArrow.Add(playerId, targetId);
            }
        }
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = CanTarget(playerId) ? 1f : 255f;
        AURoleOptions.ShapeshifterDuration = 1f;
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.AbilityButton.ToggleVisible(CanTarget(id));
        hud.AbilityButton.OverrideText(GetString("EvilTrackerChangeButtonText"));
    }

    public static bool KillFlashCheck() => CanSeeKillFlash;

    private static bool CanTarget(byte playerId)
        => !Main.PlayerStates[playerId].IsDead && CanSetTarget.TryGetValue(playerId, out var value) && value;
    
    private static byte GetTargetId(byte playerId)
        => Target.TryGetValue(playerId, out var targetId) ? targetId : byte.MaxValue;
    
    public static bool IsTrackTarget(PlayerControl seer, PlayerControl target)
        => seer.IsAlive() && playerIdList.Contains(seer.PlayerId)
        && target.IsAlive() && seer != target
        && (target.Is(CustomRoleTypes.Impostor) || GetTargetId(seer.PlayerId) == target.PlayerId);

    public override void OnShapeshift(PlayerControl shapeshifter, PlayerControl target, bool shapeshifting, bool shapeshiftIsHidden = false)
    {
        if (!CanTarget(shapeshifter.PlayerId) || (!shapeshifting && !shapeshiftIsHidden)) return;
        if (target == null || target.Is(CustomRoleTypes.Impostor)) return;

        SetTarget(shapeshifter.PlayerId, target.PlayerId);

        if (shapeshiftIsHidden)
        {
            shapeshifter.Notify(GetString("RejectShapeshift.AbilityWasUsed"), time: 2f);
            shapeshifter.SyncSettings();
        }
        else
            shapeshifter.MarkDirtySettings();

        Logger.Info($"{shapeshifter.GetNameWithRole()} target to {target.GetNameWithRole()}", "EvilTrackerTarget");
        Utils.NotifyRoles(SpecifySeer: shapeshifter, SpecifyTarget: target, ForceLoop: true);
    }
    public override void AfterMeetingTasks()
    {
        if (CurrentTargetMode == TargetMode.EveryMeeting)
        {
            SetTarget();
            Utils.MarkEveryoneDirtySettings();
        }
        foreach (var playerId in playerIdList.ToArray())
        {
            var pc = Utils.GetPlayerById(playerId);
            var target = Utils.GetPlayerById(GetTargetId(playerId));
            if (!pc.IsAlive() || !target.IsAlive())
                SetTarget(playerId);
            pc?.SyncSettings();
            pc?.RpcResetAbilityCooldown();
        }
    }
    private static void SetTarget(byte trackerId = byte.MaxValue, byte targetId = byte.MaxValue)
    {
        if (trackerId == byte.MaxValue) // Targets can be re-set
            foreach (var playerId in playerIdList.ToArray())
                CanSetTarget[playerId] = true;
        else if (targetId == byte.MaxValue) // Target deletion
            Target[trackerId] = byte.MaxValue;
        else
        {
            Target[trackerId] = targetId; // Set Target
            if (CurrentTargetMode != TargetMode.Always)
                CanSetTarget[trackerId] = false; // Target cannot be re-set
            
            TargetArrow.Add(trackerId, targetId);
        }

        if (!AmongUsClient.Instance.AmHost) return;
        SendRPC(trackerId, targetId);
    }
    private static void SendRPC(byte trackerId, byte targetId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetEvilTrackerTarget, SendOption.Reliable, -1);
        writer.Write(trackerId);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte trackerId = reader.ReadByte();
        byte targetId = reader.ReadByte();
        SetTarget(trackerId, targetId);
    }

    public override string GetProgressText(byte PlayerId, bool comms)
        => CanTarget(PlayerId) ? Utils.ColorString(Palette.ImpostorRed.ShadeColor(0.5f), "◁") : string.Empty;

    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        return Target.ContainsValue(seen.PlayerId)
            ? Utils.ColorString(Palette.ImpostorRed.ShadeColor(0.5f), "◀") : string.Empty;
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;

        if (isForMeeting)
        {
            var roomName = GetArrowAndLastRoom(seer, seen);
            return roomName.Length == 0 ? string.Empty : $"<size=1.5>{roomName}</size>";
        }
        else
        {
            return GetTargetArrow(seer, seen);
        }
    }

    private static string GetTargetArrow(PlayerControl seer, PlayerControl target)
    {
        if (!GameStates.IsInTask || !target.Is(CustomRoles.EvilTracker)) return "";

        var trackerId = target.PlayerId;
        if (seer.PlayerId != trackerId) return "";

        ImpostorsId[trackerId].RemoveWhere(id => Main.PlayerStates[id].IsDead);

        var sb = new StringBuilder(80);
        if (ImpostorsId[trackerId].Count > 0)
        {
            sb.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Impostor)}>");
            foreach (var impostorId in ImpostorsId[trackerId])
            {
                sb.Append(TargetArrow.GetArrows(target, impostorId));
            }
            sb.Append($"</color>");
        }

        var targetId = Target[trackerId];
        if (targetId != byte.MaxValue)
        {
            sb.Append(Utils.ColorString(Color.white, TargetArrow.GetArrows(target, targetId)));
        }
        return sb.ToString();
    }
    private static string GetArrowAndLastRoom(PlayerControl seer, PlayerControl target)
    {
        string text = Utils.ColorString(Palette.ImpostorRed, TargetArrow.GetArrows(seer, target.PlayerId));
        var room = Main.PlayerStates[target.PlayerId].LastRoom;
        if (room == null) text += Utils.ColorString(Color.gray, "@" + GetString("FailToTrack"));
        else text += Utils.ColorString(Palette.ImpostorRed, "@" + GetString(room.RoomId.ToString()));
        return text;
    }
}