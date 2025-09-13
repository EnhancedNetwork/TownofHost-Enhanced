using AmongUs.GameOptions;
using System;
using TOHE.Roles.AddOns;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Core;
using TOHE.Roles.Coven;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using static TOHE.Roles.Core.CustomRoleManager;

namespace TOHE;

public static class CustomRolesHelper
{
    public static readonly CustomRoles[] AllRoles = EnumHelper.GetAllValues<CustomRoles>();
    public static Dictionary<CustomRoles, Type> DuplicatedRoles;
    public static readonly Custom_Team[] AllRoleTypes = EnumHelper.GetAllValues<Custom_Team>();
    public static CustomRoles GetVNRole(this CustomRoles role) // RoleBase: Impostor, Shapeshifter, Crewmate, Engineer, Scientist
    {
        // Vanilla roles
        if (role.IsVanilla()) return role;
        if (role is CustomRoles.GM) return CustomRoles.Crewmate;
        if (role is CustomRoles.Killer) return CustomRoles.Impostor;

        // Role base
        if (role.GetStaticRoleClass() is not DefaultSetup) return role.GetStaticRoleClass().ThisRoleBase;

        //Default
        return role switch
        {
            CustomRoles.ShapeshifterTOHE => CustomRoles.Shapeshifter,
            CustomRoles.PhantomTOHE => CustomRoles.Phantom,
            CustomRoles.ScientistTOHE => CustomRoles.Scientist,
            CustomRoles.EngineerTOHE => CustomRoles.Engineer,
            CustomRoles.NoisemakerTOHE => CustomRoles.Noisemaker,
            CustomRoles.TrackerTOHE => CustomRoles.Tracker,
            CustomRoles.DetectiveTOHE => CustomRoles.Detective,
            CustomRoles.ViperTOHE => CustomRoles.Viper,

            _ => role.IsImpostor() ? CustomRoles.Impostor : CustomRoles.Crewmate,
        };
    }

    public static RoleTypes GetDYRole(this CustomRoles role) // Role has a kill button (Non-Impostor)
    {
        if (role is CustomRoles.Killer) return RoleTypes.Impostor; // FFA
        if (role.IsImpostor() && NarcManager.IsNarcAssigned()) // When Narc is in a game,make all Impostor roles desync roles so imps will be able to kill each other
            return role.GetStaticRoleClass().ThisRoleBase.GetRoleTypes();

        return (role.HasImpBasis(ForDesyncRole: false)) && !role.IsImpostor()
            ? role.GetStaticRoleClass().ThisRoleBase.GetRoleTypes()
            : RoleTypes.GuardianAngel;
    }

    public static bool HasImpKillButton(this PlayerControl player, bool considerVanillaShift = false)
    {
        if (player == null) return false;

        if (Options.CurrentGameMode is CustomGameMode.SpeedRun) return true;

        return player.GetCustomRole().HasImpBasis();
    }
    //This is a overall check for vanilla clients to see if they are imp basis 
    public static bool IsGhostRole(this CustomRoles role)
    {
        if (role.GetStaticRoleClass().ThisRoleType is
            Custom_RoleType.CrewmateGhosts or
            Custom_RoleType.CrewmateVanillaGhosts or
            Custom_RoleType.ImpostorGhosts)
            return true;

        return role is
        CustomRoles.EvilSpirit;

    }
    public static bool IsBucketableRole(this CustomRoles role)
        => !role.IsGhostRole() && !role.IsVanilla() && !(role is CustomRoles.GM
                    or CustomRoles.SpeedBooster
                    or CustomRoles.Oblivious
                    or CustomRoles.Flash
                    or CustomRoles.NotAssigned
                    or CustomRoles.SuperStar
                    or CustomRoles.Oblivious
                    or CustomRoles.Solsticer
                    or CustomRoles.Killer
                    or CustomRoles.Mini
                    or CustomRoles.Onbound
                    or CustomRoles.Rebound
                    or CustomRoles.LastImpostor
                    or CustomRoles.Mare
                    or CustomRoles.Cyber
                    or CustomRoles.Sloth
                    or CustomRoles.Apocalypse
                    or CustomRoles.Coven)
            && !role.IsTNA() && !role.IsAdditionRole();

    public static bool HasGhostRole(this PlayerControl player) => player.GetCustomRole().IsGhostRole() || player.IsAnySubRole(x => x.IsGhostRole());

    // Role's basis role is an Impostor (regular imp,shapeshifter,phantom) role
    public static bool HasImpBasis(this CustomRoles role, bool ForDesyncRole = true)
        => role.GetVNRole() is CustomRoles.Impostor
            or CustomRoles.Shapeshifter
            or CustomRoles.Phantom or CustomRoles.ViperTOHE
            || (ForDesyncRole && role.GetDYRole() is RoleTypes.Impostor
                or RoleTypes.Shapeshifter
                or RoleTypes.Phantom or RoleTypes.Viper);

    /*
    public static bool IsExperimental(this CustomRoles role)
    {
        return role is
            CustomRoles.Disperser or
            CustomRoles.Doppelganger or
            CustomRoles.God or
            CustomRoles.Quizmaster;
    }
    */

    // Add-ons
    public static bool IsAdditionRole(this CustomRoles role) => role > CustomRoles.NotAssigned;

    public static bool IsAmneMaverick(this CustomRoles role) // ROLE ASSIGNING, NOT NEUTRAL TYPE
    {
        return role is
            CustomRoles.Jester or
            CustomRoles.Terrorist or
            CustomRoles.Opportunist or
            CustomRoles.PunchingBag or
            CustomRoles.Huntsman or
            CustomRoles.Executioner or
            CustomRoles.Vector or
            CustomRoles.Shaman or
            CustomRoles.Crewpostor or
            CustomRoles.Lawyer or
            CustomRoles.God or
            CustomRoles.Amnesiac or
            CustomRoles.Glitch or
            CustomRoles.Imitator or
            CustomRoles.Bandit or
            CustomRoles.Pestilence or
            CustomRoles.PlagueBearer or
            CustomRoles.Agitater or
            CustomRoles.Innocent or
            CustomRoles.Vulture or
            CustomRoles.Taskinator or
            CustomRoles.Pursuer or
            CustomRoles.Revolutionist or
            CustomRoles.Provocateur or
            CustomRoles.Demon or
            CustomRoles.Hater or
            CustomRoles.Workaholic or
            CustomRoles.Solsticer or
            CustomRoles.Collector or
            CustomRoles.Sunnyboy or
            CustomRoles.Arsonist or
            CustomRoles.Maverick or
            CustomRoles.CursedSoul or
            CustomRoles.Specter or
            CustomRoles.Stalker or
            CustomRoles.Doomsayer or
            CustomRoles.SoulCollector or
            CustomRoles.Lich or
            CustomRoles.Death or
            CustomRoles.Berserker or
            CustomRoles.War or
            CustomRoles.Baker or
            CustomRoles.Famine or
            CustomRoles.Pirate or
            CustomRoles.Seeker or
            CustomRoles.Pixie or
            CustomRoles.Romantic or
            CustomRoles.RuthlessRomantic or
            CustomRoles.VengefulRomantic or
            CustomRoles.Doppelganger or
            CustomRoles.SchrodingersCat or
            CustomRoles.Follower;
    }
    public static bool IsAmneCrew(this PlayerControl target)
    {
        return target.IsCrewVenter()
                || target.GetCustomRole() is
                CustomRoles.Sheriff or
                CustomRoles.ChiefOfPolice or
                CustomRoles.LazyGuy or
                CustomRoles.SuperStar or
                CustomRoles.Celebrity or
                CustomRoles.Mayor or
                CustomRoles.Dictator or
                CustomRoles.NiceGuesser or
                CustomRoles.Bodyguard or
                CustomRoles.Observer or
                CustomRoles.Retributionist or
                CustomRoles.Lookout or
                CustomRoles.Admirer or
                CustomRoles.Cleanser or
                CustomRoles.CopyCat or
                CustomRoles.Deceiver or
                CustomRoles.Crusader or
                CustomRoles.Overseer or
                CustomRoles.Jailer or
                CustomRoles.Judge or
                CustomRoles.Medic or
                CustomRoles.Medium or
                CustomRoles.Monarch or
                CustomRoles.Telecommunication or
                CustomRoles.Swapper or
                CustomRoles.Mechanic;
    }
    public static bool IsAmneNK(this CustomRoles role)
    {
        return role is
            CustomRoles.Sidekick or
            CustomRoles.Infectious or
            CustomRoles.Pyromaniac or
            CustomRoles.Wraith or
            CustomRoles.Shroud or
            CustomRoles.Pelican or
            CustomRoles.Refugee or
            CustomRoles.Parasite or
            CustomRoles.PlagueDoctor or
            CustomRoles.SerialKiller or
            CustomRoles.Werewolf or
            CustomRoles.Pickpocket or
            CustomRoles.Traitor or
            CustomRoles.Virus or
            CustomRoles.Spiritcaller or
            CustomRoles.Jackal or
            CustomRoles.Juggernaut or
            CustomRoles.BloodKnight or
            CustomRoles.Cultist;
    }
    public static bool IsTasklessCrewmate(this CustomRoles role)
    {
        // Based on Imp but counted as crewmate
        return role.HasImpBasis() && role.IsCrewmate();
    }
    public static bool IsTaskBasedCrewmate(this CustomRoles role)
    {
        return role is
            CustomRoles.Snitch or
            CustomRoles.FortuneTeller or
            CustomRoles.Marshall or
            CustomRoles.TimeManager or
            CustomRoles.Guardian or
            CustomRoles.Merchant or
            CustomRoles.Mayor or
            CustomRoles.Captain or
            CustomRoles.Transporter or
            CustomRoles.Retributionist or
            CustomRoles.Benefactor or
            CustomRoles.Alchemist;
    }
    public static bool IsCrewKiller(this CustomRoles role)
    {
        return role.GetStaticRoleClass().ThisRoleType is Custom_RoleType.CrewmateKilling;
    }
    public static bool IsCrewVenter(this PlayerControl target)
    {
        return target.Is(CustomRoles.EngineerTOHE)
            || target.Is(CustomRoles.Mechanic)
            || target.Is(CustomRoles.CopyCat)
            || target.Is(CustomRoles.Telecommunication) && Telecommunication.CanUseVent()
            || Knight.CheckCanUseVent(target)
            || Vigilante.CheckCanUseVent(target)
            || target.Is(CustomRoles.Nimble);
    }
    public static bool IsNeutral(this CustomRoles role)
    {
        if (role is
            //FFA
            CustomRoles.Killer) return true;

        return role.IsNK() || role.IsNonNK() || role.IsNA() || role.IsMadmate();
    }
    public static bool IsNK(this CustomRoles role)
    {
        return role.GetStaticRoleClass().ThisRoleType is Custom_RoleType.NeutralKilling;
    }
    public static bool IsNonNK(this CustomRoles role) // ROLE ASSIGNING, NOT NEUTRAL TYPE
    {
        return role.IsNB() || role.IsNE() || role.IsNC();
    }
    public static bool IsNA(this CustomRoles role)
    {
        return role.GetStaticRoleClass().ThisRoleType is Custom_RoleType.NeutralApocalypse;
    }
    public static bool IsTNA(this CustomRoles role) // Transformed Neutral Apocalypse
    {
        return role is
            CustomRoles.Pestilence or
            CustomRoles.War or
            CustomRoles.Death or
            CustomRoles.Famine;
    }
    public static bool IsNB(this CustomRoles role)
    {
        return role.GetStaticRoleClass().ThisRoleType
            is Custom_RoleType.NeutralBenign;
    }
    public static bool IsNE(this CustomRoles role)
    {
        return role.GetStaticRoleClass().ThisRoleType
            is Custom_RoleType.NeutralEvil;
    }
    public static bool IsNC(this CustomRoles role)
    {
        return role.GetStaticRoleClass().ThisRoleType
            is Custom_RoleType.NeutralChaos;
    }
    public static bool IsImpostor(this CustomRoles role) // IsImp
    {
        if (role.GetStaticRoleClass().ThisRoleType is
            Custom_RoleType.ImpostorVanilla or
            Custom_RoleType.ImpostorKilling or
            Custom_RoleType.ImpostorSupport or
            Custom_RoleType.ImpostorConcealing or
            Custom_RoleType.ImpostorHindering or
            Custom_RoleType.ImpostorGhosts) return true;

        return role is
            CustomRoles.Impostor or
            CustomRoles.Shapeshifter or
            CustomRoles.Phantom or
            CustomRoles.Viper;
    }
    public static bool IsCoven(this CustomRoles role)
    {
        return role.GetStaticRoleClass().ThisRoleType is
            Custom_RoleType.CovenKilling or
            Custom_RoleType.CovenPower or
            Custom_RoleType.CovenTrickery or
            Custom_RoleType.CovenUtility;
    }
    public static bool IsAbleToBeSidekicked(this CustomRoles role)
        => role.GetDYRole() == RoleTypes.Impostor && !role.IsImpostor() && !role.IsRecruitingRole();

