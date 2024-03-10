using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;

namespace TOHE.Roles.Core;

public static class CustomRoleManager
{
    //public static Dictionary<byte, RoleBase> AllActiveRoles = new(15);
    public static bool IsClassEnable(this CustomRoles role) => Main.PlayerStates.Any(x => x.Value.MainRole == role && x.Value.RoleClass.IsEnable);

    public static RoleBase GetRoleClass(this PlayerControl player) => GetRoleClassById(player.PlayerId);
    public static RoleBase GetRoleClassById(this byte playerId) => Main.PlayerStates.TryGetValue(playerId, out var statePlayer) && statePlayer != null ? statePlayer.RoleClass : new VanillaRole();

    public static RoleBase CreateRoleClass(this CustomRoles role) => role switch
    {
        // ==== Vanilla ====
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
        CustomRoles.Bomber or CustomRoles.Nuker => new Bomber(),
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
        CustomRoles.EvilGuesser => new EvilGuesser(),
        CustomRoles.EvilTracker => new EvilTracker(),
        CustomRoles.Fireworker => new Fireworker(),
        CustomRoles.Gangster => new Gangster(),
        CustomRoles.Godfather => new Godfather(),
        CustomRoles.Greedy => new Greedy(),
        CustomRoles.Hangman => new Hangman(),
        CustomRoles.Inhibitor => new Inhibitor(),
        //CustomRoles.Instigator => new Instigator(),
        //CustomRoles.Kamikaze => new Kamikaze(),
        //CustomRoles.KillingMachine => new KillingMachine(),
        //CustomRoles.Lightning => new Lightning(),
        //CustomRoles.Ludopath => new Ludopath(),
        //CustomRoles.Lurker => new Lurker(),
        //CustomRoles.Mastermind => new Mastermind(),
        //CustomRoles.Mercenary => new Mercenary(),
        //CustomRoles.Miner => new Miner(),
        //CustomRoles.Morphling => new Morphling(),
        //CustomRoles.Nemesis => new Nemesis(),
        //CustomRoles.Ninja => new Ninja(),
        //CustomRoles.Parasite => new Parasite(),
        //CustomRoles.Penguin => new Penguin(),
        //CustomRoles.Pitfall => new Pitfall(),
        //CustomRoles.Puppeteer => new Puppeteer(),
        //CustomRoles.QuickShooter => new QuickShooter(),
        //CustomRoles.Refugee => new Refugee(),
        //CustomRoles.RiftMaker => new RiftMaker(),
        CustomRoles.Saboteur => new Saboteur(),
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
        CustomRoles.EvilMini or CustomRoles.NiceMini => new Mini(),

        // ==== Ghost Impostors ====
        //CustomRoles.Minion => new Minion(),

        // ==== Vanilla ====
        CustomRoles.Crewmate => new VanillaRole(),
        CustomRoles.Engineer => new VanillaRole(),
        CustomRoles.Scientist => new VanillaRole(),
        CustomRoles.GuardianAngel => new VanillaRole(),
        CustomRoles.CrewmateTOHE => new VanillaRole(),
        CustomRoles.EngineerTOHE => new VanillaRole(),
        CustomRoles.ScientistTOHE => new VanillaRole(),
        CustomRoles.GuardianAngelTOHE => new VanillaRole(),

        // ==== Crewmates ====
        CustomRoles.Addict => new Addict(),
        //ustomRoles.Admirer => new Admirer(),
        //CustomRoles.Alchemist => new Alchemist(),
        CustomRoles.Bastion => new Bastion(),
        CustomRoles.Benefactor => new Benefactor(),
        CustomRoles.Bodyguard => new Bodyguard(),
        CustomRoles.Captain => new Captain(),
        CustomRoles.Celebrity => new Celebrity(),
        CustomRoles.Chameleon => new Chameleon(),
        //CustomRoles.ChiefOfPolice => new ChiefOfPolice(), //role not used
        CustomRoles.Cleanser => new Cleanser(),
        CustomRoles.CopyCat => new CopyCat(),
        CustomRoles.Coroner => new Coroner(),
        CustomRoles.Crusader => new Crusader(),
        CustomRoles.Deceiver => new Deceiver(),
        CustomRoles.Deputy => new Deputy(),
        CustomRoles.Detective => new Detective(),
        CustomRoles.Dictator => new Dictator(),
        CustomRoles.Doctor => new Doctor(),
        CustomRoles.Enigma => new Enigma(),
        CustomRoles.FortuneTeller => new FortuneTeller(),
        CustomRoles.Grenadier => new Grenadier(),
        CustomRoles.Guardian => new Guardian(),
        CustomRoles.GuessMaster => new GuessMaster(),
        CustomRoles.Inspector => new Inspector(),
        CustomRoles.Investigator => new Investigator(),
        CustomRoles.Jailer => new Jailer(),
        CustomRoles.Judge => new Judge(),
        CustomRoles.Keeper => new Keeper(),
        CustomRoles.Knight => new Knight(),
        CustomRoles.LazyGuy => new LazyGuy(),
        CustomRoles.Lighter => new Lighter(),
        CustomRoles.Lookout => new Lookout(),
        CustomRoles.Marshall => new Marshall(),
        CustomRoles.Mayor => new Mayor(),
        CustomRoles.Mechanic => new Mechanic(),
        CustomRoles.Medic => new Medic(),
        CustomRoles.Medium => new Medium(),
        CustomRoles.Merchant => new Merchant(),
        CustomRoles.Mole => new Mole(),
        CustomRoles.Monarch => new Monarch(),
        CustomRoles.Monitor => new Monitor(),
        CustomRoles.Mortician => new Mortician(),
        CustomRoles.NiceGuesser => new NiceGuesser(),
        CustomRoles.Observer => new Observer(),
        CustomRoles.Oracle => new Oracle(),
        CustomRoles.Overseer => new Overseer(),
        CustomRoles.Pacifist => new Pacifist(),
        CustomRoles.President => new President(),
        //CustomRoles.Paranoia => new Paranoia(),
        CustomRoles.Psychic => new Psychic(),
        CustomRoles.Randomizer => new Randomizer(),
        CustomRoles.Retributionist => new Retributionist(),
        CustomRoles.Reverie => new Reverie(),
        CustomRoles.Sheriff => new Sheriff(),
        CustomRoles.Snitch => new Snitch(),
        CustomRoles.Spiritualist => new Spiritualist(),
        CustomRoles.Spy => new Spy(),
        CustomRoles.SuperStar => new SuperStar(),
        CustomRoles.Swapper => new Swapper(),
        CustomRoles.TaskManager => new TaskManager(),
        CustomRoles.TimeManager => new TimeManager(),
        CustomRoles.TimeMaster => new TimeMaster(),
        CustomRoles.Tracefinder => new Tracefinder(),
        CustomRoles.Tracker => new Tracker(),
        CustomRoles.Transporter => new Transporter(),
        CustomRoles.Veteran => new Veteran(),
        CustomRoles.Vigilante => new Vigilante(),
        CustomRoles.Witness => new Witness(),

        // ==== Neutrals ====
        CustomRoles.Seeker => new Seeker(),
        //CustomRoles.Amnesiac => new Amnesiac(),
        CustomRoles.Arsonist => new Arsonist(),
        CustomRoles.Bandit => new Bandit(),
        CustomRoles.BloodKnight => new BloodKnight(),
        CustomRoles.Agitater => new Agitater(),
        //CustomRoles.Collector => new Collector(),
        //CustomRoles.Convener => new Convener(),
        //CustomRoles.Deathknight => new Deathknight(),
        CustomRoles.Demon => new Demon(),
        //CustomRoles.Doppelganger => new Doppelganger(),
        //CustomRoles.Doomsayer => new Doomsayer(),
        //CustomRoles.Eclipse => new Eclipse(),
        //CustomRoles.Enderman => new Enderman(),
        //CustomRoles.Executioner => new Executioner(),
        //CustomRoles.Totocalcio => new Totocalcio(),
        //CustomRoles.God => new God(),
        //CustomRoles.FFF => new FFF(),
        CustomRoles.Huntsman => new Huntsman(),
        CustomRoles.HexMaster => new HexMaster(),
        //CustomRoles.Hookshot => new Hookshot(),
        //CustomRoles.Imitator => new Imitator(),
        //CustomRoles.Innocent => new Innocent(),
        CustomRoles.Infectious => new Infectious(),
        CustomRoles.Jackal => new Jackal(),
        CustomRoles.Sidekick => new Sidekick(),
        //CustomRoles.Jester => new Jester(),
        CustomRoles.Jinx => new Jinx(),
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

    public static HashSet<Action<PlayerControl, PlayerControl>> CheckDeadBodyOthers = [];
    /// <summary>
    /// If the role need check a present dead body
    /// </summary>
    public static void CheckDeadBody(PlayerControl deadBody, PlayerControl killer)
    {
        if (CheckDeadBodyOthers.Count <= 0) return;
        //Execute other viewpoint processing if any
        foreach (var checkDeadBodyOthers in CheckDeadBodyOthers.ToArray())
        {
            checkDeadBodyOthers(deadBody, killer);
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
