using Hazel;
using System.Collections.Generic;

using TOHE.Roles.Crewmate;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

public static class Imitator
{
    private static readonly int Id = 13000;
    private static List<byte> playerIdList = [];
    public static bool IsEnable = false;

    public static OptionItem RememberCooldown;
    public static OptionItem RefugeeKillCD;
    public static OptionItem IncompatibleNeutralMode;
    public static readonly string[] ImitatorIncompatibleNeutralMode =
    [
        "Role.Imitator",
        "Role.Witch",
        "Role.Pursuer",
        "Role.Follower",
        "Role.Maverick",
        "Role.Amnesiac",
    ];

    private static Dictionary<byte, int> RememberLimit = [];

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Imitator);
        RememberCooldown = FloatOptionItem.Create(Id + 10, "RememberCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Imitator])
                .SetValueFormat(OptionFormat.Seconds);
        IncompatibleNeutralMode = StringOptionItem.Create(Id + 12, "IncompatibleNeutralMode", ImitatorIncompatibleNeutralMode, 0, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Imitator]);
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
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = RememberLimit[id] >= 1 ? RememberCooldown.GetFloat() : 300f;
    public static bool CanUseKillButton(PlayerControl player) => !player.Data.IsDead && (!RememberLimit.TryGetValue(player.PlayerId, out var x) || x > 0);
    public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (RememberLimit[killer.PlayerId] < 1) return;

        var role = target.GetCustomRole();

        if (role.Is(CustomRoles.Jackal)
            || role.Is(CustomRoles.HexMaster)
            || role.Is(CustomRoles.Poisoner) 
            || role.Is(CustomRoles.Juggernaut) 
            || role.Is(CustomRoles.BloodKnight)
            || role.Is(CustomRoles.Sheriff))
        {
            RememberLimit[killer.PlayerId]--;
            SendRPC(killer.PlayerId);
            killer.RpcSetCustomRole(role);

            //Do those trash add check here
            switch (role)
            {
                case CustomRoles.Jackal:
                    Jackal.Add(killer.PlayerId);
                    break;
                case CustomRoles.HexMaster:
                    HexMaster.Add(killer.PlayerId);
                    break;
                case CustomRoles.Poisoner:
                    Poisoner.Add(killer.PlayerId);
                    break;
                case CustomRoles.Juggernaut:
                    Juggernaut.Add(killer.PlayerId);
                    break;
                case CustomRoles.BloodKnight:
                    BloodKnight.Add(killer.PlayerId);
                    break;
                case CustomRoles.Sheriff:
                    Sheriff.Add(killer.PlayerId);
                    break;
            }

            if (role.IsCrewmate())
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("RememberedCrewmate")));
            else
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("RememberedNeutralKiller")));

            // Notify target
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorImitated")));
        }
        else if (role.IsAmneMaverick())
        {
            RememberLimit[killer.PlayerId]--;
            SendRPC(killer.PlayerId);
            switch (IncompatibleNeutralMode.GetInt())
            {
                case 0:
                    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("RememberedImitator")));
                    break;
                case 1:
                    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("RememberedWitch")));
                    killer.RpcSetCustomRole(CustomRoles.NWitch);
                    NWitch.Add(killer.PlayerId);
                    break;
                case 2:
                    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("RememberedPursuer")));
                    killer.RpcSetCustomRole(CustomRoles.Pursuer);
                    Pursuer.Add(killer.PlayerId);
                    break;
                case 3:
                    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("RememberedFollower")));
                    killer.RpcSetCustomRole(CustomRoles.Totocalcio);
                    Totocalcio.Add(killer.PlayerId);
                    break;
                case 4:
                    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("RememberedMaverick")));
                    killer.RpcSetCustomRole(CustomRoles.Maverick);
                    Maverick.Add(killer.PlayerId);
                    break;
                case 5:
                    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("RememberedAmnesiac")));
                    killer.RpcSetCustomRole(CustomRoles.Amnesiac);
                    Amnesiac.Add(killer.PlayerId);
                    break;
            }

        }
        else if (role.IsCrewmate())
        {
            RememberLimit[killer.PlayerId]--;
            SendRPC(killer.PlayerId);
            killer.RpcSetCustomRole(CustomRoles.Sheriff);
            Sheriff.Add(killer.PlayerId);
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("RememberedCrewmate")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorImitated")));
        }
        else if (role.IsImpostor())
        {
            RememberLimit[killer.PlayerId]--;
            SendRPC(killer.PlayerId);
            killer.RpcSetCustomRole(CustomRoles.Refugee);
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("RememberedImpostor")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorImitated")));
        }

        var killerRole = killer.GetCustomRole();

        if (killerRole != CustomRoles.Imitator)
        {
            killer.ResetKillCooldown();
            killer.SetKillCooldown(forceAnime: true);

            Logger.Info("Imitator remembered: " + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString(), "Imitator Assign");
            Logger.Info($"{killer.GetNameWithRole()} : {RememberLimit} remember limits left", "Imitator");

            Utils.NotifyRoles(SpecifySeer: killer);
        }
        else if (killerRole == CustomRoles.Imitator)
        {
            killer.SetKillCooldown(forceAnime: true);
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorInvalidTarget")));
        }

        return;
    }
    //public static string GetRememberLimit() => Utils.ColorString(RememberLimit >= 1 ? Utils.GetRoleColor(CustomRoles.Imitator) : Color.gray, $"({RememberLimit})");
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
        if (player.Is(CustomRoles.NSerialKiller) && target.Is(CustomRoles.NSerialKiller)) return true;
        if (player.Is(CustomRoles.Pickpocket) && target.Is(CustomRoles.Pickpocket)) return true;
        if (player.Is(CustomRoles.Traitor) && target.Is(CustomRoles.Traitor)) return true;
        if (player.Is(CustomRoles.Virus) && target.Is(CustomRoles.Virus)) return true;
        if (player.Is(CustomRoles.Spiritcaller) && target.Is(CustomRoles.Spiritcaller)) return true;
        if (player.Is(CustomRoles.Succubus) && target.Is(CustomRoles.Succubus)) return true;
        if (player.Is(CustomRoles.Poisoner) && target.Is(CustomRoles.Poisoner)) return true;
        if (player.Is(CustomRoles.Shroud) && target.Is(CustomRoles.Shroud)) return true;
        if (player.Is(CustomRoles.Refugee) && target.Is(CustomRoles.Refugee)) return true;
        if (player.Is(CustomRoles.Werewolf) && target.Is(CustomRoles.Werewolf)) return true;
        //if (player.Is(CustomRoles.Occultist) && target.Is(CustomRoles.Occultist)) return true;
        if (player.Is(CustomRoles.Refugee) && target.Is(CustomRoleTypes.Impostor)) return true;
        if (player.Is(CustomRoleTypes.Impostor) && target.Is(CustomRoles.Refugee)) return true;
        return false;
    }

}
