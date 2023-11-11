using Hazel;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;

namespace TOHE;

public static class NameColorManager
{
    public static string ApplyNameColorData(this string name, PlayerControl seer, PlayerControl target, bool isMeeting)
    {
        if (!AmongUsClient.Instance.IsGameStarted) return name;

        if (!TryGetData(seer, target, out var colorCode))
        {
            if (KnowTargetRoleColor(seer, target, isMeeting, out var color))
                colorCode = color == "" ? target.GetRoleColorCode() : color;
        }
        string openTag = "", closeTag = "";
        if (colorCode != "")
        {
            if (!colorCode.StartsWith('#'))
                colorCode = "#" + colorCode;
            openTag = $"<color={colorCode}>";
            closeTag = "</color>";
        }
        return openTag + name + closeTag;
    }
    private static bool KnowTargetRoleColor(PlayerControl seer, PlayerControl target, bool isMeeting, out string color)
    {
        color = "";

        // Impostor & Madmate
        if (seer.Is(CustomRoleTypes.Impostor) && target.Is(CustomRoleTypes.Impostor)) color = (target.Is(CustomRoles.Egoist) && Options.ImpEgoistVisibalToAllies.GetBool() && seer != target) ? Main.roleColors[CustomRoles.Egoist] : Main.roleColors[CustomRoles.Impostor];
        if (seer.Is(CustomRoles.Madmate) && target.Is(CustomRoleTypes.Impostor) && Options.MadmateKnowWhosImp.GetBool()) color = Main.roleColors[CustomRoles.Impostor];
        if (seer.Is(CustomRoles.Crewpostor) && target.Is(CustomRoleTypes.Impostor) && Options.CrewpostorKnowsAllies.GetBool()) color = Main.roleColors[CustomRoles.Impostor];
        if (seer.Is(CustomRoleTypes.Impostor) && target.Is(CustomRoles.Madmate) && Options.ImpKnowWhosMadmate.GetBool()) color = Main.roleColors[CustomRoles.Madmate];
        if (seer.Is(CustomRoleTypes.Impostor) && target.Is(CustomRoles.Crewpostor) && Options.AlliesKnowCrewpostor.GetBool()) color = Main.roleColors[CustomRoles.Madmate];
        if (seer.Is(CustomRoles.Madmate) && target.Is(CustomRoles.Madmate) && Options.MadmateKnowWhosMadmate.GetBool()) color = Main.roleColors[CustomRoles.Madmate];
        if (seer.Is(CustomRoles.Gangster) && target.Is(CustomRoles.Madmate)) color = Main.roleColors[CustomRoles.Madmate];

        // Mini
        if (!seer.GetCustomRole().IsImpostorTeam() && (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini))) color = Main.roleColors[CustomRoles.Mini];

        // Succubus
        if (seer.Is(CustomRoles.Charmed) && target.Is(CustomRoles.Succubus)) color = Main.roleColors[CustomRoles.Succubus];
        if (seer.Is(CustomRoles.Succubus) && target.Is(CustomRoles.Charmed)) color = Main.roleColors[CustomRoles.Charmed];
        if (seer.Is(CustomRoles.Charmed) && target.Is(CustomRoles.Charmed) && Succubus.TargetKnowOtherTarget.GetBool()) color = Main.roleColors[CustomRoles.Charmed];

        // Cursed Soul
        if (seer.Is(CustomRoles.CursedSoul) && (target.Is(CustomRoles.Soulless))) color = Main.roleColors[CustomRoles.Soulless];

        // Admirer
        if (seer.Is(CustomRoles.Admirer) && (target.Is(CustomRoles.Admired))) color = Main.roleColors[CustomRoles.Admirer];
        if (seer.Is(CustomRoles.Admired) && (target.Is(CustomRoles.Admirer))) color = Main.roleColors[CustomRoles.Admirer];

