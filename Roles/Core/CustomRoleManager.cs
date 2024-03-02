using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using TOHE.Roles.Crewmate;

namespace TOHE.Roles.Core;

public static class CustomRoleManager
{
    public static bool IsClassEnable(this CustomRoles role) => Main.PlayerStates.Any(x => x.Value.MainRole == role && x.Value.Role.IsEnable);

    public static RoleBase GetRoleClass(this PlayerControl player) => Main.PlayerStates.TryGetValue(player.PlayerId, out var statePlayer) && statePlayer != null ? statePlayer.Role : new VanillaRole();
    public static RoleBase GetRoleClassById(this byte playerId) => Main.PlayerStates.TryGetValue(playerId, out var statePlayer) && statePlayer != null ? statePlayer.Role : new VanillaRole();

    public static RoleBase CreateRoleClass(this CustomRoles role) => role switch
    {
        // ==== Vanilla ====
        CustomRoles.Crewmate => new VanillaRole(),
        CustomRoles.Impostor => new VanillaRole(),
        CustomRoles.Shapeshifter => new VanillaRole(),
        CustomRoles.ImpostorTOHE => new VanillaRole(),
        CustomRoles.ShapeshifterTOHE => new VanillaRole(),

        // ==== Impostors ====
        CustomRoles.Anonymous => new Anonymous(),
        CustomRoles.AntiAdminer => new AntiAdminer(),
        CustomRoles.Arrogance => new Arrogance(),
        CustomRoles.Bard => new Bard(),
        CustomRoles.Berserker  => new Berserker(),
        CustomRoles.Blackmailer => new Blackmailer(),
        CustomRoles.Bomber => new Bomber(),
        CustomRoles.Nuker => new Bomber(),
        CustomRoles.BountyHunter => new BountyHunter(),
        CustomRoles.Butcher => new Butcher(),
        CustomRoles.Camouflager => new Camouflager(),
        CustomRoles.Chronomancer => new Chronomancer(),
        CustomRoles.Cleaner => new Cleaner(),
        CustomRoles.Consigliere => new Consigliere(),
        CustomRoles.Convict => new Convict(),
        CustomRoles.Councillor => new Councillor(),
        CustomRoles.Crewpostor => new Crewpostor(),
        CustomRoles.CursedWolf => new CursedWolf(),
        CustomRoles.Dazzler => new Dazzler(),
        CustomRoles.Deathpact => new Deathpact(),
        CustomRoles.Devourer => new Devourer(),
        CustomRoles.Disperser => new Disperser(),
        CustomRoles.Eraser => new Eraser(),
        CustomRoles.Escapist => new Escapist(),
        //CustomRoles.EvilGuesser => new EvilGuesser(),
        //CustomRoles.EvilTracker => new EvilTracker(),
        //CustomRoles.Fireworker => new EvilGuesser(),
        //CustomRoles.EvilTracker => new EvilTracker(),
        //CustomRoles.FireWorks => new FireWorks(),
        //CustomRoles.Gangster => new Gangster(),
        //CustomRoles.Godfather => new Godfather(),
        //CustomRoles.Greedy => new Greedy(),
        //CustomRoles.Hangman => new Hangman(),
        //CustomRoles.Hitman => new Hitman(),
        //CustomRoles.Inhibitor => new Inhibitor(),
        //CustomRoles.Instigator => new Instigator(),
        //CustomRoles.Kamikaze => new Kamikaze(),
        //CustomRoles.KillingMachine => new KillingMachine(),
        //CustomRoles.Minimalism => new Minimalism(),
        //CustomRoles.Lightning => new Lightning(),
        //CustomRoles.Ludopath => new Ludopath(),
        //CustomRoles.Lurker => new Lurker(),
        //CustomRoles.Mastermind => new Mastermind(),
        //CustomRoles.Mafia => new Mafia(),
        //CustomRoles.Miner => new Miner(),
        //CustomRoles.Morphling => new Morphling(),
        //CustomRoles.Ninja => new Ninja(),
        //CustomRoles.Parasite => new Parasite(),
        //CustomRoles.Penguin => new Penguin(),
        //CustomRoles.Pitfall => new Pitfall(),
        //CustomRoles.Puppeteer => new Puppeteer(),
        //CustomRoles.QuickShooter => new QuickShooter(),
        //CustomRoles.Refugee => new Refugee(),
        //CustomRoles.RiftMaker => new RiftMaker(),
        //CustomRoles.Saboteur => new Saboteur(),
        //CustomRoles.Scavenger => new Scavenger(),
        //CustomRoles.ShapeMaster => new ShapeMaster(),
        //CustomRoles.Sniper => new Sniper(),
        //CustomRoles.Witch => new Witch(),
        CustomRoles.SoulCatcher => new SoulCatcher(),
        //CustomRoles.Swooper => new Swooper(),
        CustomRoles.Stealth => new Stealth(),
        //CustomRoles.TimeThief => new TimeThief(),
        CustomRoles.Trapster => new Trapster(),
        //CustomRoles.Trickster => new Trickster(),
        //CustomRoles.Twister => new Twister(),
        //CustomRoles.Underdog => new Underdog(),
        //CustomRoles.Undertaker => new Undertaker(),
        //CustomRoles.Vampire => new Vampire(),
        //CustomRoles.Vampiress => new Vampire(),
        //CustomRoles.Vindicator => new Vindicator(),
        //CustomRoles.Visionary => new Visionary(),
        //CustomRoles.Warlock => new Warlock(),
        //CustomRoles.Wildling => new Wildling(),
        //CustomRoles.Zombie => new Zombie(),

        // ==== Mini ====
        //CustomRoles.Mini => new Mini(),
        //CustomRoles.EvilMini => new Mini(),
        //CustomRoles.NiceMini => new Mini(),

        // ==== Ghost Impostors ====
        //CustomRoles.Minion => new Minion(),

        // ==== Crewmates ====
        CustomRoles.Engineer => new VanillaRole(),
        CustomRoles.GuardianAngel => new VanillaRole(),
        CustomRoles.Scientist => new VanillaRole(),
        CustomRoles.CrewmateTOHE => new VanillaRole(),
        CustomRoles.EngineerTOHE => new VanillaRole(),
        CustomRoles.GuardianAngelTOHE => new VanillaRole(),
        CustomRoles.ScientistTOHE => new VanillaRole(),
        CustomRoles.Addict => new Addict(),
        //CustomRoles.Aid => new Aid(),
        //CustomRoles.Alchemist => new Alchemist(),
        //CustomRoles.Altruist => new Altruist(),
        //CustomRoles.Analyzer => new Analyzer(),
        //CustomRoles.Autocrat => new Autocrat(),
        //CustomRoles.Beacon => new Beacon(),
        CustomRoles.Benefactor => new Benefactor(),
        //CustomRoles.Bodyguard => new Bodyguard(),
        //CustomRoles.CameraMan => new CameraMan(),
        //CustomRoles.CyberStar => new CyberStar(),
        CustomRoles.Chameleon => new Chameleon(),
        //CustomRoles.Cleaner => new Cleaner(),
        //CustomRoles.Cleanser => new Cleanser(),
        CustomRoles.Captain => new Captain(),
        CustomRoles.CopyCat => new CopyCat(),
        CustomRoles.Coroner => new Coroner(),
        //CustomRoles.Crusader => new Crusader(),
        //CustomRoles.Demolitionist => new Demolitionist(),
        //CustomRoles.Deputy => new Deputy(),
        //CustomRoles.Detective => new Detective(),
        //CustomRoles.Detour => new Detour(),
        CustomRoles.Dictator => new Dictator(),
        //CustomRoles.Doctor => new Doctor(),
        //CustomRoles.DonutDelivery => new DonutDelivery(),
        //CustomRoles.Doormaster => new Doormaster(),
        //CustomRoles.DovesOfNeace => new DovesOfNeace(),
        //CustomRoles.Drainer => new Drainer(),
        //CustomRoles.Druid => new Druid(),
        //CustomRoles.Electric => new Electric(),
        //CustomRoles.Enigma => new Enigma(),
        //CustomRoles.Escort => new Escort(),
        //CustomRoles.Express => new Express(),
        CustomRoles.Overseer => new Overseer(),
        //CustomRoles.Divinator => new Divinator(),
        //CustomRoles.Gaulois => new Gaulois(),
        //CustomRoles.Glitch => new Glitch(),
        //CustomRoles.Grenadier => new Grenadier(),
        //CustomRoles.GuessManager => new GuessManager(),
        CustomRoles.Guardian => new Guardian(),
        //CustomRoles.Ignitor => new Ignitor(),
        //CustomRoles.Insight => new Insight(),
        //CustomRoles.ParityCop => new ParityCop(),
        //CustomRoles.Jailor => new Jailor(),
        //CustomRoles.Judge => new Judge(),
        //CustomRoles.Needy => new Needy(),
        //CustomRoles.Lighter => new Lighter(),
        CustomRoles.Lookout => new Lookout(),
        //CustomRoles.Luckey => new Luckey(),
        CustomRoles.Marshall => new Marshall(),
        CustomRoles.Mayor => new Mayor(),
        //CustomRoles.SabotageMaster => new SabotageMaster(),
        //CustomRoles.Medic => new Medic(),
        //CustomRoles.Mediumshiper => new Mediumshiper(),
        //CustomRoles.Merchant => new Merchant(),
        CustomRoles.Monitor => new Monitor(),
        //CustomRoles.Mole => new Mole(),
        CustomRoles.Monarch => new Monarch(),
        //CustomRoles.Mortician => new Mortician(),
        //CustomRoles.NiceEraser => new NiceEraser(),
        //CustomRoles.NiceGuesser => new NiceGuesser(),
        //CustomRoles.NiceHacker => new NiceHacker(),
        CustomRoles.Swapper => new Swapper(),
        //CustomRoles.Nightmare => new Nightmare(),
        //CustomRoles.Observer => new Observer(),
        //CustomRoles.Oracle => new Oracle(),
        CustomRoles.President => new President(),
        //CustomRoles.Paranoia => new Paranoia(),
        //CustomRoles.Philantropist => new Philantropist(),
        //CustomRoles.Psychic => new Psychic(),
        //CustomRoles.Rabbit => new Rabbit(),
        //CustomRoles.Randomizer => new Randomizer(),
        //CustomRoles.Ricochet => new Ricochet(),
        //CustomRoles.Sentinel => new Sentinel(),
        //CustomRoles.SecurityGuard => new SecurityGuard(),
        //CustomRoles.Sheriff => new Sheriff(),
        //CustomRoles.Shiftguard => new Shiftguard(),
        //CustomRoles.Snitch => new Snitch(),
        //CustomRoles.Spiritualist => new Spiritualist(),
        //CustomRoles.Speedrunner => new Speedrunner(),
        //CustomRoles.SpeedBooster => new SpeedBooster(),
        //CustomRoles.Spy => new Spy(),
        //CustomRoles.SuperStar => new SuperStar(),
        //CustomRoles.TaskManager => new TaskManager(),
        //CustomRoles.Tether => new Tether(),
        //CustomRoles.TimeManager => new TimeManager(),
        //CustomRoles.TimeMaster => new TimeMaster(),
        //CustomRoles.Tornado => new Tornado(),
        //CustomRoles.Tracker => new Tracker(),
        //CustomRoles.Transmitter => new Transmitter(),
        //CustomRoles.Transporter => new Transporter(),
        //CustomRoles.Tracefinder => new Tracefinder(),
        //CustomRoles.Tunneler => new Tunneler(),
        //CustomRoles.Ventguard => new Ventguard(),
        //CustomRoles.Veteran => new Veteran(),
        //CustomRoles.SwordsMan => new SwordsMan(),
        //CustomRoles.Witness => new Witness(),
        //CustomRoles.Agitater => new Agitater(),

        // ==== Neutrals ====
        CustomRoles.Seeker => new Seeker(),
        //CustomRoles.Amnesiac => new Amnesiac(),
        //CustomRoles.Arsonist => new Arsonist(),
        //CustomRoles.Bandit => new Bandit(),
        //CustomRoles.BloodKnight => new BloodKnight(),
        //CustomRoles.Bubble => new Bubble(),
        //CustomRoles.Collector => new Collector(),
        //CustomRoles.Convener => new Convener(),
        //CustomRoles.Deathknight => new Deathknight(),
        //CustomRoles.Gamer => new Gamer(),
        //CustomRoles.Doppelganger => new Doppelganger(),
        //CustomRoles.Doomsayer => new Doomsayer(),
        //CustomRoles.Eclipse => new Eclipse(),
        //CustomRoles.Enderman => new Enderman(),
        //CustomRoles.Executioner => new Executioner(),
        //CustomRoles.Totocalcio => new Totocalcio(),
        //CustomRoles.God => new God(),
        //CustomRoles.FFF => new FFF(),
        //CustomRoles.HeadHunter => new HeadHunter(),
        //CustomRoles.HexMaster => new HexMaster(),
        //CustomRoles.Hookshot => new Hookshot(),
        //CustomRoles.Imitator => new Imitator(),
        //CustomRoles.Innocent => new Innocent(),
        //CustomRoles.Jackal => new Jackal(),
        //CustomRoles.Jester => new Jester(),
        //CustomRoles.Jinx => new Jinx(),
        //CustomRoles.Juggernaut => new Juggernaut(),
        //CustomRoles.Konan => new Konan(),
        //CustomRoles.Lawyer => new Lawyer(),
        //CustomRoles.Magician => new Magician(),
        //CustomRoles.Mario => new Mario(),
        //CustomRoles.Mathematician => new Mathematician(),
        //CustomRoles.Maverick => new Maverick(),
        //CustomRoles.Medusa => new Medusa(),
        //CustomRoles.Mycologist => new Mycologist(),
        //CustomRoles.Necromancer => new Necromancer(),
        //CustomRoles.Opportunist => new Opportunist(),
        //CustomRoles.Pelican => new Pelican(),
        //CustomRoles.Perceiver => new Perceiver(),
        //CustomRoles.Pestilence => new Pestilence(),
        //CustomRoles.Phantom => new Phantom(),
        //CustomRoles.Pickpocket => new Pickpocket(),
        //CustomRoles.PlagueBearer => new PlagueBearer(),
        //CustomRoles.PlagueDoctor => new PlagueDoctor(),
        //CustomRoles.Poisoner => new Poisoner(),
        //CustomRoles.Postman => new Postman(),
        //CustomRoles.Provocateur => new Provocateur(),
        //CustomRoles.Pursuer => new Pursuer(),
        //CustomRoles.Pyromaniac => new Pyromaniac(),
        //CustomRoles.Reckless => new Reckless(),
        //CustomRoles.Revolutionist => new Revolutionist(),
        //CustomRoles.Ritualist => new Ritualist(),
        //CustomRoles.Romantic => new Romantic(),
        //CustomRoles.RuthlessRomantic => new RuthlessRomantic(),
        //CustomRoles.NSerialKiller => new NSerialKiller(),
        //CustomRoles.Sidekick => new Sidekick(),
        //CustomRoles.SoulHunter => new SoulHunter(),
        //CustomRoles.Spiritcaller => new Spiritcaller(),
        //CustomRoles.Sprayer => new Sprayer(),
        //CustomRoles.DarkHide => new DarkHide(),
        //CustomRoles.Succubus => new Succubus(),
        //CustomRoles.Sunnyboy => new Sunnyboy(),
        //CustomRoles.Terrorist => new Terrorist(),
        //CustomRoles.Traitor => new Traitor(),
        //CustomRoles.Vengeance => new Vengeance(),
        //CustomRoles.VengefulRomantic => new VengefulRomantic(),
        //CustomRoles.Virus => new Virus(),
        //CustomRoles.Vulture => new Vulture(),
        //CustomRoles.Wraith => new Wraith(),
        //CustomRoles.Werewolf => new Werewolf(),
        //CustomRoles.WeaponMaster => new WeaponMaster(),
        //CustomRoles.Workaholic => new Workaholic(),
        //CustomRoles.KB_Normal => new KB_Normal(),
        //CustomRoles.Killer => new Killer(),
        //CustomRoles.Tasker => new Tasker(),
        //CustomRoles.Potato => new Potato(),
        //CustomRoles.GM => new GM(),
        //CustomRoles.Convict => new Convict(),
        _ => new VanillaRole(),
    };