    public static bool IsRecruitingRole(this CustomRoles role)
        => role is
            //Crewmate
            CustomRoles.Admirer or
            CustomRoles.ChiefOfPolice or

            //Impostor
            CustomRoles.Gangster or
            CustomRoles.Godfather or

            //Neutral
            CustomRoles.Cultist or
            CustomRoles.Infectious or
            CustomRoles.Jackal or
            CustomRoles.Virus or
            CustomRoles.Spiritcaller or

            //Coven
            CustomRoles.Ritualist;

    public static bool IsMadmate(this CustomRoles role)
    {
        return role.GetStaticRoleClass().ThisRoleType is Custom_RoleType.Madmate;
    }
    /// <summary>
    /// Role Changes the Crewmates Team, Including changing to Impostor.
    /// </summary>

    public static bool IsConverted(this CustomRoles role) => (role is CustomRoles.Egoist && Egoist.EgoistCountAsConverted.GetBool())
        || role is
            CustomRoles.Charmed or
            CustomRoles.Recruit or
            CustomRoles.Infected or
            CustomRoles.Contagious or
            CustomRoles.Soulless or
            CustomRoles.Madmate or
            CustomRoles.Enchanted;

    public static bool IsNotKnightable(this CustomRoles role)
    {
        return role is
            CustomRoles.Mayor or
            CustomRoles.Vindicator or
            CustomRoles.Dictator or
            CustomRoles.Knighted or
            CustomRoles.Glitch or
            CustomRoles.Pickpocket or
            CustomRoles.Stubborn or
            CustomRoles.Stealer;
    }
    public static bool IsSpeedRole(this CustomRoles role)
    {
        return role is
            CustomRoles.Flash or
            CustomRoles.Spurt or
            CustomRoles.Statue or
            CustomRoles.Alchemist or
            CustomRoles.Tired or
            CustomRoles.Sloth;
    }
    public static bool IsRevealingRole(this CustomRoles role, PlayerControl target)
    {
        return (role is CustomRoles.Mayor && Mayor.VisibleToEveryone(target))
            || (role is CustomRoles.SuperStar && SuperStar.VisibleToEveryone(target))
            || (role is CustomRoles.Marshall && target.AllTasksCompleted())
            || (role is CustomRoles.Workaholic && Workaholic.WorkaholicVisibleToEveryone.GetBool())
            || (role is CustomRoles.Doctor && Doctor.VisibleToEveryone(target))
            || (role is CustomRoles.Bait && Bait.BaitNotification.GetBool() && Inspector.CheckBaitCountType)
            || (role is CustomRoles.President && President.CheckReveal(target.PlayerId))
            || (role is CustomRoles.Captain && Captain.CrewCanFindCaptain());
    }
    public static bool IsBetrayalAddon(this CustomRoles role)
    {
        return role is CustomRoles.Madmate
            or CustomRoles.Egoist
            or CustomRoles.Charmed
            or CustomRoles.Recruit
            or CustomRoles.Infected
            or CustomRoles.Contagious
            or CustomRoles.Rascal
            or CustomRoles.Soulless
            or CustomRoles.Enchanted
            or CustomRoles.Narc;
    }

    public static bool IsBetrayalAddonV2(this CustomRoles role)
        => (role.IsBetrayalAddon() && role is not CustomRoles.Rascal)
            || role is CustomRoles.Admired;

    // Exactly,this is not only used to check if an add-on is assigned mid-game
    // It can also be used to check if an add-on should never be removed
    public static bool IsAddonAssignedMidGame(this CustomRoles role)
        => role.IsBetrayalAddonV2()
        || role is CustomRoles.Knighted
                or CustomRoles.Cleansed
                or CustomRoles.Workhorse
                or CustomRoles.LastImpostor
                or CustomRoles.Lovers;

    public static bool IsImpOnlyAddon(this CustomRoles role)
    {
        return role is CustomRoles.Mare or
            CustomRoles.LastImpostor or
            CustomRoles.Tricky or
            CustomRoles.Mare or
            CustomRoles.Clumsy or
            CustomRoles.Mimic or
            CustomRoles.Stealer or
            CustomRoles.Circumvent or
            CustomRoles.Swift;
    }

    public static bool CheckImpCanSeeAllies(this PlayerControl pc, bool CheckAsSeer = false, bool CheckAsTarget = false)
    {
        if (pc.Is(CustomRoles.Narc) && CheckAsSeer) return false; // Narc cannot see Impostors
        if (Main.PlayerStates[pc.PlayerId].IsNecromancer) return false; // Necromancer

        //bool OnlyOneImp = Main.AliveImpostorCount < 2;
        return pc.GetCustomRole() switch
        {
            var r when r.IsImpostor() => true,
            CustomRoles.Refugee => true,
            CustomRoles.Crewpostor => (CheckAsSeer && Crewpostor.KnowsAllies.GetBool()) || (CheckAsTarget && Crewpostor.AlliesKnowCrewpostor.GetBool()),
            //CustomRoles.Parasite => OnlyOneImp,
            _ => false
        };
    }

    public static CustomRoles GetBetrayalAddon(this PlayerControl pc, bool forRecruiter = false)
    {
        //Soulless and Egoist are excluded because they don't actually change player's team.
        List<CustomRoles> BTAddonList = pc.GetCustomSubRoles().Where(x => x.IsBetrayalAddonV2() && x is not CustomRoles.Soulless and not CustomRoles.Egoist).ToList();

        //Get player's betrayal add-on,NotAssigned if player doesn't have betrayal addon
        var addon = BTAddonList.Any() ? BTAddonList.FirstOrDefault() : CustomRoles.NotAssigned;

        //for recruiting roles to get their respective betrayal add-on
        if (forRecruiter)
        {
            if (addon is CustomRoles.Narc) return CustomRoles.Admired;
            if (addon is CustomRoles.NotAssigned)
                return pc.GetCustomRole() switch //default addon for recruiting roles
                {
                    CustomRoles.Admirer => CustomRoles.Admired,
                    CustomRoles.Gangster or CustomRoles.Godfather => CustomRoles.Madmate,
                    CustomRoles.CursedSoul => CustomRoles.Soulless,
                    CustomRoles.Cultist => CustomRoles.Charmed,
                    CustomRoles.Infectious => CustomRoles.Infected,
                    CustomRoles.Jackal => CustomRoles.Recruit,
                    CustomRoles.Virus => CustomRoles.Contagious,
                    CustomRoles.Ritualist => CustomRoles.Enchanted,
                    _ => addon
                };
        }

        return addon;
    }

    public static bool CanBeRecruitedBy(this PlayerControl target, PlayerControl recruiter)
    {
        var addon = recruiter.GetBetrayalAddon(forRecruiter: true);
        //Mini shouldn't be recruited
        if (target.GetCustomRole() is CustomRoles.NiceMini or CustomRoles.EvilMini && Mini.Age < 18) return false;

        //loyal can't be recruited
        if (target.Is(CustomRoles.Loyal)) return false;

        //target already has this addon
        else if (target.Is(addon)) return false;

        //settings disabled,hurried cant be recruited
        else if (target.Is(CustomRoles.Hurried) && !Hurried.CanBeConverted.GetBool()) return false;

        //Coven Leader cant be recruited when they have the necronomicon
        else if (CovenManager.HasNecronomicon(target.PlayerId) && target.Is(CustomRoles.CovenLeader)) return false;

        return addon switch
        {
            CustomRoles.Charmed => Cultist.CanBeCharmed(target),
            CustomRoles.Madmate => recruiter.Is(CustomRoles.Gangster) ? target.CanBeMadmate(forGangster: true) : target.CanBeMadmate(forAdmirer: true),
            CustomRoles.Admired => Admirer.CanBeAdmired(target, recruiter),
            CustomRoles.Enchanted => Ritualist.CanBeConverted(target),
            CustomRoles.Recruit => Jackal.CanBeSidekick(target),
            CustomRoles.Infected => Infectious.CanBeBitten(target),
            CustomRoles.Contagious => target.CanBeInfected(),
            CustomRoles.Soulless => CursedSoul.CanBeSoulless(target),//Cursed Soul recruits players to Soulless by default
            _ => false
        };
    }

    public static bool IsPlayerImpostorTeam(this PlayerControl player, bool onlyMainRole = false) => Main.PlayerStates.TryGetValue(player.PlayerId, out var state) && state.IsPlayerImpostorTeam(onlyMainRole);
    public static bool IsPlayerImpostorTeam(this PlayerState player, bool onlyMainRole = false)
    {
        if (!onlyMainRole)
        {
            if (player.SubRoles.Contains(CustomRoles.Madmate)) return true;
            if (player.SubRoles.Any(x => (x.IsConverted() || x is CustomRoles.Admired or CustomRoles.Narc) && x is not CustomRoles.Madmate)) return false;
        }

        return (player.MainRole.IsImpostor() || player.MainRole.GetCustomRoleType() is Custom_RoleType.Madmate) && !player.IsNecromancer;
    }

