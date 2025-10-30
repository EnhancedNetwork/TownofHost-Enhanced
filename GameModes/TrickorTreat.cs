using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE;
internal static class TrickorTreat
{
    public static OptionItem GameTime;
    public static OptionItem ShowChatInGame;
    public static OptionItem TrickChance;
    public static OptionItem TrickFreezeTime;

    public static HashSet<byte> FrozenIds = [];
    public static Dictionary<byte, int> Candies = [];
    public static int RoundTime;

    public static void SetupCustomOption()
    {
        GameTime = IntegerOptionItem.Create(67_226_001, "GameTime", new(30, 600, 10), 300, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.TrickorTreat)
            .SetValueFormat(OptionFormat.Seconds)
            .SetHeader(true);
        ShowChatInGame = BooleanOptionItem.Create(67_226_02, "ShowChatInGame", false, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.TrickorTreat);
        TrickChance = IntegerOptionItem.Create(67_226_03, "TrickChance", new(5, 100, 5), 20, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.TrickorTreat)
            .SetValueFormat(OptionFormat.Percent);
        TrickFreezeTime = FloatOptionItem.Create(67_226_04, "TrickFreezeTime", new(5f, 45f, 2.5f), 15f, TabGroup.ModSettings, false)
            .SetGameMode (CustomGameMode.TrickorTreat) 
            .SetValueFormat(OptionFormat.Seconds);
    }

    public static void Init()
    {
        if (CurrentGameMode != CustomGameMode.TrickorTreat) return;

        Candies = [];
    }

    private static void Add(byte playerId)
    { }

    public static Dictionary<byte, CustomRoles> SetRoles()
    {
        Dictionary<byte, CustomRoles> finalRoles = [];
        List<PlayerControl> AllPlayers = Main.AllPlayerControls.ToList();

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

        foreach (PlayerControl pc in AllPlayers)
        {
            finalRoles[pc.PlayerId] = CustomRoles.TrickorTreater; 
            Main.PlayerStates[pc.PlayerId].MainRole = CustomRoles.TrickorTreater; 
            pc.RpcSetCustomRole(CustomRoles.TrickorTreater); 
            pc.RpcChangeRoleBasis(CustomRoles.TrickorTreater); 
            Add(pc.PlayerId);
            Logger.Msg($"set role for {pc.PlayerId}: {finalRoles[pc.PlayerId]}", "SetRoles");
        }
        return finalRoles;
    }

    public static void SetData()
    {
        if (CurrentGameMode != CustomGameMode.TrickorTreat) return;

        RoundTime = GameTime.GetInt() + 8;
        var now = Utils.GetTimeStamp() + 8;

        foreach (var player in Main.AllAlivePlayerControls)
        {
            Candies[player.PlayerId] = 0;
        }
    }

    public static string GetProgressText(byte playerId)
    {
        var player = Utils.GetPlayerById(playerId);
        string progressText = $"<color={player.GetRoleColorCode()}> ({Candies[playerId]}) candies</color>";
        if (playerId == byte.MaxValue) return progressText;
        Color32 textColor = Color.white;

        return Utils.ColorString(textColor, progressText);
    }

    public static void OnTaskComplete(PlayerControl player)
    {
        var rand = IRandom.Instance;
        if (rand.Next(1, 100) <= TrickChance.GetInt())
        {
            var tmpSpeed = Main.AllPlayerSpeed[player.PlayerId];
            Main.AllPlayerSpeed[player.PlayerId] = 0;

            // Vision
            player.MarkDirtySettings();

            _ = new LateTask(() =>
            {
                Main.AllPlayerSpeed[player.PlayerId] = Main.AllPlayerSpeed[player.PlayerId] + tmpSpeed;
                player.MarkDirtySettings();
            }, TrickFreezeTime.GetFloat());
        }
        else
        {
            Candies[player.PlayerId] += 1;
            player.RpcChangeRoleBasis(CustomRoles.TrickorTreater);
            player.RpcSetCustomRole(CustomRoles.TrickorTreater);
        }
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
            if (!GameStates.IsInTask || Options.CurrentGameMode != CustomGameMode.TrickorTreat) return;

            var now = Utils.GetTimeStamp();

            if (LastFixedUpdate == now) return;
            LastFixedUpdate = now;

            RoundTime--;
        }
    }
}