    public static HashSet<Action<PlayerControl>> CheckDeadBodyOthers = [];
    /// <summary>
    /// If the role need check a present dead body
    /// </summary>
    public static void CheckDeadBody(PlayerControl deadBody)
    {
        if (CheckDeadBodyOthers.Count <= 0) return;
        //Execute other viewpoint processing if any
        foreach (var checkDeadBodyOthers in CheckDeadBodyOthers.ToArray())
        {
            checkDeadBodyOthers(deadBody);
        }
    }

    public static HashSet<Action<PlayerControl>> OnFixedUpdateOthers = [];
    /// <summary>
    /// Function always called in a task turn
    /// For interfering with other roles
    /// Registered with OnFixedUpdateOthers+= at initialization
    /// </summary>
    public static void OnFixedUpdate(PlayerControl player)
    {
        player.GetRoleClass()?.OnFixedUpdate(player);

        if (OnFixedUpdateOthers.Count <= 0) return;
        //Execute other viewpoint processing if any
        foreach (var onFixedUpdate in OnFixedUpdateOthers.ToArray())
        {
            onFixedUpdate(player);
        }
    }
    public static HashSet<Action<PlayerControl>> OnFixedUpdateLowLoadOthers = [];
    public static void OnFixedUpdateLowLoad(PlayerControl player)
    {
        player.GetRoleClass()?.OnFixedUpdateLowLoad(player);

        if (OnFixedUpdateLowLoadOthers.Count <= 0) return;
        //Execute other viewpoint processing if any
        foreach (var onFixedUpdateLowLoad in OnFixedUpdateLowLoadOthers.ToArray())
        {
            onFixedUpdateLowLoad(player);
        }
    }