    public static bool IsPlayerCrewmateTeam(this PlayerControl player, bool onlyMainRole = false) => Main.PlayerStates.TryGetValue(player.PlayerId, out var state) && state.IsPlayerCrewmateTeam(onlyMainRole);
    public static bool IsPlayerCrewmateTeam(this PlayerState player, bool onlyMainRole = false)
    {
        if (!onlyMainRole)
        {
            if (player.SubRoles.Contains(CustomRoles.Admired) || player.SubRoles.Contains(CustomRoles.Narc))
                return true;
            if (player.SubRoles.Any(x => (x.IsConverted()))) return false;
        }

        return player.MainRole.IsCrewmate() && !player.IsNecromancer;
    }

    public static bool IsPlayerNeutralTeam(this PlayerControl player, bool onlyMainRole = false) => Main.PlayerStates.TryGetValue(player.PlayerId, out var state) && state.IsPlayerNeutralTeam(onlyMainRole);
    public static bool IsPlayerNeutralTeam(this PlayerState player, bool onlyMainRole = false)
    {
        if (!onlyMainRole)
        {
            if (player.SubRoles.Any(x => x is CustomRoles.Admired or CustomRoles.Madmate or CustomRoles.Enchanted or CustomRoles.Narc)) return false;
            if (player.SubRoles.Any(x => (x.IsConverted() && x is not CustomRoles.Madmate or CustomRoles.Enchanted))) return true;
        }

        // Imp roles like crewposter and parasite is counted as netural, but should be treated as impostor team in general
        return player.MainRole.IsNeutral() && player.MainRole.GetCustomRoleType() is not Custom_RoleType.Madmate && !player.IsNecromancer;
    }

