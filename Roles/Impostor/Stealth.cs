using Hazel;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using UnityEngine;

namespace TOHE.Roles.Impostor;

// https://github.com/tukasa0001/TownOfHost/blob/main/Roles/Impostor/Stealth.cs
internal class Stealth : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Stealth;
    private const int Id = 27400;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Stealth);
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorHindering;
    //==================================================================\\

    private static OptionItem optionExcludeImpostors;
    private static OptionItem optionDarkenDuration;

    private static bool excludeImpostors;
    private static float darkenDuration;
    private float darkenTimer;
    private PlayerControl[] darkenedPlayers;
    private SystemTypes? darkenedRoom;

    public override void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Stealth, 1);
        optionExcludeImpostors = BooleanOptionItem.Create(Id + 10, "StealthExcludeImpostors", false, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Stealth]);
        optionDarkenDuration = FloatOptionItem.Create(Id + 20, "StealthDarkenDuration", new(0.5f, 10f, 0.5f), 3f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Stealth])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add(byte playerId)
    {
        excludeImpostors = optionExcludeImpostors.GetBool();
        darkenDuration = darkenTimer = optionDarkenDuration.GetFloat();
        darkenedPlayers = null;
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
    private PlayerControl[] FindPlayersInSameRoom(PlayerControl killedPlayer)
    {
        var room = killedPlayer.GetPlainShipRoom();
        if (room == null)
        {
            return null;
        }
        var roomArea = room.roomArea;
        var roomName = room.RoomId;
        RpcDarken(roomName);
        return Main.AllAlivePlayerControls.Where(player => player != _Player && player.Collider.IsTouching(roomArea)).ToArray();
    }
    /// <summary>Give the given player zero visibility for <see cref="darkenDuration"/> seconds.</summary>
    private void DarkenPlayers(PlayerControl[] playersToDarken)
    {
        darkenedPlayers = [.. playersToDarken];
        foreach (PlayerControl player in playersToDarken)
        {
            Main.PlayerStates[player.PlayerId].IsBlackOut = true;
            player.MarkDirtySettings();
        }
    }
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
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
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            ResetDarkenState();
        }
    }
    private void RpcDarken(SystemTypes? roomType)
    {
        Logger.Info($"Set the darkened room to {roomType?.ToString() ?? "null"}", "Stealth");
        darkenedRoom = roomType;
        SendRPC(roomType);
    }
    private void SendRPC(SystemTypes? roomType)
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write((byte?)roomType ?? byte.MaxValue);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        var roomId = reader.ReadByte();
        darkenedRoom = roomId == byte.MaxValue ? null : (SystemTypes)roomId;
    }
    /// <summary>Removes the darkening effect that has occurred.</summary>
    private void ResetDarkenState()
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
        Utils.NotifyRoles(SpecifySeer: _Player, SpecifyTarget: _Player);
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        var Player = _Player;

        // During the meeting, unless it's my suffix or it's dark everywhere, I won't show anything.
        if (!HasEnabled || isForMeeting || seer != Player || seen != Player || !darkenedRoom.HasValue)
        {
            return string.Empty;
        }
        return string.Format(Translator.GetString("StealthDarkened"), DestroyableSingleton<TranslationController>.Instance.GetString(darkenedRoom.Value));
    }
}