    public static HashSet<Func<PlayerControl, PlayerControl, bool, string>> MarkOthers = [];
    public static HashSet<Func<PlayerControl, PlayerControl, bool, bool, string>> LowerOthers = [];
    public static HashSet<Func<PlayerControl, PlayerControl, bool, string>> SuffixOthers = [];

    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        if (MarkOthers.Count <= 0) return string.Empty;

        var sb = new StringBuilder(100);
        foreach (var marker in MarkOthers)
        {
            sb.Append(marker(seer, seen, isForMeeting));
        }
        return sb.ToString();
    }

    public static string GetLowerTextOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (LowerOthers.Count <= 0) return string.Empty;

        var sb = new StringBuilder(100);
        foreach (var lower in LowerOthers)
        {
            sb.Append(lower(seer, seen, isForMeeting, isForHud));
        }
        return sb.ToString();
    }

    public static string GetSuffixOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        if (SuffixOthers.Count <= 0) return string.Empty;

        var sb = new StringBuilder(100);
        foreach (var suffix in SuffixOthers)
        {
            sb.Append(suffix(seer, seen, isForMeeting));
        }
        return sb.ToString();
    }

    public static void Initialize()
    {
        MarkOthers.Clear();
        LowerOthers.Clear();
        SuffixOthers.Clear();
        OnFixedUpdateOthers.Clear();
        OnFixedUpdateLowLoadOthers.Clear();
        CheckDeadBodyOthers.Clear();
    }
}
