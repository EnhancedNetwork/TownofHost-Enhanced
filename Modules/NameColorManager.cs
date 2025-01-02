using Hazel;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Core;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;

namespace TOHE;

public static class NameColorManager
{
    public static string ApplyNameColorData(this string name, PlayerControl seer, PlayerControl target, bool isMeeting)
    {
        // Ensure game is started
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.IsGameStarted)
        {
            return name; // Return unformatted name
        }

        // Ensure seer and target are valid
        if (seer == null || target == null)
        {
            return name; // Return unformatted name
        }

       

        string colorCode = "";

        // Try to get color from data or role
        if (!TryGetData(seer, target, out colorCode))
        {
            // Check if the seer or target is a Randomizer

            if (Main.PlayerStates[seer.PlayerId].IsRandomizer || Main.PlayerStates[target.PlayerId].IsRandomizer)
            {
                // Assign default color (e.g., white for Randomizer)
                colorCode = "#FFFFFF"; // White color code
            }
            else if (KnowTargetRoleColor(seer, target, isMeeting, out var color))
            {
                colorCode = string.IsNullOrEmpty(color) ? target.GetRoleColorCode() : color;
            }
        }

        // Fallback if colorCode is still null or empty
        if (string.IsNullOrEmpty(colorCode))
        {
            return name; // No color code available, return unformatted name
        }


        // Ensure the color code starts with "#"
        if (!colorCode.StartsWith("#"))
        {
            colorCode = "#" + colorCode;
        }

        string openTag = $"<color={colorCode}>";
        string closeTag = "</color>";
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
            target = DollMaster.SwapPlayerInfo(target); // If a player is possessed by the Dollmaster, swap controllers.

        // Default color assignment logic
        color = seer.GetRoleClass()?.PlayerKnowTargetColor(seer, target); // Returns "" unless overridden.

        // Randomizer team color logic
        if (seer.Is(CustomRoles.Randomizer) || target.Is(CustomRoles.Randomizer))
        {
            color = Main.roleColors[CustomRoles.Randomizer];
            return true; // Skip further checks and directly apply Randomizer color
        }


        // Impostor & Madmate logic
        if (seer.Is(Custom_Team.Impostor) && target.Is(Custom_Team.Impostor) && !Main.PlayerStates[seer.PlayerId].IsRandomizer && !Main.PlayerStates[target.PlayerId].IsRandomizer)
            color = (seer.Is(CustomRoles.Egoist) && target.Is(CustomRoles.Egoist) && Egoist.ImpEgoistVisibalToAllies.GetBool() && seer != target)
                ? Main.roleColors[CustomRoles.Egoist]
                : Main.roleColors[CustomRoles.Impostor];
        if (seer.Is(CustomRoles.Madmate) && target.Is(Custom_Team.Impostor) && Madmate.MadmateKnowWhosImp.GetBool() && !Main.PlayerStates[seer.PlayerId].IsRandomizer && !Main.PlayerStates[target.PlayerId].IsRandomizer)
            color = Main.roleColors[CustomRoles.Impostor];
        if (seer.Is(Custom_Team.Impostor) && target.Is(CustomRoles.Madmate) && Madmate.ImpKnowWhosMadmate.GetBool() && !Main.PlayerStates[seer.PlayerId].IsRandomizer && !Main.PlayerStates[target.PlayerId].IsRandomizer)
            color = Main.roleColors[CustomRoles.Madmate];
        if (seer.Is(Custom_Team.Impostor) && target.GetCustomRole().IsGhostRole() && target.GetCustomRole().IsImpostor() && !Main.PlayerStates[seer.PlayerId].IsRandomizer && !Main.PlayerStates[target.PlayerId].IsRandomizer)
            color = Main.roleColors[CustomRoles.Impostor];
        if (seer.Is(CustomRoles.Madmate) && target.Is(CustomRoles.Madmate) && Madmate.MadmateKnowWhosMadmate.GetBool() && !Main.PlayerStates[seer.PlayerId].IsRandomizer && !Main.PlayerStates[target.PlayerId].IsRandomizer)
            color = Main.roleColors[CustomRoles.Madmate];

        // Cultist logic
        if (Cultist.NameRoleColor(seer, target))
            color = Main.roleColors[CustomRoles.Cultist];



