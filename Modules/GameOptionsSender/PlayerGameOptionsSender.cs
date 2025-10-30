using AmongUs.GameOptions;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Linq;
using InnerNet;
using TOHE.Roles.Core;
using Mathf = UnityEngine.Mathf;

namespace TOHE.Modules;

public class PlayerGameOptionsSender(PlayerControl player) : GameOptionsSender
{
    public static void SetDirty(PlayerControl player) => SetDirty(player.PlayerId);
    public static void SetDirty(byte playerId)
    {
        foreach (var sender in AllSenders.OfType<PlayerGameOptionsSender>().Where(sender => sender.player.PlayerId == playerId).ToArray())
        {
            sender.SetDirty();
            break; // Only one sender can have the same player id
        }
    }
    public static void SetDirtyToAll()
    {
        foreach (var sender in AllSenders.OfType<PlayerGameOptionsSender>().ToArray())
        {
            sender.SetDirty();
        }
    }

    public override IGameOptions BasedGameOptions => GameStates.IsNormalGame ?
            Main.RealOptionsData.Restore(new NormalGameOptionsV10(new UnityLogger().Cast<ILogger>()).Cast<IGameOptions>()) : Main.RealOptionsData.Restore(new HideNSeekGameOptionsV10(new UnityLogger().Cast<ILogger>()).Cast<IGameOptions>());
    public override bool IsDirty { get; protected set; }

    public PlayerControl player = player;

    public void SetDirty() => IsDirty = true;

    public override void SendGameOptions()
    {
        if (player.AmOwner)
        {
            var opt = BuildGameOptions();
            foreach (var com in GameManager.Instance.LogicComponents.GetFastEnumerator())
            {
                if (com.TryCast<LogicOptions>(out var lo))
                    lo.SetGameOptions(opt);
            }
            GameOptionsManager.Instance.CurrentGameOptions = opt;
        }
        else base.SendGameOptions();
    }

    public override void SendOptionsArray(Il2CppStructArray<byte> optionArray)
    {
        byte logicOptionsIndex = 0;
        foreach (var logicComponent in GameManager.Instance.LogicComponents.GetFastEnumerator())
        {
            if (logicComponent.TryCast<LogicOptions>(out _))
            {
                SendOptionsArray(optionArray, logicOptionsIndex, player.GetClientId());
            }
            logicOptionsIndex++;
        }
    }
    public static void RemoveSender(PlayerControl player)
    {
        var sender = AllSenders.OfType<PlayerGameOptionsSender>()
        .FirstOrDefault(sender => sender.player.PlayerId == player.PlayerId);
        if (sender == null) return;
        sender.player = null;
        AllSenders.Remove(sender);
    }
    public override IGameOptions BuildGameOptions()
    {
        Main.RealOptionsData ??= new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);

        var opt = BasedGameOptions;
        if (GameStates.IsNormalGame) AURoleOptions.SetOpt(opt);
        else if (GameStates.IsHideNSeek) return opt;

        var state = Main.PlayerStates[player.PlayerId];
        opt.BlackOut(state.IsBlackOut);

        CustomRoles role = player.GetCustomRole();
        switch (Options.CurrentGameMode)
        {
            case CustomGameMode.FFA:
                if (FFAManager.FFALowerVisionList.ContainsKey(player.PlayerId))
                {
                    opt.SetVision(true);
                    opt.SetFloat(FloatOptionNames.CrewLightMod, FFAManager.FFA_LowerVision.GetFloat());
                    opt.SetFloat(FloatOptionNames.ImpostorLightMod, FFAManager.FFA_LowerVision.GetFloat());
                }
                else
                {
                    opt.SetVision(true);
                    opt.SetFloat(FloatOptionNames.CrewLightMod, 1.25f);
                    opt.SetFloat(FloatOptionNames.ImpostorLightMod, 1.25f);
                }

                break;
            case CustomGameMode.CandR:
                CopsAndRobbersManager.ApplyGameOptions(ref opt, player);
                break;
            case CustomGameMode.UltimateTeam:
                opt.SetVision(true);
                opt.SetFloat(FloatOptionNames.CrewLightMod, 1.25f);
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, 1.25f);
                break;
            case CustomGameMode.TrickorTreat:
                opt.SetVision(true);
                opt.SetFloat(FloatOptionNames.CrewLightMod, 1.25f);
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, 1.25f);
                break;
        }

        if (player.Is(Custom_Team.Impostor))
        {
            AURoleOptions.ShapeshifterCooldown = Options.DefaultShapeshiftCooldown.GetFloat();
            opt.SetVision(true);
        }

        if (role.IsGhostRole())
            AURoleOptions.GuardianAngelCooldown = Options.DefaultAngelCooldown.GetFloat();

        /*
         * Builds Modified GameOptions
         */
        player.BuildCustomGameOptions(ref opt);

        AURoleOptions.EngineerCooldown = Mathf.Max(0.01f, AURoleOptions.EngineerCooldown);

        if (Main.AllPlayerKillCooldown.TryGetValue(player.PlayerId, out var killCooldown))
        {
            AURoleOptions.KillCooldown = Mathf.Max(0.02f, killCooldown);
        }

        if (Main.AllPlayerSpeed.TryGetValue(player.PlayerId, out var speed))
        {
            AURoleOptions.PlayerSpeedMod = Mathf.Clamp(speed, Main.MinSpeed, 3f);
        }

        state.taskState.hasTasks = Utils.HasTasks(player.Data, false);
        
        if (Main.UnShapeShifter.Contains(player.PlayerId) && Options.CurrentGameMode != CustomGameMode.CandR)
        {
            AURoleOptions.ShapeshifterDuration = 1f;
        }

        if (Options.GhostCanSeeOtherVotes.GetBool() && player.Data.IsDead)
        {
            opt.SetBool(BoolOptionNames.AnonymousVotes, false);
        }
        if (Options.AdditionalEmergencyCooldown.GetBool() &&
            Options.AdditionalEmergencyCooldownThreshold.GetInt() <= Utils.AllAlivePlayersCount)
        {
            opt.SetInt(
                Int32OptionNames.EmergencyCooldown,
                Options.AdditionalEmergencyCooldownTime.GetInt());
        }
        if (Options.SyncButtonMode.GetBool() && Options.SyncedButtonCount.GetValue() <= Options.UsedButtonCount)
        {
            opt.SetInt(Int32OptionNames.EmergencyCooldown, 3600);
        }

        MeetingTimeManager.ApplyGameOptions(opt);

        AURoleOptions.ShapeshifterCooldown = Mathf.Max(1f, AURoleOptions.ShapeshifterCooldown);
        AURoleOptions.ProtectionDurationSeconds = 0f;
        AURoleOptions.ImpostorsCanSeeProtect = false;

        return opt;
    }

    public override bool AmValid()
    {
        return base.AmValid() && player != null && !player.Data.Disconnected && Main.RealOptionsData != null;
    }
}
