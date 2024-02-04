using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Linq;
using InnerNet;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Crewmate;
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

        switch (role)
        {
            case CustomRoles.Terrorist:
            case CustomRoles.SabotageMaster:
       //     case CustomRoles.Mario:
            case CustomRoles.EngineerTOHE:
            case CustomRoles.Phantom:
            case CustomRoles.Crewpostor:
            case CustomRoles.Taskinator:
          //  case CustomRoles.Jester:
            case CustomRoles.Monitor:
                AURoleOptions.EngineerCooldown = 0f;
                AURoleOptions.EngineerInVentMaxTime = 0f;
                break;
            case CustomRoles.Chameleon:
                AURoleOptions.EngineerCooldown = Chameleon.ChameleonCooldown.GetFloat() + 1f;
                AURoleOptions.EngineerInVentMaxTime = 1f;
                break;
            case CustomRoles.Alchemist:
                AURoleOptions.EngineerCooldown = Alchemist.VentCooldown.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 1;
                if (Alchemist.VisionPotionActive)
                {
                    opt.SetVisionV2();
                    if (Utils.IsActive(SystemTypes.Electrical)) opt.SetFloat(FloatOptionNames.CrewLightMod, Alchemist.VisionOnLightsOut.GetFloat() * 5);
                    else opt.SetFloat(FloatOptionNames.CrewLightMod, Alchemist.Vision.GetFloat());
                }
                break;
            case CustomRoles.ShapeMaster:
                AURoleOptions.ShapeshifterCooldown = 1f;
                AURoleOptions.ShapeshifterLeaveSkin = false;
                AURoleOptions.ShapeshifterDuration = Options.ShapeMasterShapeshiftDuration.GetFloat();
                break;
            case CustomRoles.Warlock:
                AURoleOptions.ShapeshifterCooldown = Main.isCursed ? 1f : Options.DefaultKillCooldown;
                AURoleOptions.ShapeshifterDuration = Options.WarlockShiftDuration.GetFloat();
                break;
            case CustomRoles.Escapist:
                AURoleOptions.ShapeshifterCooldown = Options.EscapistSSCD.GetFloat();
                AURoleOptions.ShapeshifterDuration = Options.EscapistSSDuration.GetFloat();
                break;
            case CustomRoles.Miner:
                AURoleOptions.ShapeshifterCooldown = Options.MinerSSCD.GetFloat();
                AURoleOptions.ShapeshifterDuration = Options.MinerSSDuration.GetFloat();
                break;
            case CustomRoles.Mercenary:
                Mercenary.ApplyGameOptions(player);
                break;
            case CustomRoles.Tracefinder:
                Tracefinder.ApplyGameOptions();
                break;
            case CustomRoles.BountyHunter:
                BountyHunter.ApplyGameOptions();
                break;
            case CustomRoles.Sheriff:
            case CustomRoles.Jailer:
            case CustomRoles.SwordsMan:
            case CustomRoles.Arsonist:
            case CustomRoles.Innocent:
            case CustomRoles.Revolutionist:
            case CustomRoles.Medic:
            case CustomRoles.Crusader:
            case CustomRoles.Provocateur:
            case CustomRoles.Monarch:
            case CustomRoles.Deputy:
            case CustomRoles.Investigator:
            case CustomRoles.Counterfeiter:
            case CustomRoles.Witness:
            case CustomRoles.Succubus:
            case CustomRoles.CursedSoul:
            case CustomRoles.Admirer:
            case CustomRoles.Amnesiac:
                opt.SetVision(false);
                break;
            case CustomRoles.Pestilence:
                opt.SetVision(PlagueBearer.PestilenceHasImpostorVision.GetBool());
                break;
            case CustomRoles.Pelican:
                Pelican.ApplyGameOptions(opt);
                break;
            case CustomRoles.Refugee:
        //    case CustomRoles.Minion:
                opt.SetVision(true);
                break;
            case CustomRoles.Virus:
                opt.SetVision(Virus.ImpostorVision.GetBool());
                break;
            case CustomRoles.Zombie:
            case CustomRoles.KillingMachine:
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0.2f);
                break;
            case CustomRoles.Doctor:
                AURoleOptions.ScientistCooldown = 0f;
                AURoleOptions.ScientistBatteryCharge = Options.DoctorTaskCompletedBatteryCharge.GetFloat();
                break;
            case CustomRoles.Mayor:
                AURoleOptions.EngineerCooldown =
                    !Main.MayorUsedButtonCount.TryGetValue(player.PlayerId, out var count) || count < Options.MayorNumOfUseButton.GetInt()
                    ? opt.GetInt(Int32OptionNames.EmergencyCooldown)
                    : 300f;
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
         /* case CustomRoles.Paranoia:
                AURoleOptions.EngineerCooldown =
                    !Main.ParaUsedButtonCount.TryGetValue(player.PlayerId, out var count2) || count2 < Options.ParanoiaNumOfUseButton.GetInt()
                    ? Options.ParanoiaVentCooldown.GetFloat()
                    : 300f;
                AURoleOptions.EngineerInVentMaxTime = 1;
                break; */
         /* case CustomRoles.Mare:
                Mare.ApplyGameOptions(player.PlayerId);
                break; */
            case CustomRoles.EvilTracker:
                EvilTracker.ApplyGameOptions(player.PlayerId);
                break;
            case CustomRoles.ShapeshifterTOHE:
                AURoleOptions.ShapeshifterCooldown = Options.ShapeshiftCD.GetFloat();
                AURoleOptions.ShapeshifterDuration = Options.ShapeshiftDur.GetFloat();
                break;
            case CustomRoles.Bomber:
                AURoleOptions.ShapeshifterCooldown = Options.BombCooldown.GetFloat();
                AURoleOptions.ShapeshifterDuration = 2f;
                break;
            case CustomRoles.Nuker:
                AURoleOptions.ShapeshifterCooldown = Options.NukeCooldown.GetFloat();
                AURoleOptions.ShapeshifterDuration = 2f;
                break;
            case CustomRoles.Mafia:
                AURoleOptions.ShapeshifterCooldown = Options.MafiaShapeshiftCD.GetFloat();
                AURoleOptions.ShapeshifterDuration = Options.MafiaShapeshiftDur.GetFloat();
                break;
            case CustomRoles.ScientistTOHE:
                AURoleOptions.ScientistCooldown = Options.ScientistCD.GetFloat();
                AURoleOptions.ScientistBatteryCharge = Options.ScientistDur.GetFloat();
                break;
            case CustomRoles.Wildling:
                AURoleOptions.ShapeshifterCooldown = Wildling.ShapeshiftCD.GetFloat();
                AURoleOptions.ShapeshifterDuration = Wildling.ShapeshiftDur.GetFloat();
                break;
            case CustomRoles.Jackal:
                Jackal.ApplyGameOptions(opt);
                break;
            case CustomRoles.Sidekick:
                Sidekick.ApplyGameOptions(opt);
                break;
            case CustomRoles.Huntsman:
                Huntsman.ApplyGameOptions(opt);
                break;
            case CustomRoles.Vulture:
                Vulture.ApplyGameOptions(opt);
                AURoleOptions.EngineerCooldown = 0f;
                AURoleOptions.EngineerInVentMaxTime = 0f;
                break;
            case CustomRoles.Poisoner:
                Poisoner.ApplyGameOptions(opt);
                break;
            case CustomRoles.PlagueDoctor:
                PlagueDoctor.ApplyGameOptions(opt);
                break;
            case CustomRoles.Bandit:
                Bandit.ApplyGameOptions(opt);
                break;
            case CustomRoles.Veteran:
                AURoleOptions.EngineerCooldown = Options.VeteranSkillCooldown.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.Grenadier:
                AURoleOptions.EngineerCooldown = Options.GrenadierSkillCooldown.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
     /*       case CustomRoles.Flashbang:
                AURoleOptions.ShapeshifterCooldown = Options.FlashbangSkillCooldown.GetFloat();
                AURoleOptions.ShapeshifterDuration = Options.FlashbangSkillDuration.GetFloat();
                break; */
            case CustomRoles.Lighter:
                AURoleOptions.EngineerInVentMaxTime = 1;
                AURoleOptions.EngineerCooldown = Options.LighterSkillCooldown.GetFloat();
                if (Main.Lighter.Count > 0)
                {
                    opt.SetVision(false);
                    if (Utils.IsActive(SystemTypes.Electrical)) opt.SetFloat(FloatOptionNames.CrewLightMod, Options.LighterVisionOnLightsOut.GetFloat() * 5);
                    else opt.SetFloat(FloatOptionNames.CrewLightMod, Options.LighterVisionNormal.GetFloat());
                }
                break;
            case CustomRoles.TimeMaster:
                AURoleOptions.EngineerCooldown = Options.TimeMasterSkillCooldown.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.Penguin:
                Penguin.ApplyGameOptions();
                break;
            case CustomRoles.Bastion:
                AURoleOptions.EngineerInVentMaxTime = 1;
                AURoleOptions.EngineerCooldown = Options.BastionBombCooldown.GetFloat();
                break;
            case CustomRoles.Hater:
            case CustomRoles.Pursuer:
                opt.SetVision(true);
                break;
            case CustomRoles.SerialKiller:
                SerialKiller.ApplyGameOptions(opt);
                break;
            case CustomRoles.Pyromaniac:
                Pyromaniac.ApplyGameOptions(opt);
                break;
            case CustomRoles.Werewolf:
                Werewolf.ApplyGameOptions(opt);
                break;
            case CustomRoles.Necromancer:
                Necromancer.ApplyGameOptions(opt);
                break;
            case CustomRoles.Morphling:
                Morphling.ApplyGameOptions();
                break;
            case CustomRoles.Traitor:
                Traitor.ApplyGameOptions(opt);
                break;
            case CustomRoles.Glitch:
                Glitch.ApplyGameOptions(opt);
                break;
            case CustomRoles.Shroud:
                Shroud.ApplyGameOptions(opt);
                break;
            case CustomRoles.Maverick:
                Maverick.ApplyGameOptions(opt);
                break;
            case CustomRoles.Medusa:
                Medusa.ApplyGameOptions(opt);
                break;
            case CustomRoles.Jinx:
                Jinx.ApplyGameOptions(opt);
                break;
            case CustomRoles.PotionMaster:
                PotionMaster.ApplyGameOptions(opt);
                break;
            case CustomRoles.Pickpocket:
                Pickpocket.ApplyGameOptions(opt);
                break;
            case CustomRoles.Juggernaut:
                opt.SetVision(Juggernaut.HasImpostorVision.GetBool());
                break;
            case CustomRoles.Reverie:
                opt.SetVision(false);
                break;
            case CustomRoles.Jester:
                AURoleOptions.EngineerCooldown = 0f;
                AURoleOptions.EngineerInVentMaxTime = 0f;
                opt.SetVision(Options.JesterHasImpostorVision.GetBool());
                break;
            case CustomRoles.Doomsayer:
                opt.SetVision(Doomsayer.ImpostorVision.GetBool());
                break;
            case CustomRoles.Infectious:
                opt.SetVision(Infectious.HasImpostorVision.GetBool());
                break;
            case CustomRoles.Doppelganger:
                opt.SetVision(Doppelganger.HasImpostorVision.GetBool());
                break;
            case CustomRoles.Lawyer:
                //Main.NormalOptions.CrewLightMod = Lawyer.LawyerVision.GetFloat();
                break;
            case CustomRoles.Parasite:
                opt.SetVision(true);
                break;
        /*    case CustomRoles.Chameleon:
                opt.SetVision(false);
                break; */
            
            case CustomRoles.Gamer:
                Gamer.ApplyGameOptions(opt);
                break;
            case CustomRoles.HexMaster:
                HexMaster.ApplyGameOptions(opt);
                break;
            //case CustomRoles.Occultist:
            //    Occultist.ApplyGameOptions(opt);
            //    break;
            case CustomRoles.Wraith:
                Wraith.ApplyGameOptions(opt);
                break;
            case CustomRoles.Agitater:
                Agitater.ApplyGameOptions(opt);
                break;
            case CustomRoles.DarkHide:
                DarkHide.ApplyGameOptions(opt);
                break;
            case CustomRoles.Workaholic:
                AURoleOptions.EngineerCooldown = Options.WorkaholicVentCooldown.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 0f;
                break;
            case CustomRoles.Solsticer:
                Solsticer.ApplyGameOptions();
                break;
            case CustomRoles.ImperiusCurse:
                AURoleOptions.ShapeshifterCooldown = Options.ImperiusCurseShapeshiftCooldown.GetFloat();
                AURoleOptions.ShapeshifterLeaveSkin = false;
                AURoleOptions.ShapeshifterDuration = Options.ShapeImperiusCurseShapeshiftDuration.GetFloat();
                break;
            case CustomRoles.QuickShooter:
                AURoleOptions.ShapeshifterCooldown = QuickShooter.ShapeshiftCooldown.GetFloat();
                break;
            case CustomRoles.Camouflager:
                Camouflager.ApplyGameOptions();
                break;
            case CustomRoles.Assassin:
                Assassin.ApplyGameOptions();
                break;
            case CustomRoles.Anonymous:
                Anonymous.ApplyGameOptions();
                break;
            case CustomRoles.Hangman:
                Hangman.ApplyGameOptions();
                break;
            case CustomRoles.Sunnyboy:
                AURoleOptions.ScientistCooldown = 0f;
                AURoleOptions.ScientistBatteryCharge = 60f;
                break;
            case CustomRoles.BloodKnight:
                BloodKnight.ApplyGameOptions(opt);
                break;
            case CustomRoles.DovesOfNeace:
                AURoleOptions.EngineerCooldown = Options.DovesOfNeaceCooldown.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.Disperser:
                Disperser.ApplyGameOptions();
                break;
            case CustomRoles.Farseer:
                opt.SetVision(false);
                opt.SetFloat(FloatOptionNames.CrewLightMod, Farseer.Vision.GetFloat());
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, Farseer.Vision.GetFloat());
                break;
            case CustomRoles.Dazzler:
                Dazzler.ApplyGameOptions();
                break;
            case CustomRoles.Devourer:
                Devourer.ApplyGameOptions();
                break;
            case CustomRoles.Addict:
                AURoleOptions.EngineerCooldown = Addict.VentCooldown.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.Mole:
                AURoleOptions.EngineerCooldown = Mole.VentCooldown.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.Mario:
                AURoleOptions.EngineerCooldown = Options.MarioVentCD.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.Deathpact:
                Deathpact.ApplyGameOptions();
                break;
            case CustomRoles.Twister:
                Twister.ApplyGameOptions();
                break;
            case CustomRoles.Undertaker:
                Undertaker.ApplyGameOptions();
                break;
            case CustomRoles.RiftMaker:
                RiftMaker.ApplyGameOptions();
                break;
            case CustomRoles.Spiritcaller:
                opt.SetVision(Spiritcaller.ImpostorVision.GetBool());
                break;
            case CustomRoles.Pitfall:
                Pitfall.ApplyGameOptions();
                break;
            case CustomRoles.Blackmailer:
                Blackmailer.ApplyGameOptions();
                break;
            default:
                opt.SetVision(false);
                break;
        }

        // If the Bewilder was killed, his killer will receive his vision
        if (Bewilder.IsEnable)
        {
            Bewilder.ApplyGameOptions(opt, player);
        }
        
        if (Ghoul.IsEnable)
        {
            Ghoul.ApplyGameOptions(player);
        }

        // Grenadier or Mad Grenadier enter the vent
        if ((Main.GrenadierBlinding.Count > 0 &&
            (player.GetCustomRole().IsImpostor() ||
            (player.GetCustomRole().IsNeutral() && Options.GrenadierCanAffectNeutral.GetBool()))
            ) 
            || (Main.MadGrenadierBlinding.Count > 0 && !player.GetCustomRole().IsImpostorTeam() && !player.Is(CustomRoles.Madmate)))
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, Options.GrenadierCauseVision.GetFloat());
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, Options.GrenadierCauseVision.GetFloat());
        }

      /*if ((Main.FlashbangInProtect.Count >= 1 && Main.ForFlashbang.Contains(player.PlayerId) && (!player.GetCustomRole().IsCrewmate())))  
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, Options.FlashbangVision.GetFloat());
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, Options.FlashbangVision.GetFloat());
        }*/

        if (Dazzler.IsEnable) Dazzler.SetDazzled(player, opt);
        if (Deathpact.IsEnable) Deathpact.SetDeathpactVision(player, opt);
        if (Spiritcaller.IsEnable) Spiritcaller.ReduceVision(opt, player);
        if (Pitfall.IsEnable) Pitfall.SetPitfallTrapVision(opt, player);

        foreach (var subRole in player.GetCustomSubRoles().ToArray())
        {
            switch (subRole)
            {
                case CustomRoles.Watcher:
                    opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                    break;
                case CustomRoles.Flash:
                    Flash.SetSpeed(player.PlayerId, false);
                    break;
                case CustomRoles.Torch:
                    Torch.ApplyGameOptions(opt);
                    break;
                case CustomRoles.Tired when Tired.playerIdList.ContainsKey(player.PlayerId):
                    Tired.ApplyGameOptions(opt, player);
                    break;
                case CustomRoles.Bewilder:
                    Bewilder.ApplyVisionOptions(opt);
                    break;
                case CustomRoles.Reach:
                    opt.SetInt(Int32OptionNames.KillDistance, 2);
                    break;
                case CustomRoles.Madmate:
                    opt.SetVision(Options.MadmateHasImpostorVision.GetBool());
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