        // Admirer logic
        if (seer.Is(CustomRoles.Admirer) && !Main.PlayerStates[seer.PlayerId].IsRandomizer && target.Is(CustomRoles.Admired)) color = Main.roleColors[CustomRoles.Admirer];
        if (seer.Is(CustomRoles.Admired) && target.Is(CustomRoles.Admirer) && !Main.PlayerStates[target.PlayerId].IsRandomizer) color = Main.roleColors[CustomRoles.Admirer];

        // Bounties
        if (seer.Is(CustomRoles.BountyHunter) && BountyHunter.GetTarget(seer) == target.PlayerId)
            color = "bf1313";

        // Amnesiac and Neutral Killer logic
        if (seer.GetCustomRole() == target.GetCustomRole() && seer.GetCustomRole().IsNK())
            color = Main.roleColors[seer.GetCustomRole()];

        // Refugee
        if (seer.Is(CustomRoles.Refugee) && (target.Is(Custom_Team.Impostor)) && !Main.PlayerStates[seer.PlayerId].IsRandomizer && !Main.PlayerStates[target.PlayerId].IsRandomizer) color = Main.roleColors[CustomRoles.ImpostorTOHE];
        if (seer.Is(Custom_Team.Impostor) && (target.Is(CustomRoles.Refugee)) && !Main.PlayerStates[seer.PlayerId].IsRandomizer && !Main.PlayerStates[target.PlayerId].IsRandomizer) color = Main.roleColors[CustomRoles.Refugee];

        // Other roles
        if (Infectious.InfectedKnowColorOthersInfected(seer, target))
            color = Main.roleColors[CustomRoles.Infectious];
        if (!seer.Is(CustomRoles.Visionary) && target.Is(CustomRoles.Cyber) && Cyber.CyberKnown.GetBool())
            color = Main.roleColors[CustomRoles.Cyber];
        if (seer.Is(CustomRoles.Necroview) && seer.IsAlive() && target.Data.IsDead && !target.IsAlive())
            color = Necroview.NameColorOptions(target);
        if (Jackal.JackalKnowRole(seer, target))
            color = Main.roleColors[CustomRoles.Jackal];
        if (target.Is(CustomRoles.Mare) && Utils.IsActive(SystemTypes.Electrical) && !isMeeting)
            color = Main.roleColors[CustomRoles.Mare];
        if (Virus.KnowRoleColor(seer, target) != "")
            color = Virus.KnowRoleColor(seer, target);

        // Default visibility checks
        return color != "" && color != string.Empty
            || seer == target
            || (Main.GodMode.Value && seer.IsHost())
            || Options.CurrentGameMode == CustomGameMode.FFA
            || seer.Is(CustomRoles.GM) || target.Is(CustomRoles.GM)
            || (Main.VisibleTasksCount && Main.PlayerStates[seer.Data.PlayerId].IsDead && seer.Data.IsDead && !seer.IsAlive() && Options.GhostCanSeeOtherRoles.GetBool())
            || target.GetRoleClass().OthersKnowTargetRoleColor(seer, target)
            || Mimic.CanSeeDeadRoles(seer, target)
            || (seer.Is(Custom_Team.Impostor) && target.Is(Custom_Team.Impostor) && !Main.PlayerStates[seer.PlayerId].IsRandomizer && !Main.PlayerStates[target.PlayerId].IsRandomizer)
            || (seer.Is(CustomRoles.Madmate) && target.Is(Custom_Team.Impostor) && Madmate.MadmateKnowWhosImp.GetBool() && !Main.PlayerStates[seer.PlayerId].IsRandomizer && !Main.PlayerStates[target.PlayerId].IsRandomizer)
            || (seer.Is(Custom_Team.Impostor) && target.Is(CustomRoles.Madmate) && Madmate.ImpKnowWhosMadmate.GetBool() && !Main.PlayerStates[seer.PlayerId].IsRandomizer && !Main.PlayerStates[target.PlayerId].IsRandomizer)
            || (seer.Is(CustomRoles.Madmate) && target.Is(CustomRoles.Madmate) && Madmate.MadmateKnowWhosMadmate.GetBool() && !Main.PlayerStates[seer.PlayerId].IsRandomizer && !Main.PlayerStates[target.PlayerId].IsRandomizer)
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
