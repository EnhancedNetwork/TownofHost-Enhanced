using Hazel;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Core;
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
        color = seer.GetRoleClass()?.PlayerKnowTargetColor(seer, target); // returns "" unless overriden
        
        // Impostor & Madmate
        if (seer.Is(CustomRoleTypes.Impostor) && target.Is(CustomRoleTypes.Impostor)) color = (seer.Is(CustomRoles.Egoist) && target.Is(CustomRoles.Egoist) && Egoist.ImpEgoistVisibalToAllies.GetBool() && seer != target) ? Main.roleColors[CustomRoles.Egoist] : Main.roleColors[CustomRoles.Impostor];
        if (seer.Is(CustomRoles.Madmate) && target.Is(CustomRoleTypes.Impostor) && Madmate.MadmateKnowWhosImp.GetBool()) color = Main.roleColors[CustomRoles.Impostor];
        if (seer.Is(CustomRoleTypes.Impostor) && target.Is(CustomRoles.Madmate) && Madmate.ImpKnowWhosMadmate.GetBool()) color = Main.roleColors[CustomRoles.Madmate];
        if (seer.Is(CustomRoleTypes.Impostor) && (target.GetCustomRole().IsGhostRole() && target.GetCustomRole().IsImpostor())) color = Main.roleColors[CustomRoles.Madmate];
        if (seer.Is(CustomRoles.Madmate) && target.Is(CustomRoles.Madmate) && Madmate.MadmateKnowWhosMadmate.GetBool()) color = Main.roleColors[CustomRoles.Madmate];

        // Succubus
        if (seer.Is(CustomRoles.Charmed) && target.Is(CustomRoles.Succubus)) color = Main.roleColors[CustomRoles.Succubus];
        if (seer.Is(CustomRoles.Succubus) && target.Is(CustomRoles.Charmed)) color = Main.roleColors[CustomRoles.Charmed];
        if (seer.Is(CustomRoles.Charmed) && target.Is(CustomRoles.Charmed) && Succubus.TargetKnowOtherTarget.GetBool()) color = Main.roleColors[CustomRoles.Charmed];

        // Cursed Soul
        if (seer.Is(CustomRoles.CursedSoul) && (target.Is(CustomRoles.Soulless))) color = Main.roleColors[CustomRoles.Soulless];

        // Admirer
        if (seer.Is(CustomRoles.Admirer) && (target.Is(CustomRoles.Admired))) color = Main.roleColors[CustomRoles.Admirer];
        if (seer.Is(CustomRoles.Admired) && (target.Is(CustomRoles.Admirer))) color = Main.roleColors[CustomRoles.Admirer];

        // Bounties
        if (seer.Is(CustomRoles.BountyHunter) && BountyHunter.GetTarget(seer) == target.PlayerId) color = "bf1313";


        // Amnesiac
        if (seer.GetCustomRole() == target.GetCustomRole() && seer.GetCustomRole().IsNK()) color = Main.roleColors[seer.GetCustomRole()];

        if (seer.Is(CustomRoles.Refugee) && (target.Is(CustomRoleTypes.Impostor))) color = Main.roleColors[CustomRoles.ImpostorTOHE];
        if (seer.Is(CustomRoleTypes.Impostor) && (target.Is(CustomRoles.Refugee))) color = Main.roleColors[CustomRoles.Refugee];

        // Infectious
        if (seer.Is(CustomRoles.Infected) && target.Is(CustomRoles.Infectious)) color = Main.roleColors[CustomRoles.Infectious];
        if (seer.Is(CustomRoles.Infectious) && target.Is(CustomRoles.Infected)) color = Main.roleColors[CustomRoles.Infected];
        if (seer.Is(CustomRoles.Infected) && target.Is(CustomRoles.Infected) && Infectious.TargetKnowOtherTarget.GetBool()) color = Main.roleColors[CustomRoles.Infectious];
        

        //Pixie
        if (seer.Is(CustomRoles.Pixie) && Pixie.PixieTargets[seer.PlayerId].Contains(target.PlayerId)) color = Main.roleColors[CustomRoles.Pixie];

        // Cyber
        if (target.Is(CustomRoles.Cyber) && Cyber.CyberKnown.GetBool()) color = Main.roleColors[CustomRoles.Cyber];
        //Schrodingers Cat
        if (seer.Is(CustomRoles.SchrodingersCat))
        {
            if (SchrodingersCat.teammate.ContainsKey(seer.PlayerId) && target.PlayerId == SchrodingersCat.teammate[seer.PlayerId])
            {
                if (target.GetCustomRole().IsCrewmate()) color = "#8CFFFF";
                else color = Main.roleColors[target.GetCustomRole()];
            }
        }
        if (target.Is(CustomRoles.SchrodingersCat))
        {
            if (SchrodingersCat.teammate.ContainsKey(target.PlayerId) && seer.PlayerId == SchrodingersCat.teammate[target.PlayerId])
            {
                if (seer.GetCustomRole().IsCrewmate()) color = "#8CFFFF";
                else color = Main.roleColors[seer.GetCustomRole()];
            }
        }
        // Necroview
        if (seer.Is(CustomRoles.Necroview) && seer.IsAlive())
        {
            if (target.Data.IsDead && !target.IsAlive())
            {
                color = Necroview.NameColorOptions(target);
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
                if (target.Is(CustomRoles.Minion)) color = Main.roleColors[CustomRoles.Impostor];
                if (target.Is(CustomRoles.Convict)) color = Main.roleColors[CustomRoles.Impostor];
                if (target.Is(CustomRoles.Refugee)) color = Main.roleColors[CustomRoles.Impostor];
                if (target.Is(CustomRoles.Rascal)) color = Main.roleColors[CustomRoles.Impostor];
                if (target.Is(CustomRoleTypes.Crewmate)) color = Main.roleColors[CustomRoles.Bait];
                if (target.Is(CustomRoleTypes.Neutral)) color = Main.roleColors[CustomRoles.Knight];
                if (target.Is(CustomRoles.Charmed)) color = Main.roleColors[CustomRoles.Knight];
                if (target.Is(CustomRoles.Infected)) color = Main.roleColors[CustomRoles.Knight];
                if (target.Is(CustomRoles.Contagious)) color = Main.roleColors[CustomRoles.Knight];
                if (target.Is(CustomRoles.Egoist)) color = Main.roleColors[CustomRoles.Knight];
                if (target.Is(CustomRoles.Recruit)) color = Main.roleColors[CustomRoles.Knight];
                if (target.Is(CustomRoles.Soulless)) color = Main.roleColors[CustomRoles.Knight];
            }
        }

        // Jackal recruit
        if (Jackal.JackalKnowRoleTarget(seer, target)) color = Main.roleColors[CustomRoles.Jackal];

        // Spiritcaller can see Evil Spirits in meetings
        if (seer.Is(CustomRoles.Spiritcaller) && target.Is(CustomRoles.EvilSpirit)) color = Main.roleColors[CustomRoles.EvilSpirit];

        if (target.Is(CustomRoles.Mare) && Utils.IsActive(SystemTypes.Electrical) && !isMeeting) color = Main.roleColors[CustomRoles.Mare];

        // if ((target.PlayerId == Pirate.PirateTarget) && isMeeting) color = Main.roleColors[CustomRoles.Pirate]; */

        //Virus
        if (Virus.KnowRoleColor(seer, target) != "") color = Virus.KnowRoleColor(seer, target);

        // PlagueDoctor
        if (seer.Is(CustomRoles.PlagueDoctor) && target.Is(CustomRoles.PlagueDoctor)) color = Main.roleColors[CustomRoles.PlagueDoctor];

        if (color != "") return true;
        else return seer == target
            || (Main.GodMode.Value && seer.AmOwner)
            || (Options.CurrentGameMode == CustomGameMode.FFA)
            || (Main.VisibleTasksCount && Main.PlayerStates[seer.Data.PlayerId].IsDead && seer.Data.IsDead && !seer.IsAlive() && Options.GhostCanSeeOtherRoles.GetBool())
            || seer.Is(CustomRoles.GM) || target.Is(CustomRoles.GM)
            || target.GetRoleClass().OthersKnowTargetRoleColor(seer, target)
            || seer.Is(CustomRoles.God)
            || Mimic.CanSeeDeadRoles(seer, target)
            || (seer.Is(CustomRoleTypes.Impostor) && target.Is(CustomRoleTypes.Impostor))
            || (seer.Is(CustomRoles.Madmate) && target.Is(CustomRoleTypes.Impostor) && Madmate.MadmateKnowWhosImp.GetBool())
            || (seer.Is(CustomRoleTypes.Impostor) && target.Is(CustomRoles.Madmate) && Madmate.ImpKnowWhosMadmate.GetBool())
            || (seer.Is(CustomRoles.Madmate) && target.Is(CustomRoles.Madmate) && Madmate.MadmateKnowWhosMadmate.GetBool())
            || (target.Is(CustomRoles.Workaholic) && Options.WorkaholicVisibleToEveryone.GetBool())
            || (target.Is(CustomRoles.Gravestone) && Main.PlayerStates[target.Data.PlayerId].IsDead)
            || Mare.KnowTargetRoleColor(target, isMeeting);
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