using AmongUs.GameOptions;
using Hazel;
using Il2CppSystem.Text;
using System.Collections.Generic;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

public static class EvilTracker
{
    private static readonly int Id = 1400;
    private static List<byte> playerIdList = [];
    public static bool IsEnable = false;

    private static OptionItem OptionCanSeeKillFlash;
    private static OptionItem OptionTargetMode;
    private static OptionItem OptionCanSeeLastRoomInMeeting;

    private static bool CanSeeKillFlash;
    private static TargetMode CurrentTargetMode;
    public static RoleTypes RoleTypes;
    public static bool CanSeeLastRoomInMeeting;

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

    public static Dictionary<byte, byte> Target = [];
    public static Dictionary<byte, bool> CanSetTarget = [];
    private static Dictionary<byte, HashSet<byte>> ImpostorsId = [];
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
    public static void Init()
    {
        playerIdList = [];
        Target = [];
        CanSetTarget = [];
        ImpostorsId = [];
        IsEnable = false;

        CanSeeKillFlash = OptionCanSeeKillFlash.GetBool();
        CurrentTargetMode = (TargetMode)OptionTargetMode.GetValue();
        RoleTypes = CurrentTargetMode == TargetMode.Never ? RoleTypes.Impostor : RoleTypes.Shapeshifter;
        CanSeeLastRoomInMeeting = OptionCanSeeLastRoomInMeeting.GetBool();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;
        Target.Add(playerId, byte.MaxValue);
        CanSetTarget.Add(playerId, CurrentTargetMode != TargetMode.Never);
        //ImpostorsIdはEvilTracker内で共有
        ImpostorsId[playerId] = [];
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
    public static void ApplyGameOptions(byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = CanTarget(playerId) ? 1f : 255f;
        AURoleOptions.ShapeshifterDuration = 1f;
    }
    public static void GetAbilityButtonText(HudManager __instance, byte playerId)
    {
        __instance.AbilityButton.ToggleVisible(CanTarget(playerId));
        __instance.AbilityButton.OverrideText(GetString("EvilTrackerChangeButtonText"));
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

    public static void OnShapeshift(PlayerControl shapeshifter, PlayerControl target, bool shapeshifting)
    {
        if (!CanTarget(shapeshifter.PlayerId) || !shapeshifting) return;
        if (target == null || target.Is(CustomRoleTypes.Impostor)) return;

        SetTarget(shapeshifter.PlayerId, target.PlayerId);
        Logger.Info($"{shapeshifter.GetNameWithRole()} target to {target.GetNameWithRole()}", "EvilTrackerTarget");
        shapeshifter.SyncSettings();
        Utils.NotifyRoles(SpecifySeer: shapeshifter, SpecifyTarget: target, ForceLoop: true);
    }
    public static void AfterMeetingTasks()
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
    public static void SetTarget(byte trackerId = byte.MaxValue, byte targetId = byte.MaxValue)
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

    public static string GetMarker(byte playerId) => CanTarget(playerId) ? Utils.ColorString(Palette.ImpostorRed.ShadeColor(0.5f), "◁") : "";
    public static string GetTargetMark(PlayerControl seer, PlayerControl target) => GetTargetId(seer.PlayerId) == target.PlayerId ? Utils.ColorString(Palette.ImpostorRed, "◀") : "";
    public static string GetTargetArrow(PlayerControl seer, PlayerControl target)
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
    public static string GetArrowAndLastRoom(PlayerControl seer, PlayerControl target)
    {
        string text = Utils.ColorString(Palette.ImpostorRed, TargetArrow.GetArrows(seer, target.PlayerId));
        var room = Main.PlayerStates[target.PlayerId].LastRoom;
        if (room == null) text += Utils.ColorString(Color.gray, "@" + GetString("FailToTrack"));
        else text += Utils.ColorString(Palette.ImpostorRed, "@" + GetString(room.RoomId.ToString()));
        return text;
    }
}