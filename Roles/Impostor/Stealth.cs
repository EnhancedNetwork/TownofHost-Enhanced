using Hazel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TOHE.Roles.Impostor;

// https://github.com/tukasa0001/TownOfHost/blob/main/Roles/Impostor/Stealth.cs
internal class Stealth : RoleBase
{
    private static readonly int Id = 27400;
    private static List<byte> playerIdList = [];
    public static bool On;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    private static OptionItem optionExcludeImpostors;
    private static OptionItem optionDarkenDuration;

    private static bool excludeImpostors;
    private static float darkenDuration;
    private static float darkenTimer;
    private static PlayerControl[] darkenedPlayers;
    private static SystemTypes? darkenedRoom;

    public static void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Stealth, 1);
        optionExcludeImpostors = BooleanOptionItem.Create(Id + 10, "StealthExcludeImpostors", false, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Stealth]);
        optionDarkenDuration = FloatOptionItem.Create(Id + 20, "StealthDarkenDuration", new(0.5f, 10f, 0.5f), 3f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Stealth])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Init()
    {
        On = false;
        playerIdList = [];
    }
    public override void Add(byte playerId)
    {
        excludeImpostors = optionExcludeImpostors.GetBool();
        darkenDuration = darkenTimer = optionDarkenDuration.GetFloat();
        darkenedPlayers = null;

        playerIdList.Add(playerId);
        On = true;
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        var playersToDarken = FindPlayersInSameRoom(target);
        if (playersToDarken == null)
        {
            Logger.Info("The room will not dim because the hit detection for the room cannot be obtained.", "Stealth");
            return true;
        }
        if (excludeImpostors)
        {
            playersToDarken = playersToDarken.Where(player => !player.Is(CustomRoles.Impostor)).ToArray();
        }
        DarkenPlayers(playersToDarken);
        
        return true;
    }
    /// <summary>Get all players in the same room as you</summary>
    private static PlayerControl[] FindPlayersInSameRoom(PlayerControl killedPlayer)
    {
        var room = killedPlayer.GetPlainShipRoom();
        if (room == null)
        {
            return null;
        }
        var roomArea = room.roomArea;
        var roomName = room.RoomId;
        RpcDarken(roomName);
        return Main.AllAlivePlayerControls.Where(player => player != Utils.GetPlayerById(playerIdList[0]) && player.Collider.IsTouching(roomArea)).ToArray();
    }
    /// <summary>Give the given player zero visibility for <see cref="darkenDuration"/> seconds.</summary>
    private static void DarkenPlayers(PlayerControl[] playersToDarken)
    {
        darkenedPlayers = [.. playersToDarken];
        foreach (PlayerControl player in playersToDarken)
        {
            Main.PlayerStates[player.PlayerId].IsBlackOut = true;
            player.MarkDirtySettings();
        }
    }
    public override void OnFixedUpdateLowLoad(PlayerControl player)
    {
        // when you're darkening someone
        if (darkenedPlayers == null) return;

        // reduce timer
        darkenTimer -= Time.fixedDeltaTime;
        // When the timer reaches 0, return everyone's vision and reset the timer and darkening player.
        if (darkenTimer <= 0)
        {
            ResetDarkenState();
        }
    }
    public override void OnReportDeadBody(PlayerControl reporter, PlayerControl target)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            ResetDarkenState();
        }
    }
    private static void RpcDarken(SystemTypes? roomType)
    {
        Logger.Info($"Set the darkened room to {roomType?.ToString() ?? "null"}", "Stealth");
        darkenedRoom = roomType;
        SendRPC(roomType);
    }
    private static void SendRPC(SystemTypes? roomType)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Stealth);
        writer.Write((byte?)roomType ?? byte.MaxValue);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        var roomId = reader.ReadByte();
        darkenedRoom = roomId == byte.MaxValue ? null : (SystemTypes)roomId;
    }
    /// <summary>Removes the darkening effect that has occurred.</summary>
    private static void ResetDarkenState()
    {
        if (darkenedPlayers != null)
        {
            foreach (PlayerControl player in darkenedPlayers)
            {
                Main.PlayerStates[player.PlayerId].IsBlackOut = false;
                player.MarkDirtySettings();
            }
            darkenedPlayers = null;
        }
        darkenTimer = darkenDuration;
        RpcDarken(null);
        Utils.NotifyRoles(SpecifySeer: Utils.GetPlayerById(playerIdList[0]), SpecifyTarget: Utils.GetPlayerById(playerIdList[0]));
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        var Player = Utils.GetPlayerById(playerIdList[0]);

        // During the meeting, unless it's my suffix or it's dark everywhere, I won't show anything.
        if (!On || isForMeeting || seer != Player || seen != Player || !darkenedRoom.HasValue)
        {
            return base.GetSuffix(seer, seen, isForMeeting);
        }
        return string.Format(Translator.GetString("StealthDarkened"), DestroyableSingleton<TranslationController>.Instance.GetString(darkenedRoom.Value));
    }
}