    public static bool IsPlayerCovenTeam(this PlayerControl player, bool onlyMainRole = false) => Main.PlayerStates.TryGetValue(player.PlayerId, out var state) && state.IsPlayerCovenTeam(onlyMainRole);
    public static bool IsPlayerCovenTeam(this PlayerState player, bool onlyMainRole = false)
    {
        if (!onlyMainRole)
        {
            if (player.SubRoles.Contains(CustomRoles.Enchanted)) return true;
            if (player.SubRoles.Contains(CustomRoles.Admired) || player.SubRoles.Contains(CustomRoles.Narc)) return false;
            if (player.SubRoles.Any(x => (x.IsConverted() && x is not CustomRoles.Enchanted))) return false;
        }

        return player.MainRole.IsCoven() || player.IsNecromancer;
    }
    public static Dictionary<PlayerControl, List<CustomRoles>> GetAssignableAddons(this List<PlayerControl> players, List<CustomRoles> addons, bool includeCrew = true, bool includeImps = true, bool includeNeutral = true, bool includeCoven = true, bool noHarmfullToCrew = false, bool noHelpfullToEvil = false)
        => players.ToDictionary(player => player,
            pc => (!pc.Is(CustomRoles.Stubborn) &&
                    (!Cleanser.CantGetAddon() || (Cleanser.CantGetAddon() && !pc.Is(CustomRoles.Cleansed))) &&
                    (
                        (includeCrew && pc.GetCustomRole().IsCrewmate())
                        ||
                        (includeImps && pc.GetCustomRole().IsImpostor())
                        ||
                        (includeNeutral && pc.GetCustomRole().IsNeutral())
                        ||
                        (includeCoven && pc.GetCustomRole().IsCoven())
                    ) // Only check addon conflicts if player can get addons
            ) ? addons.Where(
                a =>
                    !a.IsConverted() &&
                    !(noHarmfullToCrew && pc.GetCustomRole().IsCrewmate() && Options.GroupedAddons[AddonTypes.Harmful].Contains(a)) &&
                    !(noHelpfullToEvil && (pc.GetCustomRole().IsImpostor() || pc.GetCustomRole().IsNeutral() || pc.GetCustomRole().IsCoven()) && Options.GroupedAddons[AddonTypes.Helpful].Contains(a)) &&
                    CheckAddonConfilct(a, pc, checkLimitAddons: false)
                ).ToList()
            : []
        );
    public static bool CheckAddonConfilct(CustomRoles role, PlayerControl pc, bool checkLimitAddons = true, bool checkConditions = true)
    {
        // Only add-ons
        if (!role.IsAdditionRole() || pc == null) return false;

        if (pc.Is(CustomRoles.GM) || pc.Is(CustomRoles.LazyGuy)) return false;

        if (checkConditions)
        {
            if (Options.AddonCanBeSettings.TryGetValue(role, out var o) && ((!o.Imp.GetBool() && pc.GetCustomRole().IsImpostor()) || (!o.Neutral.GetBool() && pc.GetCustomRole().IsNeutral()) || (!o.Crew.GetBool() && pc.GetCustomRole().IsCrewmate()) || (!o.Coven.GetBool() && pc.GetCustomRole().IsCoven())))
                return false;

            // if player already has this addon
            else if (pc.Is(role)) return false;
        }

        // Checking Lovers and Romantics
        else if ((pc.Is(CustomRoles.RuthlessRomantic) || pc.Is(CustomRoles.Romantic) || pc.Is(CustomRoles.VengefulRomantic)) && role is CustomRoles.Lovers) return false;

        if (checkLimitAddons)
        {
            if (pc.HasSubRole() && pc.GetCustomSubRoles().Count >= Options.NoLimitAddonsNumMax.GetInt()) return false;
            if (Options.NoLimitAddonsNumMax.GetInt() == 0) return false;
        }

        // Checking for conflicts with roles and other add-ons
        switch (role)
        {
            case var Addon when checkConditions && (pc.IsAnySubRole(x => x.IsSpeedRole()) || pc.GetCustomRole().IsSpeedRole()) && Addon.IsSpeedRole():
                return false;

            case CustomRoles.Autopsy:
                if (pc.Is(CustomRoles.Doctor)
                    || pc.Is(CustomRoles.Tracefinder)
                    || pc.Is(CustomRoles.ScientistTOHE)
                    || pc.Is(CustomRoles.Sunnyboy))
                    return false;
                break;

            case CustomRoles.Bait:
                if (pc.Is(CustomRoles.Trapper)
                    || pc.Is(CustomRoles.Provocateur)
                    || pc.Is(CustomRoles.Unreportable)
                    || pc.Is(CustomRoles.Burst)
                    || pc.Is(CustomRoles.NiceMini)
                    || pc.Is(CustomRoles.Randomizer)
                    || pc.Is(CustomRoles.Solsticer)
                    || pc.Is(CustomRoles.PunchingBag)
                    || (pc.Is(CustomRoles.Onbound) && Bait.BaitNotification.GetBool())
                    || (pc.Is(CustomRoles.Rebound) && Bait.BaitNotification.GetBool())
                    || pc.Is(CustomRoles.GuardianAngelTOHE))
                    return false;
                break;

            case CustomRoles.Trapper:
                if (pc.Is(CustomRoles.Bait)
                    || pc.Is(CustomRoles.Burst)
                    || pc.Is(CustomRoles.Randomizer)
                    || pc.Is(CustomRoles.Solsticer)
                    || pc.Is(CustomRoles.GuardianAngelTOHE)
                    || pc.Is(CustomRoles.PunchingBag))
                    return false;
                break;

            case CustomRoles.Guesser:
                if (Options.GuesserMode.GetBool() && ((pc.GetCustomRole().IsCrewmate() && !Guesser.CrewCanBeGuesser.GetBool()) || (pc.GetCustomRole().IsNeutral() && !Guesser.NeutralCanBeGuesser.GetBool()) || (pc.GetCustomRole().IsImpostor() && !Guesser.ImpCanBeGuesser.GetBool()) || (pc.GetCustomRole().IsCoven() && !Guesser.CovenCanBeGuesser.GetBool())))
                    return false;
                if (pc.Is(CustomRoles.EvilGuesser)
                    || pc.Is(CustomRoles.NiceGuesser)
                    || pc.Is(CustomRoles.Judge)
                    || pc.Is(CustomRoles.CopyCat)
                    || pc.Is(CustomRoles.Doomsayer)
                    || pc.Is(CustomRoles.Nemesis)
                    || pc.Is(CustomRoles.Councillor)
                    || pc.Is(CustomRoles.GuardianAngelTOHE)
                    || pc.Is(CustomRoles.PunchingBag))
                    return false;
                if ((pc.Is(CustomRoles.Specter) && !Specter.CanGuess.GetBool())
                    || (pc.Is(CustomRoles.Terrorist) && (!Terrorist.TerroristCanGuess.GetBool() || Terrorist.CanTerroristSuicideWin.GetBool()))
                    || (pc.Is(CustomRoles.Workaholic) && !Workaholic.WorkaholicCanGuess.GetBool())
                    || (pc.Is(CustomRoles.Solsticer) && !Solsticer.SolsticerCanGuess.GetBool())
                    || (pc.Is(CustomRoles.God) && !God.CanGuess.GetBool()))
                    return false; //Based on guess manager
                if ((pc.GetCustomRole().IsCrewmate() && !Guesser.CrewCanBeGuesser.GetBool()) || (pc.GetCustomRole().IsNeutral() && !Guesser.NeutralCanBeGuesser.GetBool()) || (pc.GetCustomRole().IsImpostor() && !Guesser.ImpCanBeGuesser.GetBool()) || (pc.GetCustomRole().IsCoven() && !Guesser.CovenCanBeGuesser.GetBool()))
                    return false;
                break;

            case CustomRoles.Mundane:
                if (pc.HasImpKillButton() || !Utils.HasTasks(pc.Data, false) || pc.GetCustomRole().IsTasklessCrewmate() || pc.Is(Custom_Team.Impostor) || pc.Is(Custom_Team.Coven))
                    return false;
                if ((pc.GetCustomRole().IsCrewmate() && !Mundane.CanBeOnCrew.GetBool()) || (pc.GetCustomRole().IsNeutral() && !Mundane.CanBeOnNeutral.GetBool()))
                    return false;
                if (pc.Is(CustomRoles.CopyCat)
                    || pc.Is(CustomRoles.Doomsayer)
                    || pc.Is(CustomRoles.GuardianAngelTOHE)
                    || pc.Is(CustomRoles.Collector)
                    || pc.Is(CustomRoles.Ghoul))
                    return false;
                if ((pc.Is(CustomRoles.Specter) && !Specter.CanGuess.GetBool())
                    || (pc.Is(CustomRoles.Terrorist) && (!Terrorist.TerroristCanGuess.GetBool() || Terrorist.CanTerroristSuicideWin.GetBool()))
                    || (pc.Is(CustomRoles.Workaholic) && !Workaholic.WorkaholicCanGuess.GetBool())
                    || (pc.Is(CustomRoles.Solsticer) && !Solsticer.SolsticerCanGuess.GetBool())
                    || (pc.Is(CustomRoles.God) && !God.CanGuess.GetBool()))
                    return false; //Based on guess manager

                // return true only when its a guesser, NG, guesser mode on with crew can guess (if crew role) and nnk can guess (if nnk)
                if (pc.Is(CustomRoles.Guesser) || pc.Is(CustomRoles.NiceGuesser)) return true;
                if (Options.GuesserMode.GetBool())
                {
                    if (pc.GetCustomRole().IsNonNK() && Options.PassiveNeutralsCanGuess.GetBool())
                        return true;
                    if (pc.GetCustomRole().IsCrewmate() && Options.CrewmatesCanGuess.GetBool())
                        return true;
                    else return false;
                }
                else return false;

            case CustomRoles.Onbound:
                if (pc.Is(CustomRoles.SuperStar)
                    || Doctor.VisibleToEveryone(pc)
                    || (pc.Is(CustomRoles.Bait) && Bait.BaitNotification.GetBool())
                    || pc.Is(CustomRoles.LastImpostor)
                    || pc.Is(CustomRoles.NiceMini)
                    || pc.Is(CustomRoles.Mare)
                    || pc.Is(CustomRoles.Solsticer)
                    || pc.Is(CustomRoles.Rebound)
                    || pc.Is(CustomRoles.Workaholic) && !Workaholic.WorkaholicVisibleToEveryone.GetBool()
                    || pc.Is(CustomRoles.PunchingBag))
                    return false; //Based on guess manager
                break;

            case CustomRoles.Rebound:
                if (pc.Is(CustomRoles.SuperStar)
                    || Doctor.VisibleToEveryone(pc)
                    || (pc.Is(CustomRoles.Bait) && Bait.BaitNotification.GetBool())
                    || pc.Is(CustomRoles.LastImpostor)
                    || pc.Is(CustomRoles.NiceMini)
                    || pc.Is(CustomRoles.Mare)
                    || pc.Is(CustomRoles.Solsticer)
                    || pc.Is(CustomRoles.Onbound)
                    || pc.Is(CustomRoles.Workaholic) && !Workaholic.WorkaholicVisibleToEveryone.GetBool()
                    || pc.Is(CustomRoles.PunchingBag))
                {
                    return false;
                } //Based on guess manager
                break;

            case CustomRoles.DoubleShot:

                //Guesser roles when not guesser mode
                if (!Options.GuesserMode.GetBool() && !pc.Is(CustomRoles.EvilGuesser) && !pc.Is(CustomRoles.NiceGuesser) && (!pc.Is(CustomRoles.Doomsayer)) && !pc.Is(CustomRoles.Guesser))
                    return false;

                //If guesser mode but doomsayer can't die anyways
                if (pc.Is(CustomRoles.Doomsayer) && Doomsayer.DoesNotSuicideWhenMisguessing.GetBool())
                    return false;


                if (pc.Is(CustomRoles.CopyCat)
                    || pc.Is(CustomRoles.Workaholic) && !Workaholic.WorkaholicCanGuess.GetBool()
                    || (pc.Is(CustomRoles.Terrorist) && (!Terrorist.TerroristCanGuess.GetBool() || Terrorist.CanTerroristSuicideWin.GetBool())
                    || (pc.Is(CustomRoles.Specter) && !Specter.CanGuess.GetBool()))
                    || (pc.Is(CustomRoles.Solsticer) && !Solsticer.SolsticerCanGuess.GetBool())
                    || (pc.Is(CustomRoles.God) && !God.CanGuess.GetBool()))
                    return false;
                if (Options.GuesserMode.GetBool())
                {
                    if (DoubleShot.ImpCanBeDoubleShot.GetBool() && !pc.Is(CustomRoles.Guesser) && !pc.Is(CustomRoles.EvilGuesser) && (pc.Is(Custom_Team.Impostor) || pc.GetCustomRole().IsMadmate()) && !Options.ImpostorsCanGuess.GetBool())
                        return false;
                    if (DoubleShot.CrewCanBeDoubleShot.GetBool() && !pc.Is(CustomRoles.Guesser) && !pc.Is(CustomRoles.NiceGuesser) && (pc.Is(Custom_Team.Crewmate) && !Options.CrewmatesCanGuess.GetBool()))
                        return false;
                    if (DoubleShot.NeutralCanBeDoubleShot.GetBool() && !pc.Is(CustomRoles.Guesser) && !pc.Is(CustomRoles.Doomsayer) && ((pc.GetCustomRole().IsNonNK() && !Options.PassiveNeutralsCanGuess.GetBool()) || (pc.GetCustomRole().IsNK() && !Options.NeutralKillersCanGuess.GetBool()) || (pc.GetCustomRole().IsNA() && !Options.NeutralApocalypseCanGuess.GetBool())))
                        return false;
                    if (DoubleShot.CovenCanBeDoubleShot.GetBool() && !pc.Is(CustomRoles.Guesser) && (pc.Is(Custom_Team.Coven) && !Options.CovenCanGuess.GetBool()))
                        return false;
                }
                if ((pc.Is(Custom_Team.Impostor) && !DoubleShot.ImpCanBeDoubleShot.GetBool()) || (pc.Is(Custom_Team.Crewmate) && !DoubleShot.CrewCanBeDoubleShot.GetBool()) || (pc.Is(Custom_Team.Neutral) && !DoubleShot.NeutralCanBeDoubleShot.GetBool()) || (pc.Is(Custom_Team.Coven) && !DoubleShot.CovenCanBeDoubleShot.GetBool()))
                    return false;
                break;

            case CustomRoles.Cyber:
                if (pc.Is(CustomRoles.Doppelganger)
                    || pc.Is(CustomRoles.Celebrity)
                    || pc.Is(CustomRoles.SchrodingersCat)
                    || pc.Is(CustomRoles.SuperStar))
                    return false;
                break;

            case CustomRoles.Reach:
                if (!pc.CanUseKillButton() && !pc.Is(CustomRoles.Narc))
                    return false;
                break;

            case CustomRoles.Overclocked:
                if (!pc.CanUseKillButton() && !pc.Is(CustomRoles.Narc))
                    return false;
                if (pc.Is(CustomRoles.Reverie))
                    return false;
                break;

            case CustomRoles.Lazy:
                if (!Lazy.CheckConflicts(pc))
                    return false;
                break;

            case CustomRoles.Ghoul:
                if (pc.Is(CustomRoles.Lazy)
                    || pc.Is(CustomRoles.LazyGuy)
                    || pc.Is(CustomRoles.Mundane)
                    || pc.Is(CustomRoles.Burst)
                    || pc.Is(CustomRoles.NiceMini))
                    return false;
                if (pc.GetCustomRole().IsNeutral() || pc.GetCustomRole().IsImpostor() || pc.GetCustomRole().IsCoven() || pc.GetCustomRole().IsTasklessCrewmate() || pc.GetCustomRole().IsTaskBasedCrewmate())
                    return false;
                break;

            case CustomRoles.Bloodthirst:
                if (pc.Is(CustomRoles.Lazy)
                    || pc.Is(CustomRoles.Merchant)
                    || pc.Is(CustomRoles.Alchemist)
                    || pc.Is(CustomRoles.LazyGuy)
                    || pc.Is(CustomRoles.Crewpostor)
                    || pc.Is(CustomRoles.Bodyguard)
                    || pc.Is(CustomRoles.NiceMini))
                    return false;
                if (!pc.GetCustomRole().IsCrewmate() || pc.GetCustomRole().IsTasklessCrewmate())
                    return false;
                break;

            case CustomRoles.Torch:
                if (pc.Is(CustomRoles.Bewilder)
                    || pc.Is(CustomRoles.Lighter)
                    || pc.Is(CustomRoles.Tired)
                    || pc.Is(CustomRoles.GuardianAngelTOHE)
                    || pc.Is(CustomRoles.KillingMachine))
                    return false;
                if (!pc.GetCustomRole().IsCrewmate() && !pc.Is(CustomRoles.Narc))
                    return false;
                break;

            case CustomRoles.Fragile:
                if (pc.Is(CustomRoles.Lucky)
                    || pc.Is(CustomRoles.Veteran)
                    || pc.Is(CustomRoles.Guardian)
                    || pc.Is(CustomRoles.Medic)
                    || pc.Is(CustomRoles.Bomber)
                    || pc.Is(CustomRoles.Jinx)
                    || pc.Is(CustomRoles.Solsticer)
                    || pc.Is(CustomRoles.CursedWolf)
                    || pc.Is(CustomRoles.PunchingBag)
                    || pc.Is(CustomRoles.SchrodingersCat)
                    || pc.IsNeutralApocalypse()
                    || pc.Is(CustomRoles.Spy)
                    || pc.Is(CustomRoles.Necromancer)
                    || pc.Is(CustomRoles.Demon)
                    || pc.Is(CustomRoles.Shaman)
                    || pc.Is(CustomRoles.Monarch)
                    || pc.Is(CustomRoles.NiceMini)
                    || pc.Is(CustomRoles.Opportunist) && Opportunist.OppoImmuneToAttacksWhenTasksDone.GetBool())
                    return false;
                break;

            case CustomRoles.VoidBallot:
                if (pc.Is(CustomRoles.Mayor)
                    || pc.Is(CustomRoles.Vindicator)
                    || pc.Is(CustomRoles.Stealer)
                    || pc.Is(CustomRoles.Pickpocket)
                    || pc.Is(CustomRoles.Dictator)
                    || pc.Is(CustomRoles.Influenced)
                    || pc.Is(CustomRoles.Silent)
                    || pc.Is(CustomRoles.Tiebreaker)
                    || pc.Is(CustomRoles.Paranoia))
                    return false;
                break;

            case CustomRoles.Glow:
                if (pc.Is(CustomRoles.KillingMachine))
                    return false;
                break;

            case CustomRoles.Antidote:
                if (pc.Is(CustomRoles.Diseased) || pc.Is(CustomRoles.Solsticer))
                    return false;
                break;

            case CustomRoles.Diseased:
                if (pc.Is(CustomRoles.Antidote) || pc.Is(CustomRoles.Solsticer))
                    return false;
                break;

            case CustomRoles.Seer:
                if (pc.Is(CustomRoles.Mortician)
                    || pc.Is(CustomRoles.EvilTracker)
                    || pc.Is(CustomRoles.GuardianAngelTOHE))
                    return false;
                break;

            case CustomRoles.Sleuth:
                if (pc.Is(CustomRoles.Oblivious)
                    || pc.Is(CustomRoles.Forensic)
                    || pc.Is(CustomRoles.Mortician)
                    || pc.Is(CustomRoles.Cleaner)
                    || pc.Is(CustomRoles.Medusa)
                    || pc.Is(CustomRoles.Vulture)
                    || pc.Is(CustomRoles.Coroner))
                    return false;
                break;

            case CustomRoles.Necroview:
                if (pc.Is(CustomRoles.Doctor)
                    || pc.Is(CustomRoles.God)
                    || pc.Is(CustomRoles.Visionary)
                    || pc.Is(CustomRoles.GuardianAngelTOHE)
                    || pc.Is(CustomRoles.Mimic))
                    return false;
                break;

            case CustomRoles.Bewilder:
                if (pc.Is(CustomRoles.Torch)
                    //|| pc.Is(CustomRoles.Sunglasses)
                    || pc.Is(CustomRoles.Randomizer)
                    || pc.Is(CustomRoles.Lighter)
                    || pc.Is(CustomRoles.Solsticer)
                    || pc.Is(CustomRoles.Tired)
                    || pc.Is(CustomRoles.GuardianAngelTOHE)
                    || pc.Is(CustomRoles.PunchingBag)
                    || pc.Is(CustomRoles.KillingMachine))
                    return false;
                break;

            case CustomRoles.Lucky:
                if (pc.Is(CustomRoles.Guardian)
                    || pc.Is(CustomRoles.Unlucky)
                    || pc.Is(CustomRoles.Solsticer)
                    || pc.Is(CustomRoles.Fragile)
                    || pc.Is(CustomRoles.PunchingBag))
                    return false;
                break;

            case CustomRoles.Unlucky:
                if (pc.Is(CustomRoles.Vector)
                    || pc.Is(CustomRoles.Lucky)
                    || pc.Is(CustomRoles.Vector)
                    || pc.Is(CustomRoles.Solsticer)
                    || pc.Is(CustomRoles.Taskinator)
                    || pc.Is(CustomRoles.NiceMini)
                    || pc.Is(CustomRoles.PunchingBag)
                    || pc.IsTransformedNeutralApocalypse())
                    return false;
                break;

            case CustomRoles.Madmate:
                if (pc.Is(CustomRoles.Sidekick)
                    || pc.Is(CustomRoles.SuperStar)
                    || pc.Is(CustomRoles.Egoist)
                    || pc.Is(CustomRoles.Rascal)
                    || pc.Is(CustomRoles.NiceMini))
                    return false;
                if (!pc.CanBeMadmate() || pc.IsAnySubRole(sub => sub.IsConverted()))
                    return false;
                break;

            case CustomRoles.Oblivious:
                if (pc.Is(CustomRoles.Forensic)
                    || pc.Is(CustomRoles.Vulture)
                    || pc.Is(CustomRoles.Sleuth)
                    || pc.Is(CustomRoles.Cleaner)
                    || pc.Is(CustomRoles.Amnesiac)
                    || pc.Is(CustomRoles.Coroner)
                    || pc.Is(CustomRoles.Medusa)
                    || pc.Is(CustomRoles.Mortician)
                    || pc.Is(CustomRoles.Medium)
                    || pc.Is(CustomRoles.KillingMachine)
                    || pc.Is(CustomRoles.GuardianAngelTOHE)
                    || pc.Is(CustomRoles.Altruist))
                    return false;
                break;

            case CustomRoles.Tiebreaker:
                if (pc.Is(CustomRoles.Dictator)
                    || pc.Is(CustomRoles.VoidBallot)
                    || pc.Is(CustomRoles.Influenced)
                    || pc.Is(CustomRoles.GuardianAngelTOHE)) return false;
                break;

            case CustomRoles.Rebirth:
                if (pc.Is(CustomRoles.Doppelganger)
                    || pc.Is(CustomRoles.Jester)
                    || pc.Is(CustomRoles.Zombie)
                    || pc.Is(CustomRoles.Solsticer) || pc.IsNeutralApocalypse()) return false;
                break;

            case CustomRoles.Youtuber:
                if (pc.Is(CustomRoles.Madmate)
                    || pc.Is(CustomRoles.NiceMini)
                    || pc.Is(CustomRoles.Randomizer)
                    || pc.Is(CustomRoles.Sheriff)
                    || pc.Is(CustomRoles.Hurried)
                    || pc.Is(CustomRoles.Solsticer)
                    || pc.Is(CustomRoles.GuardianAngelTOHE))
                    return false;
                if (!pc.GetCustomRole().IsCrewmate())
                    return false;
                break;

            case CustomRoles.Egoist:
                if (pc.Is(CustomRoles.Sidekick)
                    || pc.Is(CustomRoles.Madmate)
                    || pc.Is(CustomRoles.Hurried)
                    || pc.Is(CustomRoles.Gangster)
                    || pc.Is(CustomRoles.Admirer)
                    || pc.Is(CustomRoles.NiceMini)
                    || pc.Is(CustomRoles.GuardianAngelTOHE)
                    || pc.Is(CustomRoles.Godfather)
                    || pc.Is(CustomRoles.Narc))
                    return false;
                if (pc.GetCustomRole().IsNeutral() || pc.GetCustomRole().IsMadmate() || pc.IsAnySubRole(sub => sub.IsConverted()) || pc.GetCustomRole().IsCoven())
                    return false;
                if ((pc.GetCustomRole().IsImpostor() && !Egoist.ImpCanBeEgoist.GetBool()) || (pc.GetCustomRole().IsCrewmate() && !Egoist.CrewCanBeEgoist.GetBool()))
                    return false;
                break;

            case CustomRoles.Mimic:
                if (pc.Is(CustomRoles.Nemesis)
                    || pc.Is(CustomRoles.Narc)
                    || pc.Is(CustomRoles.Necroview))
                    return false;
                if (!pc.GetCustomRole().IsImpostor())
                    return false;
                break;

            case CustomRoles.Rascal:
                if (pc.Is(CustomRoles.SuperStar)
                    || pc.Is(CustomRoles.NiceMini)
                    || pc.Is(CustomRoles.Madmate)
                    || pc.Is(CustomRoles.ChiefOfPolice))
                    return false;
                if (!pc.GetCustomRole().IsCrewmate())
                    return false;
                break;

            case CustomRoles.Stealer:
                if (pc.Is(CustomRoles.Vindicator)
                    || pc.Is(CustomRoles.Bomber)
                    || pc.Is(CustomRoles.VoidBallot)
                    || pc.Is(CustomRoles.Swift))
                    return false;
                if (!pc.GetCustomRole().IsImpostor())
                    return false;
                break;
            case CustomRoles.Tricky:
                if (pc.Is(CustomRoles.Mastermind)
                    || pc.Is(CustomRoles.Vampire)
                    || pc.Is(CustomRoles.Puppeteer)
                    || pc.Is(CustomRoles.Scavenger)
                    || pc.Is(CustomRoles.Lightning)
                    || pc.Is(CustomRoles.Swift)
                    || pc.Is(CustomRoles.Swooper)
                    || pc.Is(CustomRoles.DoubleAgent))
                    return false;
                if (!pc.GetCustomRole().IsImpostor())
                    return false;
                break;
            case CustomRoles.Mare:
                if (pc.Is(CustomRoles.Underdog)
                    || pc.Is(CustomRoles.Berserker)
                    || pc.Is(CustomRoles.Inhibitor)
                    || pc.Is(CustomRoles.Saboteur)
                    || pc.Is(CustomRoles.Swift)
                    || pc.Is(CustomRoles.Nemesis)
                    || pc.Is(CustomRoles.Sniper)
                    || pc.Is(CustomRoles.Fireworker)
                    || pc.Is(CustomRoles.Ludopath)
                    || pc.Is(CustomRoles.Swooper)
                    || pc.Is(CustomRoles.Vampire)
                    || pc.Is(CustomRoles.Arrogance)
                    || pc.Is(CustomRoles.LastImpostor)
                    || pc.Is(CustomRoles.Bomber)
                    || pc.Is(CustomRoles.Trapster)
                    || pc.Is(CustomRoles.Onbound)
                    || pc.Is(CustomRoles.Rebound)
                    || pc.Is(CustomRoles.Tired)
                    || pc.Is(CustomRoles.Flash)
                    || pc.Is(CustomRoles.Sloth)
                    || pc.Is(CustomRoles.KillingMachine)
                    || pc.Is(CustomRoles.Narc))
                    return false;
                if (!pc.GetCustomRole().IsImpostor())
                    return false;
                break;

            case CustomRoles.Swift:
                if (pc.Is(CustomRoles.Bomber)
                    || pc.Is(CustomRoles.Trapster)
                    || pc.Is(CustomRoles.Kamikaze)
                    || pc.Is(CustomRoles.Swooper)
                    || pc.Is(CustomRoles.Vampire)
                    || pc.Is(CustomRoles.Scavenger)
                    || pc.Is(CustomRoles.Puppeteer)
                    || pc.Is(CustomRoles.Mastermind)
                    || pc.Is(CustomRoles.Warlock)
                    || pc.Is(CustomRoles.Witch)
                    || pc.Is(CustomRoles.Penguin)
                    || pc.Is(CustomRoles.Nemesis)
                    || pc.Is(CustomRoles.Mare)
                    || pc.Is(CustomRoles.Clumsy)
                    || pc.Is(CustomRoles.Wildling)
                    || pc.Is(CustomRoles.Consigliere)
                    || pc.Is(CustomRoles.Butcher)
                    || pc.Is(CustomRoles.KillingMachine)
                    || pc.Is(CustomRoles.Gangster)
                    || pc.Is(CustomRoles.BountyHunter)
                    || pc.Is(CustomRoles.Lightning)
                    || pc.Is(CustomRoles.Hangman)
                    || pc.Is(CustomRoles.Stealer)
                    || pc.Is(CustomRoles.Tricky)
                    || pc.Is(CustomRoles.DoubleAgent)
                    || pc.Is(CustomRoles.YinYanger))
                    return false;
                if (!pc.GetCustomRole().IsImpostor())
                    return false;
                break;

            case CustomRoles.Nimble:
                if (Knight.CheckCanUseVent(pc)
                    || Vigilante.CheckCanUseVent(pc)
                    || pc.Is(CustomRoles.CopyCat))
                    return false;
                if (!pc.GetCustomRole().IsTasklessCrewmate())
                    return false;
                break;

            case CustomRoles.Circumvent:
                if (pc.GetCustomRole() is CustomRoles.Vampire && !Vampire.CheckCanUseVent()
                    || pc.Is(CustomRoles.Witch) && Witch.ModeSwitchActionOpt.GetValue() == 1
                    || pc.Is(CustomRoles.Swooper)
                    || pc.Is(CustomRoles.Wildling)
                    || pc.Is(CustomRoles.KillingMachine)
                    || pc.Is(CustomRoles.Lurker)
                    || pc.Is(CustomRoles.Miner)
                    || pc.Is(CustomRoles.Prohibited)
                    || pc.Is(CustomRoles.DoubleAgent))
                    return false;
                if (!pc.Is(Custom_Team.Impostor))
                    return false;
                break;

            case CustomRoles.Clumsy:
                if (pc.Is(CustomRoles.Swift)
                    || pc.Is(CustomRoles.Bomber)
                    || pc.Is(CustomRoles.KillingMachine))
                    return false;
                if (!pc.GetCustomRole().IsImpostor())
                    return false;
                break;

            case CustomRoles.Burst:
                if (pc.Is(CustomRoles.Avanger)
                    || pc.Is(CustomRoles.Trapper)
                    || pc.Is(CustomRoles.Randomizer)
                    || pc.Is(CustomRoles.Solsticer)
                    || pc.Is(CustomRoles.Bait)
                    || pc.Is(CustomRoles.Ghoul)
                    || pc.Is(CustomRoles.PunchingBag))
                    return false;
                break;

            case CustomRoles.Avanger:
                if (pc.Is(CustomRoles.Burst)
                    || pc.Is(CustomRoles.Randomizer)
                    || pc.Is(CustomRoles.Solsticer)
                    || pc.Is(CustomRoles.NiceMini)
                    || pc.Is(CustomRoles.PunchingBag))
                    return false;
                break;

            case CustomRoles.Paranoia:
                if (pc.Is(CustomRoles.Dictator)
                    || pc.Is(CustomRoles.Madmate)
                    || pc.Is(CustomRoles.VoidBallot)
                    || pc.Is(CustomRoles.GuardianAngelTOHE))
                    return false;
                if (!pc.GetCustomRole().IsImpostor() && !pc.GetCustomRole().IsCrewmate() && !pc.GetCustomRole().IsCoven())
                    return false;
                if ((pc.GetCustomRole().IsImpostor() && !Paranoia.CanBeImp.GetBool()) || (pc.GetCustomRole().IsCrewmate() && !Paranoia.CanBeCrew.GetBool()) || (pc.GetCustomRole().IsCoven() && !Paranoia.CanBeCov.GetBool()))
                    return false;
                if (pc.GetCustomRole().IsNotKnightable() && Paranoia.DualVotes.GetBool())
                    return false;
                break;

            case CustomRoles.Loyal:
                if (pc.Is(CustomRoles.Madmate)
                    || pc.Is(CustomRoles.Oiiai)
                    || pc.Is(CustomRoles.GuardianAngelTOHE)
                    || pc.Is(CustomRoles.Influenced)
                    || pc.Is(CustomRoles.Solsticer)
                    || pc.Is(CustomRoles.NiceMini)
                    || pc.Is(CustomRoles.EvilMini)
                    || (pc.Is(CustomRoles.CopyCat) && CopyCat.CanCopyTeamChangingAddon()))
                    return false;
                if (!pc.GetCustomRole().IsImpostor() && !pc.GetCustomRole().IsCrewmate() && !pc.GetCustomRole().IsCoven())
                    return false;
                if ((pc.GetCustomRole().IsImpostor() && !Loyal.ImpCanBeLoyal.GetBool()) || (pc.GetCustomRole().IsCrewmate() && !Loyal.CrewCanBeLoyal.GetBool()) || (pc.GetCustomRole().IsCoven() && !Loyal.CovenCanBeLoyal.GetBool()))
                    return false;
                break;

            case CustomRoles.Gravestone:
                if (pc.Is(CustomRoles.SuperStar)
                    || pc.Is(CustomRoles.Innocent)
                    || pc.Is(CustomRoles.Solsticer)
                    || pc.Is(CustomRoles.NiceMini)
                    || pc.Is(CustomRoles.Marshall))
                    return false;
                break;

            case CustomRoles.Unreportable:
                if (pc.Is(CustomRoles.Randomizer)
                    || pc.Is(CustomRoles.Solsticer)
                    || pc.Is(CustomRoles.Bait))
                    return false;
                break;

            case CustomRoles.Prohibited:
                if (Prohibited.GetCountBlokedVents() <= 0 || !pc.CanUseVents())
                    return false;
                if (pc.Is(CustomRoles.Ventguard)
                    || pc.Is(CustomRoles.Circumvent)
                    || pc.Is(CustomRoles.Jester) && Jester.CantMoveInVents.GetBool()
                    )
                    return false;
                break;

            case CustomRoles.Flash:
                if (pc.Is(CustomRoles.Swooper)
                    || pc.Is(CustomRoles.Solsticer)
                    || pc.Is(CustomRoles.Tired)
                    || pc.Is(CustomRoles.Statue)
                    || pc.Is(CustomRoles.Seeker)
                    || pc.Is(CustomRoles.Doppelganger)
                    || pc.Is(CustomRoles.DollMaster)
                    || pc.Is(CustomRoles.Sloth)
                    || pc.Is(CustomRoles.Zombie)
                    || pc.Is(CustomRoles.Wraith)
                    || pc.Is(CustomRoles.Spurt)
                    || pc.Is(CustomRoles.Chameleon)
                    || pc.Is(CustomRoles.Alchemist)
                    || pc.Is(CustomRoles.Mare)
                    || pc.Is(CustomRoles.ShapeMaster)
                    || pc.Is(CustomRoles.ShapeshifterTOHE)
                    || pc.Is(CustomRoles.Morphling))
                    return false;
                break;

            case CustomRoles.Fool:
                if (pc.Is(CustomRoles.Mechanic)
                    || pc.Is(CustomRoles.GuardianAngelTOHE)
                    || pc.Is(CustomRoles.Alchemist)
                    || pc.Is(CustomRoles.Troller))
                    return false;
                break;

            case CustomRoles.Influenced:
                if (pc.Is(CustomRoles.Dictator)
                    || pc.Is(CustomRoles.Loyal)
                    || pc.Is(CustomRoles.VoidBallot)
                    || pc.Is(CustomRoles.Tiebreaker)
                    || pc.Is(CustomRoles.Collector)
                    || pc.Is(CustomRoles.Keeper))
                    return false;
                break;

            case CustomRoles.Oiiai:
                if (pc.Is(CustomRoles.Loyal)
                    || pc.Is(CustomRoles.Solsticer)
                    || pc.Is(CustomRoles.Innocent)
                    || pc.Is(CustomRoles.PunchingBag))
                    return false;
                break;

            case CustomRoles.Hurried:
                if (pc.Is(CustomRoles.Youtuber)
                    || pc.Is(CustomRoles.Egoist)
                    || pc.Is(CustomRoles.Cleanser)
                    || pc.Is(CustomRoles.Solsticer))
                    return false;
                if (pc.Is(CustomRoles.Madmate) && !Hurried.CanBeOnMadMate.GetBool()) return false;
                if (!pc.GetCustomRole().IsCrewmate() && !pc.Is(CustomRoles.Madmate)) return false;
                if (pc.GetCustomRole().IsTasklessCrewmate()) return false;
                if (pc.GetCustomRole().IsTaskBasedCrewmate() && !Hurried.CanBeOnTaskBasedCrew.GetBool()) return false;
                break;

            case CustomRoles.Silent:
                if (pc.Is(CustomRoles.Dictator) || pc.Is(CustomRoles.VoidBallot)) return false;
                break;

            case CustomRoles.Rainbow:
                if (pc.Is(CustomRoles.Doppelganger)
                    || pc.Is(CustomRoles.DollMaster)
                    || pc.Is(CustomRoles.Chameleon)
                    || pc.Is(CustomRoles.Swooper)
                    || pc.Is(CustomRoles.Alchemist)
                    || pc.Is(CustomRoles.Wraith))
                    return false;
                break;

            case CustomRoles.Tired:
                if (pc.Is(CustomRoles.Overseer)
                  || pc.Is(CustomRoles.Alchemist)
                  || pc.Is(CustomRoles.Torch)
                  || pc.Is(CustomRoles.Bewilder)
                  || pc.Is(CustomRoles.Lighter)
                  || pc.Is(CustomRoles.Flash)
                  || pc.Is(CustomRoles.Mare)
                  || pc.Is(CustomRoles.Sloth)
                  || pc.Is(CustomRoles.Troller))
                    return false;
                break;

            case CustomRoles.Statue:
                if (pc.Is(CustomRoles.Alchemist)
                    || pc.Is(CustomRoles.Flash)
                    || pc.Is(CustomRoles.Tired)
                    || pc.Is(CustomRoles.Sloth))
                    return false;
                break;

            case CustomRoles.Susceptible:
                if (pc.Is(CustomRoles.Jester))
                    return false;
                break;

            case CustomRoles.Sloth:
                if (pc.Is(CustomRoles.Swooper)
                    || pc.Is(CustomRoles.Solsticer)
                    || pc.Is(CustomRoles.Tired)
                    || pc.Is(CustomRoles.Statue)
                    || pc.Is(CustomRoles.Seeker)
                    || pc.Is(CustomRoles.Doppelganger)
                    || pc.Is(CustomRoles.DollMaster)
                    || pc.Is(CustomRoles.Flash)
                    || pc.Is(CustomRoles.Zombie)
                    || pc.Is(CustomRoles.Wraith)
                    || pc.Is(CustomRoles.Spurt)
                    || pc.Is(CustomRoles.Chameleon)
                    || pc.Is(CustomRoles.Alchemist)
                    || pc.Is(CustomRoles.Mare))
                    return false;
                break;
            case CustomRoles.Evader:
                if (pc.IsNeutralApocalypse())
                    return false;
                break;
        }

        return true;
    }
    public static RoleTypes GetRoleTypes(this CustomRoles role)
        => GetVNRole(role) switch // Dog Shit
        {
            CustomRoles.Crewmate => RoleTypes.Crewmate,
            CustomRoles.Impostor => RoleTypes.Impostor,
            CustomRoles.Scientist => RoleTypes.Scientist,
            CustomRoles.Engineer => RoleTypes.Engineer,
            CustomRoles.GuardianAngel => RoleTypes.GuardianAngel,
            CustomRoles.Shapeshifter => RoleTypes.Shapeshifter,
            CustomRoles.Noisemaker => RoleTypes.Noisemaker,
            CustomRoles.Phantom => RoleTypes.Phantom,
            CustomRoles.Tracker => RoleTypes.Tracker,
            CustomRoles.Detective => RoleTypes.Detective,
            CustomRoles.Viper => RoleTypes.Viper,
            _ => role.IsImpostor() ? RoleTypes.Impostor : RoleTypes.Crewmate,
        };

