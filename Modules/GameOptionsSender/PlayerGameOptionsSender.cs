using System;
using System.Collections;
using AmongUs.GameOptions;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Linq;
using InnerNet;
using TOHE.Roles.Core;
using TOHE.Roles.Crewmate;
using Mathf = UnityEngine.Mathf;

namespace TOHE.Modules;

public class PlayerGameOptionsSender(PlayerControl player) : GameOptionsSender
{
    public PlayerControl player = player;

    private static IGameOptions BasedGameOptions =>
        Main.RealOptionsData.Restore(new NormalGameOptionsV10(new UnityLogger().CastFast<ILogger>()).CastFast<IGameOptions>());

    protected override bool IsDirty { get; set; }

    public static void SetDirty(byte playerId)
    {
        for (var index = 0; index < AllSenders.Count; index++)
        {
            GameOptionsSender allSender = AllSenders[index];

            if (allSender is PlayerGameOptionsSender sender && sender.player.PlayerId == playerId)
            {
                sender.SetDirty();
                break; // Only one sender can have the same player id
            }
        }
    }

    public static void ForceSendImmediately(byte playerId)
    {
        for (var index = 0; index < AllSenders.Count; index++)
        {
            GameOptionsSender allSender = AllSenders[index];

            if (allSender is PlayerGameOptionsSender sender && sender.player.PlayerId == playerId)
            {
                ForceWaitFrame = true;
                sender.SendGameOptions();
                sender.IsDirty = false;
                break; // Only one sender can have the same player id
            }
        }
    }

    public static void SendAllImmediately()
    {
        ForceWaitFrame = true;

        if (PackedWriterMessages > 0 && PackedWriter != null)
        {
            PackedWriter.EndMessage();
            var capturedWriter = PackedWriter;
            DataFlagRateLimiter.Enqueue(() =>
            {
                AmongUsClient.Instance.SendOrDisconnect(capturedWriter);
                capturedWriter.Recycle();
            }, cleanup: capturedWriter.Recycle);
        }

        PackedWriter = MessageWriter.Get(SendOption.Reliable);
        PackedWriter.StartMessage(26);
        PackedWriter.WritePacked(AmongUsClient.Instance.GameId);
        PackedWriterMessages = 0;
        
        for (var index = 0; index < AllSenders.Count; index++)
        {
            GameOptionsSender allSender = AllSenders[index];

            if (allSender is PlayerGameOptionsSender { IsDirty: true } sender)
            {
                if (PackedWriter != null && (PackedWriter.Length > 500 || PackedWriterMessages >= AmongUsClient.Instance.GetMaxMessagePackingLimit()))
                {
                    PackedWriter.EndMessage();
                    var capturedWriter = PackedWriter;
                    DataFlagRateLimiter.Enqueue(() =>
                    {
                        AmongUsClient.Instance.SendOrDisconnect(capturedWriter);
                        capturedWriter.Recycle();
                    }, cleanup: capturedWriter.Recycle);
                    PackedWriterMessages = 0;
                    PackedWriter = MessageWriter.Get(SendOption.Reliable);
                    PackedWriter.StartMessage(26);
                    PackedWriter.WritePacked(AmongUsClient.Instance.GameId);
                }
                sender.SendGameOptions();
                sender.IsDirty = false;
            }
        }

        if (PackedWriter != null)
        {
            if (PackedWriterMessages > 0)
            {
                PackedWriter.EndMessage();
                var capturedWriter = PackedWriter;
                DataFlagRateLimiter.Enqueue(() =>
                {
                    AmongUsClient.Instance.SendOrDisconnect(capturedWriter);
                    capturedWriter.Recycle();
                }, cleanup: capturedWriter.Recycle);
            }
            else
            {
                PackedWriter.Recycle();
            }
        }

        PackedWriter = null;
        PackedWriterMessages = 0;
    }

    public static void SetDirtyToAll()
    {
        for (var index = 0; index < AllSenders.Count; index++)
        {
            GameOptionsSender allSender = AllSenders[index];

            if (allSender is PlayerGameOptionsSender sender)
                sender.SetDirty();
        }
    }

    // For lights call/fix
    public static void SetDirtyToAllV2()
    {
        for (var index = 0; index < AllSenders.Count; index++)
        {
            GameOptionsSender allSender = AllSenders[index];

            if (allSender is PlayerGameOptionsSender { IsDirty: false } sender && sender.player.IsAlive() && (sender.player.HasDesyncRole() || sender.player.GetCustomRole() is CustomRoles.Transporter or CustomRoles.Lighter or CustomRoles.Doomsayer || sender.player.Is(CustomRoles.Torch) || sender.player.Is(CustomRoles.Mare)))
                sender.SetDirty();
        }
    }

    // For players with kill buttons
    public static void SetDirtyToAllV3()
    {
        for (var index = 0; index < AllSenders.Count; index++)
        {
            GameOptionsSender allSender = AllSenders[index];

            if (allSender is PlayerGameOptionsSender { IsDirty: false } sender && sender.player.IsAlive() && sender.player.CanUseKillButton())
                sender.SetDirty();
        }
    }