        // Investigator
    /*    if (seer.Is(CustomRoles.Investigator) && Investigator.InvestigatedList.ContainsKey(target.PlayerId) && target.GetCustomRole().IsCK() && Investigator.CrewKillingShowAs.GetInt() == 0) color = "ff1919";
        if (seer.Is(CustomRoles.Investigator) && Investigator.InvestigatedList.ContainsKey(target.PlayerId) && target.GetCustomRole().IsCK() && Investigator.CrewKillingShowAs.GetInt() == 1) color = "00ff00";
        if (seer.Is(CustomRoles.Investigator) && Investigator.InvestigatedList.ContainsKey(target.PlayerId) && target.GetCustomRole().IsCK() && Investigator.CrewKillingShowAs.GetInt() == 2) color = "696969";
        if (seer.Is(CustomRoles.Investigator) && Investigator.InvestigatedList.ContainsKey(target.PlayerId) && target.GetCustomRole().IsNK() && Investigator.NeutralKillingShowAs.GetInt() == 0) color = "ff1919";
        if (seer.Is(CustomRoles.Investigator) && Investigator.InvestigatedList.ContainsKey(target.PlayerId) && target.GetCustomRole().IsNK() && Investigator.NeutralKillingShowAs.GetInt() == 1) color = "00ff00";
        if (seer.Is(CustomRoles.Investigator) && Investigator.InvestigatedList.ContainsKey(target.PlayerId) && target.GetCustomRole().IsNK() && Investigator.NeutralKillingShowAs.GetInt() == 2) color = "696969";
        if (seer.Is(CustomRoles.Investigator) && Investigator.InvestigatedList.ContainsKey(target.PlayerId) && target.GetCustomRole().IsNonNK() && Investigator.PassiveNeutralsShowAs.GetInt() == 0) color = "ff1919";
        if (seer.Is(CustomRoles.Investigator) && Investigator.InvestigatedList.ContainsKey(target.PlayerId) && target.GetCustomRole().IsNonNK() && Investigator.PassiveNeutralsShowAs.GetInt() == 1) color = "00ff00";
        if (seer.Is(CustomRoles.Investigator) && Investigator.InvestigatedList.ContainsKey(target.PlayerId) && target.GetCustomRole().IsNonNK() && Investigator.PassiveNeutralsShowAs.GetInt() == 2) color = "696969";
        if (seer.Is(CustomRoles.Investigator) && Investigator.InvestigatedList.ContainsKey(target.PlayerId) && target.GetCustomRole().IsImpostor()) color = "ff1919";
        if (seer.Is(CustomRoles.Investigator) && Investigator.InvestigatedList.ContainsKey(target.PlayerId) && target.GetCustomRole().IsMadmate()) color = "ff1919";
        if (seer.Is(CustomRoles.Investigator) && Investigator.InvestigatedList.ContainsKey(target.PlayerId) && target.GetCustomRole().IsCrewmate() && !target.GetCustomRole().IsCK()) color = "00ff00"; */

        // Bounties
        if (seer.Is(CustomRoles.BountyHunter) && BountyHunter.GetTarget(seer) == target.PlayerId) color = "bf1313";
        if (seer.Is(CustomRoles.Huntsman) && Huntsman.Targets.Contains(target.PlayerId)) color = "6e5524";

        // Mastermind
        if (seer.Is(CustomRoles.Mastermind) && Mastermind.ManipulateDelays.ContainsKey(target.PlayerId)) color = "#00ffa5";
        if (seer.Is(CustomRoles.Mastermind) && Mastermind.ManipulatedPlayers.ContainsKey(target.PlayerId)) color = Main.roleColors[CustomRoles.Arsonist];