    public static RoleTypes GetRoleTypesDirect(this CustomRoles role)
    {
        return role switch
        {
            CustomRoles.Crewmate => RoleTypes.Crewmate,
            CustomRoles.Impostor => RoleTypes.Impostor,
            CustomRoles.Scientist => RoleTypes.Scientist,
            CustomRoles.Engineer => RoleTypes.Engineer,
            CustomRoles.GuardianAngel => RoleTypes.GuardianAngel,
            CustomRoles.Shapeshifter => RoleTypes.Shapeshifter,
            CustomRoles.Noisemaker => RoleTypes.Noisemaker,
            CustomRoles.Phantom => RoleTypes.Phantom,
            CustomRoles.Tracker => RoleTypes.Tracker,
            CustomRoles.Detective => RoleTypes.Detective,
            CustomRoles.Viper => RoleTypes.Viper,
            _ => role.IsImpostor() ? RoleTypes.Impostor : RoleTypes.Crewmate,
        };
    }
    public static bool IsDesyncRole(this CustomRoles role) => role.GetDYRole() != RoleTypes.GuardianAngel;
    /// <summary>
    /// Role is Madmate Or Impostor
    /// </summary>
    public static bool IsImpostorTeam(this CustomRoles role) => role.IsImpostor() || role == CustomRoles.Madmate;
    /// <summary>
    /// Role Is Not Impostor nor Madmate Nor Neutral nor Coven.
    /// </summary>
    public static bool IsCrewmate(this CustomRoles role) => !role.IsImpostor() && !role.IsNeutral() && !role.IsMadmate() && !role.IsCoven();
    /// <summary>
    /// Role is Rascal or Madmate and not trickster.
    /// </summary>
    public static bool IsImpostorTeamV2(this CustomRoles role) => role == CustomRoles.Rascal || role == CustomRoles.Madmate || (role.IsImpostorTeamV3() && role != CustomRoles.Trickster && (!role.IsConverted() || role is CustomRoles.Madmate));
    /// <summary>
    /// Role Is Converting or neutral.
    /// </summary>
    public static bool IsNeutralTeamV2(this CustomRoles role) => (role.IsConverted() && role != CustomRoles.Madmate || role.IsNeutral()) && role != CustomRoles.Madmate;
    /// <summary>
    /// Role is not impostor nor rascal nor madmate nor converting nor neutral or role is trickster.
    /// </summary>
    public static bool IsCrewmateTeamV2(this CustomRoles role) => !(role.IsImpostorTeamV2() || role.IsNeutralTeamV2()) || role == CustomRoles.Trickster;
    /// <summary>
    /// Role is Enchanted Or Coven
    /// </summary>
    public static bool IsCovenTeam(this CustomRoles role) => role.IsCoven() || role == CustomRoles.Enchanted;
    public static bool IsImpostorTeamV3(this CustomRoles role) => role.IsImpostor() || role.IsMadmate();
    public static bool IsNeutralKillerTeam(this CustomRoles role) => role.IsNK() && !role.IsMadmate();
    public static bool IsPassiveNeutralTeam(this CustomRoles role) => role.IsNonNK() && !role.IsMadmate();
    public static bool IsNNK(this CustomRoles role) => role.IsNeutral() && !role.IsNK();
    public static bool IsVanilla(this CustomRoles role)
    {
        return role is
            CustomRoles.Crewmate or
            CustomRoles.Impostor or
            CustomRoles.Scientist or
            CustomRoles.Engineer or
            CustomRoles.GuardianAngel or
            CustomRoles.Shapeshifter or
            CustomRoles.Noisemaker or
            CustomRoles.Phantom or
            CustomRoles.Tracker or
            CustomRoles.Detective or
            CustomRoles.Viper;
    }
    public static Custom_Team GetCustomRoleTeam(this CustomRoles role)
    {
        Custom_Team team = Custom_Team.Crewmate;
        if (role.IsImpostor()) team = Custom_Team.Impostor;
        if (role.IsNeutral()) team = Custom_Team.Neutral;
        if (role.IsCoven()) team = Custom_Team.Coven;
        if (role.IsAdditionRole()) team = Custom_Team.Addon;
        return team;
    }
    public static Custom_RoleType GetCustomRoleType(this CustomRoles role)
    {
        return role.GetStaticRoleClass().ThisRoleType;
    }
    public static bool RoleExist(this CustomRoles role, bool countDead = false) => Main.AllPlayerControls.Any(x => x.Is(role) && (x.IsAlive() || countDead));
    public static int GetCount(this CustomRoles role)
    {
        if (role.IsVanilla())
        {
            if (Options.DisableVanillaRoles.GetBool()) return 0;
            var roleOpt = Main.NormalOptions.RoleOptions;
            return role switch
            {
                CustomRoles.Crewmate => roleOpt.GetNumPerGame(RoleTypes.Crewmate),
                CustomRoles.Scientist => roleOpt.GetNumPerGame(RoleTypes.Scientist),
                CustomRoles.Engineer => roleOpt.GetNumPerGame(RoleTypes.Engineer),
                CustomRoles.GuardianAngel => roleOpt.GetNumPerGame(RoleTypes.GuardianAngel),
                CustomRoles.Shapeshifter => roleOpt.GetNumPerGame(RoleTypes.Shapeshifter),
                CustomRoles.Noisemaker => roleOpt.GetNumPerGame(RoleTypes.Noisemaker),
                CustomRoles.Phantom => roleOpt.GetNumPerGame(RoleTypes.Phantom),
                CustomRoles.Tracker => roleOpt.GetNumPerGame(RoleTypes.Tracker),
                CustomRoles.Detective => roleOpt.GetNumPerGame(RoleTypes.Detective),
                CustomRoles.Viper => roleOpt.GetNumPerGame(RoleTypes.Viper),
                _ => 0
            };
        }
        else
        {
            return Options.GetRoleCount(role);
        }
    }
    public static int GetMode(this CustomRoles role) => Options.GetRoleSpawnMode(role);
    public static float GetChance(this CustomRoles role)
    {
        if (role.IsVanilla())
        {
            var roleOpt = Main.NormalOptions.RoleOptions;
            return role switch
            {
                CustomRoles.Crewmate => roleOpt.GetChancePerGame(RoleTypes.Crewmate),
                CustomRoles.Scientist => roleOpt.GetChancePerGame(RoleTypes.Scientist),
                CustomRoles.Engineer => roleOpt.GetChancePerGame(RoleTypes.Engineer),
                CustomRoles.GuardianAngel => roleOpt.GetChancePerGame(RoleTypes.GuardianAngel),
                CustomRoles.Shapeshifter => roleOpt.GetChancePerGame(RoleTypes.Shapeshifter),
                CustomRoles.Noisemaker => roleOpt.GetChancePerGame(RoleTypes.Noisemaker),
                CustomRoles.Phantom => roleOpt.GetChancePerGame(RoleTypes.Phantom),
                CustomRoles.Tracker => roleOpt.GetChancePerGame(RoleTypes.Tracker),
                CustomRoles.Detective => roleOpt.GetChancePerGame(RoleTypes.Detective),
                CustomRoles.Viper => roleOpt.GetChancePerGame(RoleTypes.Viper),
                _ => 0
            } / 100f;
        }
        else
        {
            return Options.GetRoleChance(role);
        }
    }
    public static bool IsEnable(this CustomRoles role) => role.GetCount() > 0;
    public static CountTypes GetCountTypes(this CustomRoles role)
       => role switch
       {
           CustomRoles.GM => CountTypes.OutOfGame,
           CustomRoles.Runner => CountTypes.OutOfGame,
           CustomRoles.Jackal => CountTypes.Jackal,
           CustomRoles.Sidekick => CountTypes.Jackal,
           CustomRoles.Doppelganger => CountTypes.Doppelganger,
           CustomRoles.Bandit => CountTypes.Bandit,
           CustomRoles.Pelican => CountTypes.Pelican,
           CustomRoles.Minion => CountTypes.Impostor,
           CustomRoles.Bloodmoon => CountTypes.Impostor,
           CustomRoles.Possessor => CountTypes.Impostor,
           CustomRoles.Demon => CountTypes.Demon,
           CustomRoles.BloodKnight => CountTypes.BloodKnight,
           CustomRoles.Cultist => CountTypes.Cultist,
           CustomRoles.Stalker => Stalker.SnatchesWins ? CountTypes.Crew : CountTypes.Stalker,
           CustomRoles.Arsonist => Arsonist.CanIgniteAnytime() ? CountTypes.Arsonist : CountTypes.Crew,
           CustomRoles.Shroud => CountTypes.Shroud,
           CustomRoles.Werewolf => CountTypes.Werewolf,
           CustomRoles.Wraith => CountTypes.Wraith,
           var r when r.IsNA() => CountTypes.Apocalypse,
           var r when r.IsCoven() => CountTypes.Coven,
           CustomRoles.Enchanted => CountTypes.Coven,
           CustomRoles.Agitater => CountTypes.Agitater,
           CustomRoles.Parasite => CountTypes.Impostor,
           CustomRoles.SerialKiller => CountTypes.SerialKiller,
           CustomRoles.Quizmaster => Quizmaster.CanKillsAfterMark() ? CountTypes.Quizmaster : CountTypes.Crew,
           CustomRoles.Juggernaut => CountTypes.Juggernaut,
           CustomRoles.Infectious or CustomRoles.Infected => CountTypes.Infectious,
           CustomRoles.Crewpostor => CountTypes.Impostor,
           CustomRoles.Pyromaniac => CountTypes.Pyromaniac,
           CustomRoles.PlagueDoctor => CountTypes.PlagueDoctor,
           CustomRoles.Virus => CountTypes.Virus,
           CustomRoles.Pickpocket => CountTypes.Pickpocket,
           CustomRoles.Traitor => CountTypes.Traitor,
           CustomRoles.Refugee => CountTypes.Impostor,
           CustomRoles.Huntsman => CountTypes.Huntsman,
           CustomRoles.Glitch => CountTypes.Glitch,
           CustomRoles.Spiritcaller => CountTypes.Spiritcaller,
           CustomRoles.RuthlessRomantic => CountTypes.RuthlessRomantic,
           CustomRoles.Shocker => CountTypes.Shocker,
           CustomRoles.SchrodingersCat => CountTypes.None,
           CustomRoles.Solsticer => CountTypes.None,
           CustomRoles.Revenant => CountTypes.None,
           _ => role.IsImpostorTeam() ? CountTypes.Impostor : CountTypes.Crew,

           // CustomRoles.Phantom => CountTypes.OutOfGame,
           //   CustomRoles.CursedSoul => CountTypes.OutOfGame, // if they count as OutOfGame, it prevents them from winning lmao
       };
    public static CustomWinner GetNeutralCustomWinnerFromRole(this CustomRoles role) // only to be used for Neutrals
        => role switch
        {
            CustomRoles.Jester => CustomWinner.Jester,
            CustomRoles.Terrorist => CustomWinner.Terrorist,
            CustomRoles.Lovers => CustomWinner.Lovers,
            CustomRoles.Executioner => CustomWinner.Executioner,
            CustomRoles.Arsonist => CustomWinner.Arsonist,
            CustomRoles.Pyromaniac => CustomWinner.Pyromaniac,
            CustomRoles.Agitater => CustomWinner.Agitater,
            CustomRoles.Revolutionist => CustomWinner.Revolutionist,
            CustomRoles.Jackal => CustomWinner.Jackal,
            CustomRoles.Sidekick => CustomWinner.Sidekick,
            CustomRoles.God => CustomWinner.God,
            CustomRoles.Vector => CustomWinner.Vector,
            CustomRoles.Innocent => CustomWinner.Innocent,
            CustomRoles.Inquisitor => CustomWinner.Inquisitor,
            CustomRoles.Pelican => CustomWinner.Pelican,
            CustomRoles.Youtuber => CustomWinner.Youtuber,
            CustomRoles.Egoist => CustomWinner.Egoist,
            CustomRoles.Demon => CustomWinner.Demon,
            CustomRoles.Stalker => CustomWinner.Stalker,
            CustomRoles.Workaholic => CustomWinner.Workaholic,
            CustomRoles.Solsticer => CustomWinner.Solsticer,
            CustomRoles.Collector => CustomWinner.Collector,
            CustomRoles.BloodKnight => CustomWinner.BloodKnight,
            CustomRoles.Cultist => CustomWinner.Cultist,
            CustomRoles.Wraith => CustomWinner.Wraith,
            CustomRoles.Bandit => CustomWinner.Bandit,
            CustomRoles.Pirate => CustomWinner.Pirate,
            CustomRoles.SerialKiller => CustomWinner.SerialKiller,
            CustomRoles.Quizmaster => CustomWinner.Quizmaster,
            CustomRoles.Werewolf => CustomWinner.Werewolf,
            CustomRoles.Huntsman => CustomWinner.Huntsman,
            CustomRoles.Juggernaut => CustomWinner.Juggernaut,
            CustomRoles.Infectious => CustomWinner.Infectious,
            CustomRoles.Virus => CustomWinner.Virus,
            CustomRoles.Specter => CustomWinner.Specter,
            CustomRoles.CursedSoul => CustomWinner.CursedSoul,
            CustomRoles.Pickpocket => CustomWinner.Pickpocket,
            CustomRoles.Traitor => CustomWinner.Traitor,
            CustomRoles.Vulture => CustomWinner.Vulture,
            CustomRoles.Apocalypse => CustomWinner.Apocalypse,
            CustomRoles.Spiritcaller => CustomWinner.Spiritcaller,
            CustomRoles.Glitch => CustomWinner.Glitch,
            CustomRoles.PunchingBag => CustomWinner.PunchingBag,
            CustomRoles.Doomsayer => CustomWinner.Doomsayer,
            CustomRoles.Shroud => CustomWinner.Shroud,
            CustomRoles.Seeker => CustomWinner.Seeker,
            CustomRoles.SoulCollector => CustomWinner.SoulCollector,
            CustomRoles.RuthlessRomantic => CustomWinner.RuthlessRomantic,
            CustomRoles.Mini => CustomWinner.NiceMini,
            CustomRoles.Doppelganger => CustomWinner.Doppelganger,
            CustomRoles.Shocker => CustomWinner.Shocker,
            _ => throw new NotImplementedException()

        };
    public static CustomRoles GetNeutralCustomRoleFromCountType(this CountTypes type) //only to be used for NKs
        => type switch
        {
            CountTypes.OutOfGame => CustomRoles.GM,
            CountTypes.Jackal => CustomRoles.Jackal,
            CountTypes.Doppelganger => CustomRoles.Doppelganger,
            CountTypes.Bandit => CustomRoles.Bandit,
            CountTypes.Pelican => CustomRoles.Pelican,
            CountTypes.Demon => CustomRoles.Demon,
            CountTypes.BloodKnight => CustomRoles.BloodKnight,
            CountTypes.Cultist => CustomRoles.Cultist,
            CountTypes.Shroud => CustomRoles.Shroud,
            CountTypes.Werewolf => CustomRoles.Werewolf,
            CountTypes.Wraith => CustomRoles.Wraith,
            CountTypes.Apocalypse => CustomRoles.Apocalypse,
            CountTypes.Agitater => CustomRoles.Agitater,
            CountTypes.SerialKiller => CustomRoles.SerialKiller,
            CountTypes.Quizmaster => CustomRoles.Quizmaster,
            CountTypes.Juggernaut => CustomRoles.Juggernaut,
            CountTypes.Infectious => CustomRoles.Infectious,
            CountTypes.Pyromaniac => CustomRoles.Pyromaniac,
            CountTypes.Virus => CustomRoles.Virus,
            CountTypes.Pickpocket => CustomRoles.Pickpocket,
            CountTypes.Traitor => CustomRoles.Traitor,
            CountTypes.Huntsman => CustomRoles.Huntsman,
            CountTypes.Glitch => CustomRoles.Glitch,
            CountTypes.Stalker => CustomRoles.Stalker,
            CountTypes.Spiritcaller => CustomRoles.Spiritcaller,
            CountTypes.Arsonist => CustomRoles.Arsonist,
            CountTypes.RuthlessRomantic => CustomRoles.RuthlessRomantic,
            CountTypes.Shocker => CustomRoles.Shocker,
            _ => throw new NotImplementedException()
        };
    public static bool HasSubRole(this PlayerControl pc) => Main.PlayerStates[pc.PlayerId].SubRoles.Any();

