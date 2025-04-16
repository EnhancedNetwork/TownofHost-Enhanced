using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TOHE.Modules;
using UnityEngine;
using static NetworkedPlayerInfo;
using static TOHE.Options;
using static TOHE.RoleBase;
using static TOHE.Translator;

namespace TOHE;
internal static class UltimateTeam
{
    public static OptionItem GameTime;
    public static OptionItem ShowChatInGame;
    public static OptionItem PlayerLives;
    public static OptionItem PlayerKillCooldown;

    public static Dictionary<byte, int> Lives = [];
    public static List<byte> RedTeam = [];
    public static List<byte> BlueTeam = [];
    public static int RoundTime;

    public static void SetupCustomOption()
    {
        GameTime = IntegerOptionItem.Create(67_225_001, "GameTime", new(30, 600, 10), 300, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.UltimateTeam)
            .SetValueFormat(OptionFormat.Seconds)
            .SetHeader(true);
        ShowChatInGame = BooleanOptionItem.Create(67_225_02, "ShowChatInGame", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.UltimateTeam);
        PlayerLives = IntegerOptionItem.Create(67_225_03, "PlayerLives", new(1, 5, 1), 3, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.UltimateTeam)
            .SetValueFormat(OptionFormat.Multiplier);
        PlayerKillCooldown = FloatOptionItem.Create(67_225_04, GeneralOption.KillCooldown, new(5f, 45f, 2.5f), 15f, TabGroup.ModSettings, false)
            .SetGameMode (CustomGameMode.UltimateTeam) 
            .SetValueFormat(OptionFormat.Seconds);
    }

    public static void Init()
    {
        if (CurrentGameMode != CustomGameMode.UltimateTeam) return;

        Lives = [];
        RedTeam = [];
        BlueTeam = [];
    }

    private static void Add(byte playerId)
    {
        var player = Utils.GetPlayerById(playerId);
        var role = player.GetCustomRole();
        var playerOutfit = new NetworkedPlayerInfo.PlayerOutfit();

        switch (role)
        {
            case CustomRoles.Red:
                RedTeam.Add(playerId);
                Main.UnShapeShifter.Add(playerId);
                var redout = new PlayerOutfit()
                    .Set(
                    player.CurrentOutfit.PlayerName,
                    0, //red
                    player.CurrentOutfit.HatId,
                    player.CurrentOutfit.SkinId,
                    player.CurrentOutfit.VisorId,
                    player.CurrentOutfit.PetId,
                    player.CurrentOutfit.NamePlateId);

                player.SetNewOutfit(redout, newLevel: player.Data.PlayerLevel);
                Main.OvverideOutfit[player.PlayerId] = (redout, Main.PlayerStates[player.PlayerId].NormalOutfit.PlayerName);
                return;

            case CustomRoles.Blue:
                BlueTeam.Add(playerId);
                var bluout = new PlayerOutfit()
                    .Set(
                    player.CurrentOutfit.PlayerName,
                    1, //blue
                    player.CurrentOutfit.HatId,
                    player.CurrentOutfit.SkinId,
                    player.CurrentOutfit.VisorId,
                    player.CurrentOutfit.PetId,
                    player.CurrentOutfit.NamePlateId);

                player.SetNewOutfit(bluout, newLevel: player.Data.PlayerLevel);
                Main.OvverideOutfit[player.PlayerId] = (bluout, Main.PlayerStates[player.PlayerId].NormalOutfit.PlayerName);
                return;
        }
    }