        // Amnesiac
        if (seer.Is(CustomRoles.Jackal) && (target.Is(CustomRoles.Jackal))) color = Main.roleColors[CustomRoles.Jackal];
        if (seer.Is(CustomRoles.Juggernaut) && (target.Is(CustomRoles.Juggernaut))) color = Main.roleColors[CustomRoles.Juggernaut];
        if (seer.Is(CustomRoles.NSerialKiller) && (target.Is(CustomRoles.NSerialKiller))) color = Main.roleColors[CustomRoles.NSerialKiller];
        if (seer.Is(CustomRoles.Pyromaniac) && target.Is(CustomRoles.Pyromaniac)) color = Main.roleColors[CustomRoles.Pyromaniac];
        if (seer.Is(CustomRoles.Necromancer) && target.Is(CustomRoles.Necromancer)) color = Main.roleColors[CustomRoles.Necromancer];
        if (seer.Is(CustomRoles.Werewolf) && (target.Is(CustomRoles.Werewolf))) color = Main.roleColors[CustomRoles.Werewolf];
        if (seer.Is(CustomRoles.Huntsman) && target.Is(CustomRoles.Huntsman)) color = Main.roleColors[CustomRoles.Huntsman];
        if (seer.Is(CustomRoles.NWitch) && (target.Is(CustomRoles.NWitch))) color = Main.roleColors[CustomRoles.NWitch];
        if (seer.Is(CustomRoles.Shroud) && (target.Is(CustomRoles.Shroud))) color = Main.roleColors[CustomRoles.Shroud];
        if (seer.Is(CustomRoles.Jinx) && (target.Is(CustomRoles.Jinx))) color = Main.roleColors[CustomRoles.Jinx];
        if (seer.Is(CustomRoles.Wraith) && (target.Is(CustomRoles.Wraith))) color = Main.roleColors[CustomRoles.Wraith];
        if (seer.Is(CustomRoles.HexMaster) && (target.Is(CustomRoles.HexMaster))) color = Main.roleColors[CustomRoles.HexMaster];
        //if (seer.Is(CustomRoles.Occultist) && (target.Is(CustomRoles.Occultist))) color = Main.roleColors[CustomRoles.Occultist];
        if (seer.Is(CustomRoles.BloodKnight) && (target.Is(CustomRoles.BloodKnight))) color = Main.roleColors[CustomRoles.BloodKnight];
        if (seer.Is(CustomRoles.Pelican) && (target.Is(CustomRoles.Pelican))) color = Main.roleColors[CustomRoles.Pelican];
        if (seer.Is(CustomRoles.Poisoner) && (target.Is(CustomRoles.Poisoner))) color = Main.roleColors[CustomRoles.Poisoner];
        if (seer.Is(CustomRoles.Infectious) && (target.Is(CustomRoles.Infectious))) color = Main.roleColors[CustomRoles.Infectious];
        if (seer.Is(CustomRoles.Virus) && (target.Is(CustomRoles.Virus))) color = Main.roleColors[CustomRoles.Virus];
        if (seer.Is(CustomRoles.Parasite) && (target.Is(CustomRoles.Parasite))) color = Main.roleColors[CustomRoles.Parasite];
        if (seer.Is(CustomRoles.Traitor) && (target.Is(CustomRoles.Traitor))) color = Main.roleColors[CustomRoles.Traitor];
        if (seer.Is(CustomRoles.DarkHide) && (target.Is(CustomRoles.DarkHide))) color = Main.roleColors[CustomRoles.DarkHide];
        if (seer.Is(CustomRoles.Pickpocket) && (target.Is(CustomRoles.Pickpocket))) color = Main.roleColors[CustomRoles.Pickpocket];
        if (seer.Is(CustomRoles.Spiritcaller) && (target.Is(CustomRoles.Spiritcaller))) color = Main.roleColors[CustomRoles.Spiritcaller];
        if (seer.Is(CustomRoles.Medusa) && (target.Is(CustomRoles.Medusa))) color = Main.roleColors[CustomRoles.Medusa];
        if (seer.Is(CustomRoles.PotionMaster) && (target.Is(CustomRoles.PotionMaster))) color = Main.roleColors[CustomRoles.PotionMaster];
        if (seer.Is(CustomRoles.Glitch) && (target.Is(CustomRoles.Glitch))) color = Main.roleColors[CustomRoles.Glitch];
        if (seer.Is(CustomRoles.Succubus) && (target.Is(CustomRoles.Succubus))) color = Main.roleColors[CustomRoles.Succubus];

        if (seer.Is(CustomRoles.Refugee) && (target.Is(CustomRoleTypes.Impostor))) color = Main.roleColors[CustomRoles.ImpostorTOHE];
        if (seer.Is(CustomRoleTypes.Impostor) && (target.Is(CustomRoles.Refugee))) color = Main.roleColors[CustomRoles.Refugee];