    /// <summary>
    /// Whether the role is in the given role bucket
    /// </summary>
    public static bool IsInRoleBucket(this CustomRoles role, RoleBucket bucket)
    {
        var roleType = role.GetStaticRoleClass().ThisRoleType;

        return bucket switch
        {
            RoleBucket.ImpostorKilling => roleType is Custom_RoleType.ImpostorKilling,
            RoleBucket.ImpostorSupport => roleType is Custom_RoleType.ImpostorSupport,
            RoleBucket.ImpostorConcealing => roleType is Custom_RoleType.ImpostorConcealing,
            RoleBucket.ImpostorHindering => roleType is Custom_RoleType.ImpostorHindering,
            RoleBucket.ImpostorCommon => roleType is Custom_RoleType.ImpostorSupport or Custom_RoleType.ImpostorConcealing or Custom_RoleType.ImpostorHindering,
            RoleBucket.ImpostorRandom => roleType is Custom_RoleType.ImpostorKilling or Custom_RoleType.ImpostorSupport or Custom_RoleType.ImpostorConcealing or Custom_RoleType.ImpostorHindering,

            RoleBucket.CrewmateBasic => roleType is Custom_RoleType.CrewmateBasic,
            RoleBucket.CrewmateSupport => roleType is Custom_RoleType.CrewmateSupport,
            RoleBucket.CrewmateKilling => roleType is Custom_RoleType.CrewmateKilling,
            RoleBucket.CrewmatePower => roleType is Custom_RoleType.CrewmatePower,
            RoleBucket.CrewmateCommon => roleType is Custom_RoleType.CrewmateBasic or Custom_RoleType.CrewmateSupport or Custom_RoleType.CrewmateKilling,
            RoleBucket.CrewmateRandom => roleType is Custom_RoleType.CrewmatePower or Custom_RoleType.CrewmateBasic or Custom_RoleType.CrewmateSupport or Custom_RoleType.CrewmateKilling,

            RoleBucket.NeutralBenign => roleType is Custom_RoleType.NeutralBenign,
            RoleBucket.NeutralEvil => roleType is Custom_RoleType.NeutralEvil,
            RoleBucket.NeutralChaos => roleType is Custom_RoleType.NeutralChaos,
            RoleBucket.NeutralKilling => roleType is Custom_RoleType.NeutralKilling,
            RoleBucket.NeutralApocalypse => roleType is Custom_RoleType.NeutralApocalypse,
            RoleBucket.NeutralRandom => roleType is Custom_RoleType.NeutralBenign or Custom_RoleType.NeutralEvil or Custom_RoleType.NeutralChaos or Custom_RoleType.NeutralKilling or Custom_RoleType.NeutralApocalypse,

            RoleBucket.CovenPower => roleType is Custom_RoleType.CovenPower,
            RoleBucket.CovenKilling => roleType is Custom_RoleType.CovenKilling,
            RoleBucket.CovenTrickery => roleType is Custom_RoleType.CovenTrickery,
            RoleBucket.CovenUtility => roleType is Custom_RoleType.CovenUtility,
            RoleBucket.CovenCommon => roleType is Custom_RoleType.CovenTrickery or Custom_RoleType.CovenUtility,
            RoleBucket.CovenRandom => roleType is Custom_RoleType.CovenPower or Custom_RoleType.CovenKilling or Custom_RoleType.CovenTrickery or Custom_RoleType.CovenUtility,

            RoleBucket.Any => true,
            _ => false
        };
    }
}
[Obfuscation(Exclude = true)]
public enum Custom_Team
{
    Crewmate,
    Impostor,
    Neutral,
    Coven,
    Addon,
}
[Obfuscation(Exclude = true)]
public enum Custom_RoleType
{
    // Impostors
    ImpostorVanilla,
    ImpostorKilling,
    ImpostorSupport,
    ImpostorConcealing,
    ImpostorHindering,
    ImpostorGhosts,
    Madmate,

