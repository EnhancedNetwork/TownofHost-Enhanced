using AmongUs.GameOptions;
using System;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Core;
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
            _ => role.IsImpostor() ? CustomRoles.Impostor : CustomRoles.Crewmate,
        };
    }

    public static RoleTypes GetDYRole(this CustomRoles role) // Role has a kill button (Non-Impostor)
    {
        if (role is CustomRoles.Killer) return RoleTypes.Impostor; // FFA

        return (role.GetStaticRoleClass().ThisRoleBase is CustomRoles.Impostor or CustomRoles.Shapeshifter) && !role.IsImpostor()
            ? role.GetStaticRoleClass().ThisRoleBase.GetRoleTypes()
            : RoleTypes.GuardianAngel;
    }

    /* Needs recode, awaiting phantom role base*/
    public static bool HasImpKillButton(this PlayerControl player, bool considerVanillaShift = false)
    {
        if (player == null) return false;
        var customRole = player.GetCustomRole();
        bool ModSideHasKillButton = customRole.GetDYRole() == RoleTypes.Impostor || customRole.GetVNRole() is CustomRoles.Impostor or CustomRoles.Shapeshifter or CustomRoles.Phantom;

        if (player.IsModded() || (!considerVanillaShift && !player.IsModded()))
            return ModSideHasKillButton;

        bool vanillaSideHasKillButton = EAC.OriginalRoles.TryGetValue(player.PlayerId, out var OriginalRole) ?
                                         (OriginalRole.GetDYRole() == RoleTypes.Impostor || OriginalRole.GetVNRole() is CustomRoles.Impostor or CustomRoles.Shapeshifter or CustomRoles.Phantom) : ModSideHasKillButton;

        return vanillaSideHasKillButton;
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
    public static bool HasGhostRole(this PlayerControl player) => player.GetCustomRole().IsGhostRole() || player.IsAnySubRole(x => x.IsGhostRole());


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
            CustomRoles.Medusa or
            CustomRoles.Necromancer or
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
        return role.GetVNRole() is CustomRoles.Impostor && role.IsCrewmate();
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
            CustomRoles.Phantom;
    }

    public static bool IsAbleToBeSidekicked(this CustomRoles role)
        => role.GetDYRole() == RoleTypes.Impostor && !role.IsImpostor() && !role.IsRecruitingRole();

    public static bool IsRecruitingRole(this CustomRoles role)
        => role is
            CustomRoles.Jackal or
            CustomRoles.Cultist or
            CustomRoles.Necromancer or
            CustomRoles.Virus or
            CustomRoles.Spiritcaller;

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
            CustomRoles.Madmate;

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
            or CustomRoles.Soulless;
    }

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
    public static bool CheckAddonConfilct(CustomRoles role, PlayerControl pc, bool checkLimitAddons = true)
    {
        // Only add-ons
        if (!role.IsAdditionRole() || pc == null) return false;

        if (Options.AddonCanBeSettings.TryGetValue(role, out var o) && ((!o.Imp.GetBool() && pc.GetCustomRole().IsImpostor()) || (!o.Neutral.GetBool() && pc.GetCustomRole().IsNeutral()) || (!o.Crew.GetBool() && pc.GetCustomRole().IsCrewmate()))) 
            return false;

        // if player already has this addon
        else if (pc.Is(role)) return false;

        // Checking Lovers and Romantics
        else if ((pc.Is(CustomRoles.RuthlessRomantic) || pc.Is(CustomRoles.Romantic) || pc.Is(CustomRoles.VengefulRomantic)) && role is CustomRoles.Lovers) return false;

        // Checking for conflicts with roles
        else if (pc.Is(CustomRoles.GM) || role is CustomRoles.Lovers || pc.Is(CustomRoles.LazyGuy)) return false;

        if (checkLimitAddons)
            if (pc.HasSubRole() && pc.GetCustomSubRoles().Count >= Options.NoLimitAddonsNumMax.GetInt()) return false;


        // Checking for conflicts with roles and other add-ons
        switch (role)
        {
            case var Addon when (pc.IsAnySubRole(x => x.IsSpeedRole()) || pc.GetCustomRole().IsSpeedRole()) && Addon.IsSpeedRole():
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
                if (Options.GuesserMode.GetBool() && ((pc.GetCustomRole().IsCrewmate() && !Guesser.CrewCanBeGuesser.GetBool()) || (pc.GetCustomRole().IsNeutral() && !Guesser.NeutralCanBeGuesser.GetBool()) || (pc.GetCustomRole().IsImpostor() && !Guesser.ImpCanBeGuesser.GetBool())))
                    return false;
                if (pc.Is(CustomRoles.EvilGuesser)
                    || pc.Is(CustomRoles.NiceGuesser)
                    || pc.Is(CustomRoles.Judge)
                    || pc.Is(CustomRoles.CopyCat)
                    || pc.Is(CustomRoles.Doomsayer)
                    || pc.Is(CustomRoles.Nemesis)
                    || pc.Is(CustomRoles.Councillor)
                    || pc.Is(CustomRoles.GuardianAngelTOHE))
                    return false;
                if ((pc.Is(CustomRoles.Specter) && !Specter.CanGuess.GetBool())
                    || (pc.Is(CustomRoles.Terrorist) && (!Terrorist.TerroristCanGuess.GetBool() || Terrorist.CanTerroristSuicideWin.GetBool()))
                    || (pc.Is(CustomRoles.Workaholic) && !Workaholic.WorkaholicCanGuess.GetBool())
                    || (pc.Is(CustomRoles.Solsticer) && !Solsticer.SolsticerCanGuess.GetBool())
                    || (pc.Is(CustomRoles.God) && !God.CanGuess.GetBool()))
                    return false; //Based on guess manager
                if ((pc.GetCustomRole().IsCrewmate() && !Guesser.CrewCanBeGuesser.GetBool()) || (pc.GetCustomRole().IsNeutral() && !Guesser.NeutralCanBeGuesser.GetBool()) || (pc.GetCustomRole().IsImpostor() && !Guesser.ImpCanBeGuesser.GetBool()))
                    return false;
                break;

            case CustomRoles.Mundane:
                if (pc.HasImpKillButton() || !Utils.HasTasks(pc.Data, false) || pc.GetCustomRole().IsTasklessCrewmate() || pc.Is(Custom_Team.Impostor))
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
                    if (DoubleShot.NeutralCanBeDoubleShot.GetBool() && !pc.Is(CustomRoles.Guesser) && !pc.Is(CustomRoles.Doomsayer) && ((pc.GetCustomRole().IsNonNK() && !Options.PassiveNeutralsCanGuess.GetBool()) || (pc.GetCustomRole().IsNK() && !Options.NeutralKillersCanGuess.GetBool())))
                        return false;
                }
                if ((pc.Is(Custom_Team.Impostor) && !DoubleShot.ImpCanBeDoubleShot.GetBool()) || (pc.Is(Custom_Team.Crewmate) && !DoubleShot.CrewCanBeDoubleShot.GetBool()) || (pc.Is(Custom_Team.Neutral) && !DoubleShot.NeutralCanBeDoubleShot.GetBool()))
                    return false;
                break;

            case CustomRoles.Cyber:
                if (pc.Is(CustomRoles.Doppelganger) || pc.Is(CustomRoles.Celebrity) || pc.Is(CustomRoles.SuperStar))
                    return false;
                break;

            case CustomRoles.Reach:
                if (!pc.CanUseKillButton())
                    return false;
                break;

            case CustomRoles.Overclocked:
                if (!pc.CanUseKillButton())
                    return false;
                break;

            case CustomRoles.Lazy:
                if (!Lazy.CheckConflicts(pc))
                    return false;
                break;

            case CustomRoles.Ghoul:
                if (pc.Is(CustomRoles.Lazy)
                    || pc.Is(CustomRoles.LazyGuy)
                    || pc.Is(CustomRoles.Mundane))
                    return false;
                if (pc.GetCustomRole().IsNeutral() || pc.GetCustomRole().IsImpostor() || pc.GetCustomRole().IsTasklessCrewmate() || pc.GetCustomRole().IsTaskBasedCrewmate())
                    return false;
                break;

            case CustomRoles.Bloodthirst:
                if (pc.Is(CustomRoles.Lazy)
                    || pc.Is(CustomRoles.Merchant)
                    || pc.Is(CustomRoles.Alchemist)
                    || pc.Is(CustomRoles.LazyGuy)
                    || pc.Is(CustomRoles.Crewpostor)
                    || pc.Is(CustomRoles.Bodyguard))
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
                if (!pc.GetCustomRole().IsCrewmate())
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
                    || pc.Is(CustomRoles.Detective)
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
                    || pc.Is(CustomRoles.GuardianAngelTOHE))
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
                    || pc.Is(CustomRoles.PunchingBag))
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
                if (pc.Is(CustomRoles.Detective)
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
                    || pc.Is(CustomRoles.GuardianAngelTOHE))
                    return false;
                if (pc.GetCustomRole().IsNeutral() || pc.GetCustomRole().IsMadmate() || pc.IsAnySubRole(sub => sub.IsConverted()))
                    return false;
                if ((pc.GetCustomRole().IsImpostor() && !Egoist.ImpCanBeEgoist.GetBool()) || (pc.GetCustomRole().IsCrewmate() && !Egoist.CrewCanBeEgoist.GetBool()))
                    return false;
                break;

            case CustomRoles.Mimic:
                if (pc.Is(CustomRoles.Nemesis))
                    return false;
                if (!pc.GetCustomRole().IsImpostor())
                    return false;
                break;

            case CustomRoles.Rascal:
                if (pc.Is(CustomRoles.SuperStar)
                    || pc.Is(CustomRoles.NiceMini)
                    || pc.Is(CustomRoles.Madmate))
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
                    || pc.Is(CustomRoles.Tired))
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
                    || pc.Is(CustomRoles.Miner))
                    return false;
                if (!pc.GetCustomRole().IsImpostor())
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
                    || pc.Is(CustomRoles.Solsticer)
                    || pc.Is(CustomRoles.Bait)
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
                    || pc.Is(CustomRoles.GuardianAngelTOHE))
                    return false;
                if (!pc.GetCustomRole().IsImpostor() && !pc.GetCustomRole().IsCrewmate())
                    return false;
                if ((pc.GetCustomRole().IsImpostor() && !Paranoia.CanBeImp.GetBool()) || (pc.GetCustomRole().IsCrewmate() && !Paranoia.CanBeCrew.GetBool()))
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
                if (!pc.GetCustomRole().IsImpostor() && !pc.GetCustomRole().IsCrewmate())
                    return false;
                if ((pc.GetCustomRole().IsImpostor() && !Loyal.ImpCanBeLoyal.GetBool()) || (pc.GetCustomRole().IsCrewmate() && !Loyal.CrewCanBeLoyal.GetBool()))
                    return false;
                break;

            case CustomRoles.Gravestone:
                if (pc.Is(CustomRoles.SuperStar)
                    || pc.Is(CustomRoles.Innocent)
                    || pc.Is(CustomRoles.Solsticer)
                    || pc.Is(CustomRoles.NiceMini))
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
                    || pc.Is(CustomRoles.Jester) && Jester.CantMoveInVents.GetBool())
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
                    || pc.Is(CustomRoles.Spurt))
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
                    || pc.Is(CustomRoles.Spurt))
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
        => GetVNRole(role) switch
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
            _ => role.IsImpostor() ? RoleTypes.Impostor : RoleTypes.Crewmate,
        };
    public static bool IsDesyncRole(this CustomRoles role) => role.GetDYRole() != RoleTypes.GuardianAngel;
    /// <summary>
    /// Role is Madmate Or Impostor
    /// </summary>
    public static bool IsImpostorTeam(this CustomRoles role) => role.IsImpostor() || role == CustomRoles.Madmate;
    /// <summary>
    /// Role Is Not Impostor nor Madmate Nor Neutral.
    /// </summary>
    public static bool IsCrewmate(this CustomRoles role) => !role.IsImpostor() && !role.IsNeutral() && !role.IsMadmate();
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
            CustomRoles.Tracker;
    }
    public static Custom_Team GetCustomRoleTeam(this CustomRoles role)
    {
        Custom_Team team = Custom_Team.Crewmate;
        if (role.IsImpostor()) team = Custom_Team.Impostor;
        if (role.IsNeutral()) team = Custom_Team.Neutral;
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
           CustomRoles.Jackal => CountTypes.Jackal,
           CustomRoles.Sidekick => CountTypes.Jackal,
           CustomRoles.Doppelganger => CountTypes.Doppelganger,
           CustomRoles.Bandit => CountTypes.Bandit,
           CustomRoles.Poisoner => CountTypes.Poisoner,
           CustomRoles.Pelican => CountTypes.Pelican,
           CustomRoles.Minion => CountTypes.Impostor,
           CustomRoles.Bloodmoon => CountTypes.Impostor,
           CustomRoles.Possessor => CountTypes.Impostor,
           CustomRoles.Demon => CountTypes.Demon,
           CustomRoles.BloodKnight => CountTypes.BloodKnight,
           CustomRoles.Cultist => CountTypes.Cultist,
           CustomRoles.HexMaster => CountTypes.HexMaster,
           CustomRoles.Necromancer => CountTypes.Necromancer,
           CustomRoles.Stalker => Stalker.SnatchesWins ? CountTypes.Crew : CountTypes.Stalker,
           CustomRoles.Arsonist => Arsonist.CanIgniteAnytime() ? CountTypes.Arsonist : CountTypes.Crew,
           CustomRoles.Shroud => CountTypes.Shroud,
           CustomRoles.Werewolf => CountTypes.Werewolf,
           CustomRoles.Wraith => CountTypes.Wraith,
           var r when r.IsNA() => CountTypes.Apocalypse,
           CustomRoles.Agitater => CountTypes.Agitater,
           CustomRoles.Parasite => CountTypes.Impostor,
           CustomRoles.SerialKiller => CountTypes.SerialKiller,
           CustomRoles.Quizmaster => CountTypes.Quizmaster,
           CustomRoles.Juggernaut => CountTypes.Juggernaut,
           CustomRoles.Jinx => CountTypes.Jinx,
           CustomRoles.Infectious or CustomRoles.Infected => CountTypes.Infectious,
           CustomRoles.Crewpostor => CountTypes.Impostor,
           CustomRoles.Pyromaniac => CountTypes.Pyromaniac,
           CustomRoles.PlagueDoctor => CountTypes.PlagueDoctor,
           CustomRoles.Virus => CountTypes.Virus,
           CustomRoles.PotionMaster => CountTypes.PotionMaster,
           CustomRoles.Pickpocket => CountTypes.Pickpocket,
           CustomRoles.Traitor => CountTypes.Traitor,
           CustomRoles.Medusa => CountTypes.Medusa,
           CustomRoles.Refugee => CountTypes.Impostor,
           CustomRoles.Huntsman => CountTypes.Huntsman,
           CustomRoles.Glitch => CountTypes.Glitch,
           CustomRoles.Spiritcaller => CountTypes.Spiritcaller,
           CustomRoles.RuthlessRomantic => CountTypes.RuthlessRomantic,
           CustomRoles.SchrodingersCat => CountTypes.None,
           CustomRoles.Solsticer => CountTypes.None,
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
            CustomRoles.Pelican => CustomWinner.Pelican,
            CustomRoles.Youtuber => CustomWinner.Youtuber,
            CustomRoles.Egoist => CustomWinner.Egoist,
            CustomRoles.Demon => CustomWinner.Demon,
            CustomRoles.Stalker => CustomWinner.Stalker,
            CustomRoles.Workaholic => CustomWinner.Workaholic,
            CustomRoles.Solsticer => CustomWinner.Solsticer,
            CustomRoles.Collector => CustomWinner.Collector,
            CustomRoles.BloodKnight => CustomWinner.BloodKnight,
            CustomRoles.Poisoner => CustomWinner.Poisoner,
            CustomRoles.HexMaster => CustomWinner.HexMaster,
            CustomRoles.Cultist => CustomWinner.Cultist,
            CustomRoles.Wraith => CustomWinner.Wraith,
            CustomRoles.Bandit => CustomWinner.Bandit,
            CustomRoles.Pirate => CustomWinner.Pirate,
            CustomRoles.SerialKiller => CustomWinner.SerialKiller,
            CustomRoles.Quizmaster => CustomWinner.Quizmaster,
            CustomRoles.Werewolf => CustomWinner.Werewolf,
            CustomRoles.Necromancer => CustomWinner.Necromancer,
            CustomRoles.Huntsman => CustomWinner.Huntsman,
            CustomRoles.Juggernaut => CustomWinner.Juggernaut,
            CustomRoles.Infectious => CustomWinner.Infectious,
            CustomRoles.Virus => CustomWinner.Virus,
            CustomRoles.Specter => CustomWinner.Specter,
            CustomRoles.Jinx => CustomWinner.Jinx,
            CustomRoles.CursedSoul => CustomWinner.CursedSoul,
            CustomRoles.PotionMaster => CustomWinner.PotionMaster,
            CustomRoles.Pickpocket => CustomWinner.Pickpocket,
            CustomRoles.Traitor => CustomWinner.Traitor,
            CustomRoles.Vulture => CustomWinner.Vulture,
            CustomRoles.Apocalypse => CustomWinner.Apocalypse,
            CustomRoles.Medusa => CustomWinner.Medusa,
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
            _ => throw new NotImplementedException()

        };
    public static CustomRoles GetNeutralCustomRoleFromCountType(this CountTypes type) //only to be used for NKs
        => type switch
        {
            CountTypes.OutOfGame => CustomRoles.GM,
            CountTypes.Jackal => CustomRoles.Jackal,
            CountTypes.Doppelganger => CustomRoles.Doppelganger,
            CountTypes.Bandit => CustomRoles.Bandit,
            CountTypes.Poisoner => CustomRoles.Poisoner,
            CountTypes.Pelican => CustomRoles.Pelican,
            CountTypes.Demon => CustomRoles.Demon,
            CountTypes.BloodKnight => CustomRoles.BloodKnight,
            CountTypes.Cultist => CustomRoles.Cultist,
            CountTypes.HexMaster => CustomRoles.HexMaster,
            CountTypes.Necromancer => CustomRoles.Necromancer,
            CountTypes.Shroud => CustomRoles.Shroud,
            CountTypes.Werewolf => CustomRoles.Werewolf,
            CountTypes.Wraith => CustomRoles.Wraith,
            CountTypes.Apocalypse => CustomRoles.Apocalypse,
            CountTypes.Agitater => CustomRoles.Agitater,
            CountTypes.SerialKiller => CustomRoles.SerialKiller,
            CountTypes.Quizmaster => CustomRoles.Quizmaster,
            CountTypes.Juggernaut => CustomRoles.Juggernaut,
            CountTypes.Jinx => CustomRoles.Jinx,
            CountTypes.Infectious => CustomRoles.Infectious,
            CountTypes.Pyromaniac => CustomRoles.Pyromaniac,
            CountTypes.Virus => CustomRoles.Virus,
            CountTypes.PotionMaster => CustomRoles.PotionMaster,
            CountTypes.Pickpocket => CustomRoles.Pickpocket,
            CountTypes.Traitor => CustomRoles.Traitor,
            CountTypes.Medusa => CustomRoles.Medusa,
            CountTypes.Huntsman => CustomRoles.Huntsman,
            CountTypes.Glitch => CustomRoles.Glitch,
            CountTypes.Stalker => CustomRoles.Stalker,
            CountTypes.Spiritcaller => CustomRoles.Spiritcaller,
            CountTypes.Arsonist => CustomRoles.Arsonist,
            CountTypes.RuthlessRomantic => CustomRoles.RuthlessRomantic,
            _ => throw new NotImplementedException()
        };
    public static bool HasSubRole(this PlayerControl pc) => Main.PlayerStates[pc.PlayerId].SubRoles.Any();
}
public enum Custom_Team
{
    Crewmate,
    Impostor,
    Neutral,
    Addon,
}
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

    None
}
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
    HexMaster,
    Wraith,
    SerialKiller,
    Juggernaut,
    Infectious,
    Virus,
    Stalker,
    Jinx,
    PotionMaster,
    Pickpocket,
    Traitor,
    Medusa,
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
    Necromancer
}
