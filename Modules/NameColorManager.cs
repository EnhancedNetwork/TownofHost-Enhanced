using Hazel;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Core;
using TOHE.Roles.Coven;
using TOHE.Roles.Crewmate;
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
        if (Altruist.HasEnabled && seer.IsMurderedThisRound())
        {
            color = "";
            return false;
        }

        if (seer != target)
            target = DollMaster.SwapPlayerInfo(target); // If a player is possessed by the Dollmaster swap each other's controllers.

        color = seer.GetRoleClass()?.PlayerKnowTargetColor(seer, target); // returns "" unless overriden

        // Impostor & Madmate
        if (seer.Is(Custom_Team.Impostor) && target.Is(Custom_Team.Impostor)) color = (seer.Is(CustomRoles.Egoist) && target.Is(CustomRoles.Egoist) && Egoist.ImpEgoistVisibalToAllies.GetBool() && seer != target) ? Main.roleColors[CustomRoles.Egoist] : Main.roleColors[CustomRoles.Impostor];
        if (seer.Is(CustomRoles.Madmate) && target.Is(Custom_Team.Impostor) && Madmate.MadmateKnowWhosImp.GetBool()) color = Main.roleColors[CustomRoles.Impostor];
        if (seer.Is(Custom_Team.Impostor) && target.Is(CustomRoles.Madmate) && Madmate.ImpKnowWhosMadmate.GetBool()) color = Main.roleColors[CustomRoles.Madmate];
        if (seer.Is(Custom_Team.Impostor) && target.GetCustomRole().IsGhostRole() && target.GetCustomRole().IsImpostor()) color = Main.roleColors[CustomRoles.Madmate];
        if (seer.Is(CustomRoles.Madmate) && target.Is(CustomRoles.Madmate) && Madmate.MadmateKnowWhosMadmate.GetBool()) color = Main.roleColors[CustomRoles.Madmate];

        // Coven
        if (seer.Is(Custom_Team.Coven) && target.Is(Custom_Team.Coven)) color = Main.roleColors[CustomRoles.Coven];
        if (seer.Is(CustomRoles.Enchanted) && target.Is(Custom_Team.Coven) && Ritualist.EnchantedKnowsCoven.GetBool()) color = Main.roleColors[CustomRoles.Coven];
        if (seer.Is(Custom_Team.Coven) && target.Is(CustomRoles.Enchanted)) color = Main.roleColors[CustomRoles.Enchanted];
        if (seer.Is(CustomRoles.Enchanted) && target.Is(CustomRoles.Enchanted) && Ritualist.EnchantedKnowsEnchanted.GetBool()) color = Main.roleColors[CustomRoles.Enchanted];

        // Cultist
        if (Cultist.NameRoleColor(seer, target)) color = Main.roleColors[CustomRoles.Cultist];

        // Admirer
        if (seer.Is(CustomRoles.Admirer) && target.Is(CustomRoles.Admired)) color = Main.roleColors[CustomRoles.Admirer];
        if (seer.Is(CustomRoles.Admired) && target.Is(CustomRoles.Admirer)) color = Main.roleColors[CustomRoles.Admirer];

        // Bounties
        if (seer.Is(CustomRoles.BountyHunter) && BountyHunter.GetTarget(seer) == target.PlayerId) color = "bf1313";

        // Amnesiac
        if (seer.GetCustomRole() == target.GetCustomRole() && seer.GetCustomRole().IsNK()) color = Main.roleColors[seer.GetCustomRole()];

        if (seer.Is(CustomRoles.Refugee) && (target.Is(Custom_Team.Impostor))) color = Main.roleColors[CustomRoles.ImpostorTOHE];
        if (seer.Is(Custom_Team.Impostor) && (target.Is(CustomRoles.Refugee))) color = Main.roleColors[CustomRoles.Refugee];

        // Infectious
        if (Infectious.InfectedKnowColorOthersInfected(seer, target)) color = Main.roleColors[CustomRoles.Infectious];

        // Cyber
        if (!seer.Is(CustomRoles.Visionary) && target.Is(CustomRoles.Cyber) && Cyber.CyberKnown.GetBool()) color = Main.roleColors[CustomRoles.Cyber];

        // Necroview
        if (seer.Is(CustomRoles.Necroview) && seer.IsAlive())
        {
            if (target.Data.IsDead && !target.IsAlive())
            {
                color = Necroview.NameColorOptions(target);
            }
        }

        // Jackal recruit
        if (Jackal.JackalKnowRole(seer, target)) color = Main.roleColors[CustomRoles.Jackal];

        if (target.Is(CustomRoles.Mare) && Utils.IsActive(SystemTypes.Electrical) && !isMeeting) color = Main.roleColors[CustomRoles.Mare];

        //Virus
        if (Virus.KnowRoleColor(seer, target) != "") color = Virus.KnowRoleColor(seer, target);

        if (color != "" && color != string.Empty) return true;

        else return seer == target
            || (Main.GodMode.Value && seer.IsHost())
            || (Options.CurrentGameMode == CustomGameMode.FFA)
            || seer.Is(CustomRoles.GM) || target.Is(CustomRoles.GM)
            || (Main.VisibleTasksCount && Main.PlayerStates[seer.Data.PlayerId].IsDead && seer.Data.IsDead && !seer.IsAlive() && Options.GhostCanSeeOtherRoles.GetBool())
            || target.GetRoleClass().OthersKnowTargetRoleColor(seer, target)
            || Mimic.CanSeeDeadRoles(seer, target)
            || (seer.Is(Custom_Team.Impostor) && target.Is(Custom_Team.Impostor))
            || (seer.Is(CustomRoles.Madmate) && target.Is(Custom_Team.Impostor) && Madmate.MadmateKnowWhosImp.GetBool())
            || (seer.Is(Custom_Team.Impostor) && target.Is(CustomRoles.Madmate) && Madmate.ImpKnowWhosMadmate.GetBool())
            || (seer.Is(CustomRoles.Madmate) && target.Is(CustomRoles.Madmate) && Madmate.MadmateKnowWhosMadmate.GetBool())
            || Workaholic.OthersKnowWorka(target)
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
            var target = targetId.GetPlayer();
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