    // Crewmate
    CrewmateVanilla,
    CrewmateVanillaGhosts,
    CrewmateBasic,
    CrewmateSupport,
    CrewmateKilling,
    CrewmatePower,
    CrewmateGhosts,

    // Neutral
    NeutralBenign,
    NeutralEvil,
    NeutralChaos,
    NeutralKilling,
    NeutralApocalypse,

    // Coven
    CovenPower,
    CovenKilling,
    CovenTrickery,
    CovenUtility,

    None
}
[Obfuscation(Exclude = true)]
public enum CountTypes
{
    OutOfGame,
    None,
    Crew,
    Impostor,
    Jackal,
    Doppelganger,
    Bandit,
    Pelican,
    PlagueDoctor,
    Demon,
    BloodKnight,
    Poisoner,
    Charmed,
    Cultist,
    Wraith,
    SerialKiller,
    Juggernaut,
    Infectious,
    Virus,
    Stalker,
    Pickpocket,
    Traitor,
    Spiritcaller,
    Quizmaster,
    Apocalypse,
    Glitch,
    Arsonist,
    Huntsman,
    Pyromaniac,
    Shroud,
    Werewolf,
    Agitater,
    RuthlessRomantic,
    Shocker,
    Coven
}
[Obfuscation(Exclude = true)]
public enum RoleBucket
{
    Any,

    // Impostors
    ImpostorKilling,
    ImpostorSupport,
    ImpostorConcealing,
    ImpostorHindering,
    ImpostorCommon, // Common = All except Killing
    ImpostorRandom,

    // Crewmate
    CrewmateBasic,
    CrewmateSupport,
    CrewmateKilling,
    CrewmatePower,
    CrewmateCommon, // Common = All except Power
    CrewmateRandom,

    // Neutral
    NeutralBenign,
    NeutralEvil,
    NeutralChaos,
    NeutralKilling,
    NeutralApocalypse,
    NeutralRandom,

    // Coven
    CovenPower,
    CovenKilling,
    CovenTrickery,
    CovenUtility,
    CovenCommon, // Common = All except Power and Killing
    CovenRandom,

    
    None
}
