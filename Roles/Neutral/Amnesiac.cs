using Hazel;
using System.Collections.Generic;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

public static class Amnesiac
{
    private static readonly int Id = 12700;
    private static List<byte> playerIdList = [];
    public static bool IsEnable = false;

    public static OptionItem RememberCooldown;
    public static OptionItem RefugeeKillCD;
    public static OptionItem IncompatibleNeutralMode;
    public static readonly string[] amnesiacIncompatibleNeutralMode =
    [
        "Role.Amnesiac",
        "Role.Pursuer",
        "Role.Follower",
        "Role.Maverick",
        "Role.Imitator",
    ];

    private static Dictionary<byte, int> RememberLimit = [];

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Amnesiac);
        //    RememberCooldown = FloatOptionItem.Create(Id + 10, "RememberCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Amnesiac])
        //        .SetValueFormat(OptionFormat.Seconds);
        /*   RefugeeKillCD = FloatOptionItem.Create(Id + 11, "RefugeeKillCD", new(0f, 180f, 2.5f), 25f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Amnesiac])
               .SetValueFormat(OptionFormat.Seconds); */
        IncompatibleNeutralMode = StringOptionItem.Create(Id + 12, "IncompatibleNeutralMode", amnesiacIncompatibleNeutralMode, 0, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Amnesiac]);
    }
    public static void Init()
    {
        playerIdList = [];
        RememberLimit = [];
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        RememberLimit.Add(playerId, 1);
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetRememberLimit, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(RememberLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte playerId = reader.ReadByte();
        int Limit = reader.ReadInt32();

        if (!RememberLimit.ContainsKey(playerId))
        {
            RememberLimit.Add(playerId, Limit);
        }
        else
        {
            RememberLimit[playerId] = Limit;
        }
    }
    //public static string GetRememberLimit() => Utils.ColorString(RememberLimit[] >= 1 ? Utils.GetRoleColor(CustomRoles.Amnesiac) : Color.gray, $"({RememberLimit})");
    public static bool KnowRole(PlayerControl player, PlayerControl target)
    {
        if (!playerIdList.Contains(player.PlayerId)) return false; //Add this next time you copy paste

        if (player.Is(CustomRoles.Infectious) && target.Is(CustomRoles.Infectious)) return true;
        if (player.Is(CustomRoles.Glitch) && target.Is(CustomRoles.Glitch)) return true;
        if (player.Is(CustomRoles.Wraith) && target.Is(CustomRoles.Wraith)) return true;
        if (player.Is(CustomRoles.Medusa) && target.Is(CustomRoles.Medusa)) return true;
        if (player.Is(CustomRoles.Pelican) && target.Is(CustomRoles.Pelican)) return true;
        if (player.Is(CustomRoles.Refugee) && target.Is(CustomRoles.Refugee)) return true;
        if (player.Is(CustomRoles.Parasite) && target.Is(CustomRoles.Parasite)) return true;
        if (player.Is(CustomRoles.SerialKiller) && target.Is(CustomRoles.SerialKiller)) return true;
        if (player.Is(CustomRoles.Pickpocket) && target.Is(CustomRoles.Pickpocket)) return true;
        if (player.Is(CustomRoles.Traitor) && target.Is(CustomRoles.Traitor)) return true;
        if (player.Is(CustomRoles.Virus) && target.Is(CustomRoles.Virus)) return true;
        if (player.Is(CustomRoles.Spiritcaller) && target.Is(CustomRoles.Spiritcaller)) return true;
        if (player.Is(CustomRoles.Succubus) && target.Is(CustomRoles.Succubus)) return true;
        if (player.Is(CustomRoles.Poisoner) && target.Is(CustomRoles.Poisoner)) return true;
        if (player.Is(CustomRoles.Shroud) && target.Is(CustomRoles.Shroud)) return true;
        if (player.Is(CustomRoles.Pyromaniac) && target.Is(CustomRoles.Pyromaniac)) return true;
        if (player.Is(CustomRoles.Refugee) && target.Is(CustomRoles.Refugee)) return true;
        if (player.Is(CustomRoles.Werewolf) && target.Is(CustomRoles.Werewolf)) return true;
        //if (player.Is(CustomRoles.Occultist) && target.Is(CustomRoles.Occultist)) return true;
        if (player.Is(CustomRoles.Refugee) && target.Is(CustomRoleTypes.Impostor)) return true;
        if (player.Is(CustomRoleTypes.Impostor) && target.Is(CustomRoles.Refugee)) return true;
        return false;
    }

}