    public static Dictionary<byte, CustomRoles> SetRoles()
    {
        Dictionary<byte, CustomRoles> finalRoles = [];
        var random = IRandom.Instance;
        List<PlayerControl> AllPlayers = Main.AllPlayerControls.Shuffle(random).ToList();

        if (Main.EnableGM.Value)
        {
            finalRoles[PlayerControl.LocalPlayer.PlayerId] = CustomRoles.GM;
            Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].MainRole = CustomRoles.GM;//might cause bugs
            AllPlayers.Remove(PlayerControl.LocalPlayer);
        }
        foreach (byte spectator in ChatCommands.Spectators)
        {
            finalRoles.AddRange(ChatCommands.Spectators.ToDictionary(x => x, _ => CustomRoles.GM));
            foreach (var specId in ChatCommands.Spectators)
            {
                Main.PlayerStates[specId].MainRole = CustomRoles.GM;
            }
            AllPlayers.RemoveAll(x => ChatCommands.Spectators.Contains(x.PlayerId));
        }

        int optImpNum = (int)(Main.AllAlivePlayerControls.Length * 0.5);
        foreach (PlayerControl pc in AllPlayers)
        {
            if (pc == null) continue;
            if (optImpNum > 0)
            {
                finalRoles[pc.PlayerId] = CustomRoles.Red;
                Main.PlayerStates[pc.PlayerId].MainRole = CustomRoles.Red;
                pc.RpcSetCustomRole(CustomRoles.Red);
                pc.RpcChangeRoleBasis(CustomRoles.Red);
                Add(pc.PlayerId);
                optImpNum--;
            }
            else
            {
                finalRoles[pc.PlayerId] = CustomRoles.Blue;
                Main.PlayerStates[pc.PlayerId].MainRole = CustomRoles.Blue;
                pc.RpcSetCustomRole(CustomRoles.Blue);
                pc.RpcChangeRoleBasis(CustomRoles.Blue);
                Add(pc.PlayerId);
            }
            Logger.Msg($"set role for {pc.PlayerId}: {finalRoles[pc.PlayerId]}", "SetRoles");
        }
        return finalRoles;
    }

    public static void SetData()
    {
        if (CurrentGameMode != CustomGameMode.UltimateTeam) return;

        RoundTime = GameTime.GetInt() + 8;
        var now = Utils.GetTimeStamp() + 8;

        foreach (var player in Main.AllAlivePlayerControls)
        {
            Lives[player.PlayerId] = PlayerLives.GetInt();
        }
    }

    public static string GetProgressText(byte playerId)
    {
        var player = Utils.GetPlayerById(playerId);
        string progressText = $"<color={player.GetRoleColorCode()}> ({Lives[playerId]})</color>";
        if (playerId == byte.MaxValue) return progressText;
        Color32 textColor = Color.white;

        return Utils.ColorString(textColor, progressText);
    }

    public static bool OnTag(PlayerControl killer, PlayerControl target)
    {
        if (killer.GetCustomRole() == target.GetCustomRole()) return false;
        foreach (var player in Main.AllPlayerControls)
        {
            if (player.IsAlive()) continue;
            if (player.GetRealKiller() == target && Lives[player.PlayerId] > 0)
            {
                player.RpcRevive();
                player.RpcChangeRoleBasis(CustomRoles.Impostor); 
                player.RpcSetCustomRole(player.GetCustomRole());
                player.SetRealKiller(null);
                player.RpcTeleport(killer.GetCustomPosition());
                player.ResetKillCooldown();
            }
        }
        Lives[target.PlayerId]--;
        if (Lives[target.PlayerId] == 0) return true;

        Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Kill;
        killer.RpcGuardAndKill(target);
        target.RpcGuardAndKill(killer);
        target.RpcExileV2();
        Main.PlayerStates[target.PlayerId].SetDead();
        target.Data.IsDead = true;
        target.SetRealKiller(killer);
        killer.SetKillCooldown();
        return false;
    }

    public static string GetHudText()
    {
        return string.Format(GetString("GameModeTimeRemain"), RoundTime.ToString());
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    class FixedUpdateInGameModeUltimatePatch
    {
        private static long LastFixedUpdate;
        public static void Postfix()
        {
            if (!GameStates.IsInTask || Options.CurrentGameMode != CustomGameMode.UltimateTeam) return;

            var now = Utils.GetTimeStamp();

            if (LastFixedUpdate == now) return;
            LastFixedUpdate = now;

            RoundTime--;
        }
    }
}