        // Infectious
        if (seer.Is(CustomRoles.Infected) && target.Is(CustomRoles.Infectious)) color = Main.roleColors[CustomRoles.Infectious];
        if (seer.Is(CustomRoles.Infectious) && target.Is(CustomRoles.Infected)) color = Main.roleColors[CustomRoles.Infected];
        if (seer.Is(CustomRoles.Infected) && target.Is(CustomRoles.Infected) && Infectious.TargetKnowOtherTarget.GetBool()) color = Main.roleColors[CustomRoles.Infectious];
        //Spy
        if (seer.Is(CustomRoles.Spy) && Spy.SpyRedNameList.ContainsKey(target.PlayerId)) color = "#BA4A00";
        //Seeker
        if (seer.Is(CustomRoles.Seeker) && Seeker.Targets.ContainsValue(target.PlayerId)) color = Main.roleColors[CustomRoles.Seeker];
        //Pixie
        if (seer.Is(CustomRoles.Pixie) && Pixie.PixieTargets[seer.PlayerId].Contains(target.PlayerId)) color = Main.roleColors[CustomRoles.Pixie];
        // Pyromaniac
        if (seer.Is(CustomRoles.Pyromaniac) && Pyromaniac.DousedList.Contains(target.PlayerId)) color = "#BA4A00";

        // Cyber
        if (target.Is(CustomRoles.Cyber) && Options.CyberKnown.GetBool()) color = Main.roleColors[CustomRoles.Cyber];

        // Necroview
        if (seer.Is(CustomRoles.Necroview) && seer.IsAlive())
        {
            if (target.Data.IsDead && !target.IsAlive())
            {
                if (target.Is(CustomRoleTypes.Impostor)) color = Main.roleColors[CustomRoles.Impostor];
                if (target.Is(CustomRoles.Madmate)) color = Main.roleColors[CustomRoles.Impostor];
                if (target.Is(CustomRoles.Admired)) color = Main.roleColors[CustomRoles.Bait];
                if (target.Is(CustomRoles.Parasite)) color = Main.roleColors[CustomRoles.Impostor];
                if (target.Is(CustomRoles.Crewpostor)) color = Main.roleColors[CustomRoles.Impostor];
                if (target.Is(CustomRoles.Convict)) color = Main.roleColors[CustomRoles.Impostor];
                if (target.Is(CustomRoles.Refugee)) color = Main.roleColors[CustomRoles.Impostor];
                if (target.Is(CustomRoles.Rascal)) color = Main.roleColors[CustomRoles.Impostor];
                if (target.Is(CustomRoleTypes.Crewmate)) color = Main.roleColors[CustomRoles.Bait];
                if (target.Is(CustomRoleTypes.Neutral)) color = Main.roleColors[CustomRoles.SwordsMan];
                if (target.Is(CustomRoles.Charmed)) color = Main.roleColors[CustomRoles.SwordsMan];
                if (target.Is(CustomRoles.Infected)) color = Main.roleColors[CustomRoles.SwordsMan];
                if (target.Is(CustomRoles.Contagious)) color = Main.roleColors[CustomRoles.SwordsMan];
                if (target.Is(CustomRoles.Egoist)) color = Main.roleColors[CustomRoles.SwordsMan];
                if (target.Is(CustomRoles.Recruit)) color = Main.roleColors[CustomRoles.SwordsMan];
                if (target.Is(CustomRoles.Soulless)) color = Main.roleColors[CustomRoles.SwordsMan];
            }
        }

        // Visionary
        if (seer.Is(CustomRoles.Visionary) && seer.IsAlive())
        {
            if (target.IsAlive() && !target.Data.IsDead)
            {
                if (target.Is(CustomRoleTypes.Impostor)) color = Main.roleColors[CustomRoles.Impostor];
                if (target.Is(CustomRoles.Madmate)) color = Main.roleColors[CustomRoles.Impostor];
                if (target.Is(CustomRoles.Admired)) color = Main.roleColors[CustomRoles.Bait];
                if (target.Is(CustomRoles.Parasite)) color = Main.roleColors[CustomRoles.Impostor];
                if (target.Is(CustomRoles.Crewpostor)) color = Main.roleColors[CustomRoles.Impostor];
                if (target.Is(CustomRoles.Convict)) color = Main.roleColors[CustomRoles.Impostor];
                if (target.Is(CustomRoles.Refugee)) color = Main.roleColors[CustomRoles.Impostor];
                if (target.Is(CustomRoles.Rascal)) color = Main.roleColors[CustomRoles.Impostor];
                if (target.Is(CustomRoleTypes.Crewmate)) color = Main.roleColors[CustomRoles.Bait];
                if (target.Is(CustomRoleTypes.Neutral)) color = Main.roleColors[CustomRoles.SwordsMan];
                if (target.Is(CustomRoles.Charmed)) color = Main.roleColors[CustomRoles.SwordsMan];
                if (target.Is(CustomRoles.Infected)) color = Main.roleColors[CustomRoles.SwordsMan];
                if (target.Is(CustomRoles.Contagious)) color = Main.roleColors[CustomRoles.SwordsMan];
                if (target.Is(CustomRoles.Egoist)) color = Main.roleColors[CustomRoles.SwordsMan];
                if (target.Is(CustomRoles.Recruit)) color = Main.roleColors[CustomRoles.SwordsMan];
                if (target.Is(CustomRoles.Soulless)) color = Main.roleColors[CustomRoles.SwordsMan];
            }
        }