    private void SetDirty()
    {
        IsDirty = true;
    }

    protected override void SendGameOptions()
    {
        if (player.AmOwner)
        {
            IGameOptions opt = BuildGameOptions();

            if (GameManager.Instance?.LogicComponents != null)
            {
                foreach (GameLogicComponent com in GameManager.Instance.LogicComponents)
                {
                    if (com.TryCast(out LogicOptions lo))
                        lo.SetGameOptions(opt);
                }
            }

            GameOptionsManager.Instance.CurrentGameOptions = opt;
        }
        else
            base.SendGameOptions();
    }

    protected override IEnumerator SendGameOptionsAsync()
    {
        if (player.AmOwner)
        {
            IGameOptions opt = BuildGameOptions();

            if (GameManager.Instance?.LogicComponents != null)
            {
                foreach (GameLogicComponent com in GameManager.Instance.LogicComponents)
                {
                    if (com.TryCast(out LogicOptions lo))
                        lo.SetGameOptions(opt);

                    yield return WaitFrameIfNecessary();
                }
            }

            GameOptionsManager.Instance.CurrentGameOptions = opt;
        }
        else
            yield return base.SendGameOptionsAsync();
    }
    
    protected override void SendOptionsArray(Il2CppStructArray<byte> optionArray, byte logicOptionsIndex)
    {
        if (PackedWriter == null)
        {
            DataFlagRateLimiter.Enqueue(() =>
            {
                MessageWriter writer = MessageWriter.Get(SendOption.Reliable);

                writer.StartMessage(6);
                {
                    writer.Write(AmongUsClient.Instance.GameId);
                    writer.WritePacked(player.OwnerId);

                    writer.StartMessage(1);
                    {
                        writer.WritePacked(GameManager.Instance.NetId);
                        writer.StartMessage(logicOptionsIndex);
                        {
                            writer.WriteBytesAndSize(optionArray);
                        }
                        writer.EndMessage();
                    }
                    writer.EndMessage();
                }

                writer.EndMessage();

                AmongUsClient.Instance.SendOrDisconnect(writer);
                writer.Recycle();
            });
            return;
        }
        
        PackedWriterMessages++;
        
        PackedWriter.StartMessage(6);
        {
            PackedWriter.Write(AmongUsClient.Instance.GameId);
            PackedWriter.WritePacked(player.OwnerId);

            PackedWriter.StartMessage(1);
            {
                PackedWriter.WritePacked(GameManager.Instance.NetId);
                PackedWriter.StartMessage(logicOptionsIndex);
                {
                    PackedWriter.WriteBytesAndSize(optionArray);
                }
                PackedWriter.EndMessage();
            }
            PackedWriter.EndMessage();
        }
        PackedWriter.EndMessage();
    }

    public static void RemoveSender(PlayerControl player)
    {
        PlayerGameOptionsSender sender = AllSenders.OfType<PlayerGameOptionsSender>()
            .FirstOrDefault(sender => sender.player.PlayerId == player.PlayerId);

        if (sender == null) return;

        sender.player = null;
        AllSenders.Remove(sender);
    }

    public override IGameOptions BuildGameOptions()
    {
        try
        {
            Main.RealOptionsData ??= new(GameOptionsManager.Instance.CurrentGameOptions);

            IGameOptions opt = BasedGameOptions;
            if (GameStates.IsNormalGame) AURoleOptions.SetOpt(opt);
            else if (GameStates.IsHideNSeek) return opt;

            PlayerState state = Main.PlayerStates[player.PlayerId];
            opt.BlackOut(state.IsBlackOut);

            CustomRoles role = player.GetCustomRole();
            RoleTypes roleTypes = player.GetCustomRole().GetRoleTypes();

            if (Options.CurrentGameMode == CustomGameMode.FFA)
            {
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
                AURoleOptions.PlayerSpeedMod = Mathf.Clamp(speed, Main.MinSpeed, 10f);
            }

            state.taskState.hasTasks = Utils.HasTasks(player.Data, false);

            if (Main.UnShapeShifter.Contains(player.PlayerId))
            {
                AURoleOptions.ShapeshifterDuration = 1f;
            }

            if (Options.GhostCanSeeOtherVotes.GetBool() && !player.IsAlive())
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
                opt.SetInt(Int32OptionNames.EmergencyCooldown, 3600);

            MeetingTimeManager.ApplyGameOptions(opt);

            AURoleOptions.ShapeshifterCooldown = Mathf.Max(1f, AURoleOptions.ShapeshifterCooldown);
            AURoleOptions.ProtectionDurationSeconds = 0f;
            AURoleOptions.ImpostorsCanSeeProtect = false;

            return opt;
        }
        catch (Exception e)
        {
            Logger.Error($"Error for {player.GetRealName()} ({player.GetCustomRole()}): {e}", "PlayerGameOptionsSender.BuildGameOptions");
            return BasedGameOptions;
        }
    }

    protected override bool AmValid()
    {
        return base.AmValid() && player && player.Data && !player.Data.Disconnected && Main.RealOptionsData != null;
    }
}