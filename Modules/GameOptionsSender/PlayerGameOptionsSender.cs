using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Linq;
using InnerNet;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Core;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
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
            Main.RealOptionsData.Restore(new NormalGameOptionsV07(new UnityLogger().Cast<ILogger>()).Cast<IGameOptions>()) : Main.RealOptionsData.Restore(new HideNSeekGameOptionsV07(new UnityLogger().Cast<ILogger>()).Cast<IGameOptions>());
    public override bool IsDirty { get; protected set; }

    public PlayerControl player = player;

    public void SetDirty() => IsDirty = true;

    public override void SendGameOptions()
    {
        if (player.AmOwner)
        {
            var opt = BuildGameOptions();
            foreach (var com in GameManager.Instance.LogicComponents.ToArray())
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
        for (byte i = 0; i < GameManager.Instance.LogicComponents.Count; i++)
        {
            if (GameManager.Instance.LogicComponents[i].TryCast<LogicOptions>(out _))
            {
                SendOptionsArray(optionArray, i, player.GetClientId());
            }
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
        if (!role.IsGhostRole() || player.IsAnySubRole(x => x is CustomRoles.EvilSpirit)) 
        {
            switch (role.GetCustomRoleTypes())
            {
                case CustomRoleTypes.Impostor:
                    AURoleOptions.ShapeshifterCooldown = Options.DefaultShapeshiftCooldown.GetFloat();
                    AURoleOptions.GuardianAngelCooldown = Spiritcaller.SpiritAbilityCooldown.GetFloat();
                    opt.SetVision(true);
                    break;
                case CustomRoleTypes.Neutral:
                    AURoleOptions.GuardianAngelCooldown = Spiritcaller.SpiritAbilityCooldown.GetFloat();
                    break;
                case CustomRoleTypes.Crewmate:
                    AURoleOptions.GuardianAngelCooldown = Spiritcaller.SpiritAbilityCooldown.GetFloat();
                    break;
            }
        }

        player.GetRoleClass()?.ApplyGameOptions(opt, player.PlayerId);

        switch (role)
        {
            case CustomRoles.Terrorist:
            case CustomRoles.EngineerTOHE:
            case CustomRoles.Phantom:
            case CustomRoles.Taskinator:
                AURoleOptions.EngineerCooldown = 0f;
                AURoleOptions.EngineerInVentMaxTime = 0f;
                break;
            case CustomRoles.Warlock:
                AURoleOptions.ShapeshifterCooldown = Main.isCursed ? 1f : Options.DefaultKillCooldown;
                AURoleOptions.ShapeshifterDuration = Options.WarlockShiftDuration.GetFloat();
                break;
            case CustomRoles.Refugee:
                opt.SetVision(true);
                break;
            case CustomRoles.Zombie:
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0.2f);
                break;
         /* case CustomRoles.Paranoia:
                AURoleOptions.EngineerCooldown =
                    !Main.ParaUsedButtonCount.TryGetValue(player.PlayerId, out var count2) || count2 < Options.ParanoiaNumOfUseButton.GetInt()
                    ? Options.ParanoiaVentCooldown.GetFloat()
                    : 300f;
                AURoleOptions.EngineerInVentMaxTime = 1;
                break; */
            case CustomRoles.ShapeshifterTOHE:
                AURoleOptions.ShapeshifterCooldown = Options.ShapeshiftCD.GetFloat();
                AURoleOptions.ShapeshifterDuration = Options.ShapeshiftDur.GetFloat();
                break;
            case CustomRoles.Bloodmoon:
                Bloodmoon.SetKillCooldown();
                break;
            case CustomRoles.ScientistTOHE:
                AURoleOptions.ScientistCooldown = Options.ScientistCD.GetFloat();
                AURoleOptions.ScientistBatteryCharge = Options.ScientistDur.GetFloat();
                break;
            case CustomRoles.Wildling:
                AURoleOptions.ShapeshifterCooldown = Wildling.ShapeshiftCD.GetFloat();
                AURoleOptions.ShapeshifterDuration = Wildling.ShapeshiftDur.GetFloat();
                break;
            case CustomRoles.Vulture:
                Vulture.ApplyGameOptions(opt);
                AURoleOptions.EngineerCooldown = 0f;
                AURoleOptions.EngineerInVentMaxTime = 0f;
                break;
            case CustomRoles.Hater:
            case CustomRoles.Pursuer:
                opt.SetVision(true);
                break;
            case CustomRoles.Maverick:
                Maverick.ApplyGameOptions(opt);
                break;
            case CustomRoles.Workaholic:
                AURoleOptions.EngineerCooldown = Options.WorkaholicVentCooldown.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 0f;
                break;
            case CustomRoles.Solsticer:
                Solsticer.ApplyGameOptions();
                break;
            case CustomRoles.Mario:
                AURoleOptions.EngineerCooldown = Options.MarioVentCD.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.Warden:
                Warden.SetAbilityCooldown();
                break;
            case CustomRoles.Minion:
                Minion.SetAbilityCooldown();
                break;
            case CustomRoles.Hawk:
                Hawk.SetKillCooldown();
                break;
            default:
                opt.SetVision(false);
                break;
        }

        if (Grenadier.On) Grenadier.ApplyGameOptionsForOthers(opt, player);
        if (Dazzler.On) Dazzler.SetDazzled(player, opt);
        if (Deathpact.On) Deathpact.SetDeathpactVision(player, opt);
        if (Spiritcaller.HasEnabled) Spiritcaller.ReduceVision(opt, player);
        if (Pitfall.On) Pitfall.SetPitfallTrapVision(opt, player);

        // Add-ons
        if (Bewilder.IsEnable) Bewilder.ApplyGameOptions(opt, player);
        if (Ghoul.IsEnable) Ghoul.ApplyGameOptions(player);

        foreach (var subRole in player.GetCustomSubRoles().ToArray())
        {
            switch (subRole)
            {
                case CustomRoles.Watcher:
                    Watcher.RevealVotes(opt);
                    break;
                case CustomRoles.Flash:
                    Flash.SetSpeed(player.PlayerId, false);
                    break;
                case CustomRoles.Torch:
                    Torch.ApplyGameOptions(opt);
                    break;
                case CustomRoles.Tired:
                    Tired.ApplyGameOptions(opt, player);
                    break;
                case CustomRoles.Bewilder:
                    Bewilder.ApplyVisionOptions(opt);
                    break;
                case CustomRoles.Reach:
                    Reach.ApplyGameOptions(opt);
                    break;
                case CustomRoles.Madmate:
                    Madmate.ApplyGameOptions(opt);
                    break;
                case CustomRoles.Mare:
                    Mare.ApplyGameOptions(player.PlayerId);
                    break;
                //case CustomRoles.Sunglasses:
                    //opt.SetVision(false);
                    //opt.SetFloat(FloatOptionNames.CrewLightMod, Options.SunglassesVision.GetFloat());
                    //opt.SetFloat(FloatOptionNames.ImpostorLightMod, Options.SunglassesVision.GetFloat());
                    //break;
            }
        }

        AURoleOptions.EngineerCooldown = Mathf.Max(0.01f, AURoleOptions.EngineerCooldown);

        if (Main.AllPlayerKillCooldown.TryGetValue(player.PlayerId, out var killCooldown))
        {
            AURoleOptions.KillCooldown = Mathf.Max(0.01f, killCooldown);
        }

        if (Main.AllPlayerSpeed.TryGetValue(player.PlayerId, out var speed))
        {
            AURoleOptions.PlayerSpeedMod = Mathf.Clamp(speed, Main.MinSpeed, 3f);
        }

        state.taskState.hasTasks = Utils.HasTasks(player.Data, false);

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