        // Rogue
        if (seer.Is(CustomRoles.Rogue) && target.Is(CustomRoles.Rogue) && Options.RogueKnowEachOther.GetBool()) color = Main.roleColors[CustomRoles.Rogue];

        // Jackal recruit
        if (seer.Is(CustomRoles.Jackal) && (target.Is(CustomRoles.Sidekick) || target.Is(CustomRoles.Recruit))) color = Main.roleColors[CustomRoles.Jackal];
        if (seer.Is(CustomRoles.Sidekick) && (target.Is(CustomRoles.Jackal) || target.Is(CustomRoles.Recruit) || target.Is(CustomRoles.Sidekick))) color = Main.roleColors[CustomRoles.Jackal];
        if (seer.Is(CustomRoles.Recruit) && (target.Is(CustomRoles.Jackal) || target.Is(CustomRoles.Sidekick) || target.Is(CustomRoles.Recruit))) color = Main.roleColors[CustomRoles.Jackal];

        // Spiritcaller can see Evil Spirits in meetings
        if (seer.Is(CustomRoles.Spiritcaller) && target.Is(CustomRoles.EvilSpirit)) color = Main.roleColors[CustomRoles.EvilSpirit];
 
        // Monarch seeing knighted players
        if (seer.Is(CustomRoles.Monarch) && target.Is(CustomRoles.Knighted)) color = Main.roleColors[CustomRoles.Knighted];

        // GLOW SQUID IS 
        // BEST MOB IN MINECRAFT
        //if (target.Is(CustomRoles.Glow) && Utils.IsActive(SystemTypes.Electrical)) color = Main.roleColors[CustomRoles.Glow];


        if (target.Is(CustomRoles.Mare) && Utils.IsActive(SystemTypes.Electrical) && !isMeeting) color = Main.roleColors[CustomRoles.Mare];

   /*     if (Main.ShroudList.ContainsKey(target.PlayerId) && isMeeting) color = Main.roleColors[CustomRoles.Shroud];

        if ((target.PlayerId == Pirate.PirateTarget) && isMeeting) color = Main.roleColors[CustomRoles.Pirate]; */

        // Virus
        if (seer.Is(CustomRoles.Contagious) && target.Is(CustomRoles.Virus)) color = Main.roleColors[CustomRoles.Virus];
        if (seer.Is(CustomRoles.Virus) && target.Is(CustomRoles.Contagious)) color = Main.roleColors[CustomRoles.Contagious];
        if (seer.Is(CustomRoles.Contagious) && target.Is(CustomRoles.Contagious) && Virus.TargetKnowOtherTarget.GetBool()) color = Main.roleColors[CustomRoles.Virus];

        if (color != "") return true;
        else return seer == target
            || (Main.GodMode.Value && seer.AmOwner)
            || (Main.VisibleTasksCount && Main.PlayerStates[seer.Data.PlayerId].IsDead && seer.Data.IsDead && !seer.IsAlive() && Options.GhostCanSeeOtherRoles.GetBool())
            || (seer.Is(CustomRoles.Mimic) && Main.VisibleTasksCount && Main.PlayerStates[target.Data.PlayerId].IsDead && target.Data.IsDead && !target.IsAlive() && Options.MimicCanSeeDeadRoles.GetBool())
            || (seer.Is(CustomRoles.GM) || target.Is(CustomRoles.GM))
            || seer.Is(CustomRoles.God)
            || (seer.Is(CustomRoleTypes.Impostor) && target.Is(CustomRoleTypes.Impostor))
            || (seer.Is(CustomRoles.Traitor) && target.Is(CustomRoleTypes.Impostor))
            || (seer.Is(CustomRoles.Madmate) && target.Is(CustomRoleTypes.Impostor) && Options.MadmateKnowWhosImp.GetBool())
            || (seer.Is(CustomRoleTypes.Impostor) && target.Is(CustomRoles.Madmate) && Options.ImpKnowWhosMadmate.GetBool())
            || (seer.Is(CustomRoles.Madmate) && target.Is(CustomRoles.Madmate) && Options.MadmateKnowWhosMadmate.GetBool())
            || (target.Is(CustomRoles.SuperStar) && Options.EveryOneKnowSuperStar.GetBool())
            || (target.Is(CustomRoles.Workaholic) && Options.WorkaholicVisibleToEveryone.GetBool())
            || (target.Is(CustomRoles.Doctor) && !target.IsEvilAddons() && Options.DoctorVisibleToEveryone.GetBool())
            || (target.Is(CustomRoles.Gravestone) && Main.PlayerStates[target.Data.PlayerId].IsDead)
            || (target.Is(CustomRoles.Mayor) && Options.MayorRevealWhenDoneTasks.GetBool() && target.GetPlayerTaskState().IsTaskFinished)
            || (seer.Is(CustomRoleTypes.Crewmate) && target.Is(CustomRoles.Marshall) && target.GetPlayerTaskState().IsTaskFinished)
            || EvilDiviner.IsShowTargetRole(seer, target)
            || PotionMaster.IsShowTargetRole(seer, target)
         //   || Mare.KnowTargetRoleColor(target, isMeeting)
            || (target.Is(CustomRoles.NiceMini) && Mini.EveryoneCanKnowMini.GetBool())
            || (target.Is(CustomRoles.EvilMini) && Mini.EveryoneCanKnowMini.GetBool())
            || (target.Is(CustomRoles.President) && seer.Is(CustomRoles.Madmate) && President.MadmatesSeePresident.GetBool() && President.CheckPresidentReveal[target.PlayerId] == true)
            || (target.Is(CustomRoles.President) && seer.Is(CustomRoleTypes.Neutral) && President.NeutralsSeePresident.GetBool() && President.CheckPresidentReveal[target.PlayerId] == true)
            || (target.Is(CustomRoles.President) && (seer.Is(CustomRoleTypes.Impostor) || seer.Is(CustomRoles.Crewpostor)) && President.ImpsSeePresident.GetBool() && President.CheckPresidentReveal[target.PlayerId] == true);
    }
    public static bool TryGetData(PlayerControl seer, PlayerControl target, out string colorCode)
    {
        colorCode = "";
        var state = Main.PlayerStates[seer.PlayerId];
        if (!state.TargetColorData.TryGetValue(target.PlayerId, out var value)) return false;
        colorCode = value;
        return true;
    }

    public static void Add(byte seerId, byte targetId, string colorCode = "")
    {
        if (colorCode == "")
        {
            var target = Utils.GetPlayerById(targetId);
            if (target == null) return;
            colorCode = target.GetRoleColorCode();
        }

        var state = Main.PlayerStates[seerId];
        if (state.TargetColorData.TryGetValue(targetId, out var value) && colorCode == value) return;
        state.TargetColorData.Add(targetId, colorCode);

        SendRPC(seerId, targetId, colorCode);
    }
    public static void Remove(byte seerId, byte targetId)
    {
        var state = Main.PlayerStates[seerId];
        if (!state.TargetColorData.ContainsKey(targetId)) return;
        state.TargetColorData.Remove(targetId);

        SendRPC(seerId, targetId);
    }
    public static void RemoveAll(byte seerId)
    {
        Main.PlayerStates[seerId].TargetColorData.Clear();

        SendRPC(seerId);
    }
    private static void SendRPC(byte seerId, byte targetId = byte.MaxValue, string colorCode = "")
    {
        if (!AmongUsClient.Instance.AmHost) return;

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetNameColorData, SendOption.Reliable, -1);
        writer.Write(seerId);
        writer.Write(targetId);
        writer.Write(colorCode);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte seerId = reader.ReadByte();
        byte targetId = reader.ReadByte();
        string colorCode = reader.ReadString();

        if (targetId == byte.MaxValue)
            RemoveAll(seerId);
        else if (colorCode == "")
            Remove(seerId, targetId);
        else
            Add(seerId, targetId, colorCode);
    }
}
