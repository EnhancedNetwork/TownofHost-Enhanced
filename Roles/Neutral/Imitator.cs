using Hazel;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Crewmate;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

public static class Imitator
{
    private static readonly int Id = 35050;
    private static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    public static OptionItem RememberCooldown;
    public static OptionItem RefugeeKillCD;
    public static OptionItem IncompatibleNeutralMode;
    public static readonly string[] ImitatorIncompatibleNeutralMode =
    {
        "Role.Imitator",
        "Role.Witch",
        "Role.Pursuer",
        "Role.Follower",
        "Role.Maverick",
        "Role.Amnesiac",
    };

    private static int RememberLimit = new();

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Imitator);
        RememberCooldown = FloatOptionItem.Create(Id + 10, "RememberCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Imitator])
                .SetValueFormat(OptionFormat.Seconds);
        IncompatibleNeutralMode = StringOptionItem.Create(Id + 12, "IncompatibleNeutralMode", ImitatorIncompatibleNeutralMode, 0, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Imitator]);
    }
    public static void Init()
    {
        playerIdList = new();
        RememberLimit = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        RememberLimit = 1;
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    private static void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetImitateLimit, SendOption.Reliable, -1);
        writer.Write(RememberLimit);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        RememberLimit = reader.ReadInt32();
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = RememberLimit >= 1 ? RememberCooldown.GetFloat() : 300f;
    public static bool CanUseKillButton(PlayerControl player) => !player.Data.IsDead && RememberLimit >= 1;
    public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (RememberLimit < 1) return;
        if (CanBeRememberedNeutralKiller(target))
        {
            RememberLimit--;
            SendRPC();
            killer.RpcSetCustomRole(target.GetCustomRole());

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatedNeutralKiller")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorImitated")));
            Utils.NotifyRoles();

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target); 

            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Soulless.ToString(), "Assign " + CustomRoles.Soulless.ToString());
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{RememberLimit}次魅惑机会", "Imitator");
            return;
        }
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorInvalidTarget")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{RememberLimit}次魅惑机会", "Imitator");

        if (CanBeRememberedJackal(target))
        {
            RememberLimit--;
            SendRPC();
            killer.RpcSetCustomRole(CustomRoles.Sidekick);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatedNeutralKiller")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorImitated")));
            Utils.NotifyRoles();

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);

            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Soulless.ToString(), "Assign " + CustomRoles.Soulless.ToString());
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{RememberLimit}次魅惑机会", "Imitator");
            return;
        }
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorInvalidTarget")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{RememberLimit}次魅惑机会", "Imitator");

        if (CanBeRememberedNeutral(target))
        {
            if (IncompatibleNeutralMode.GetValue() == 0)
            {
                RememberLimit--;
                SendRPC();
                killer.RpcSetCustomRole(CustomRoles.Imitator);

                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatedImitator")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorImitated")));
                Utils.NotifyRoles();

                Imitator.Add(killer.PlayerId);

                killer.ResetKillCooldown();
                killer.SetKillCooldown();
                if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
                target.RpcGuardAndKill(killer);
                target.RpcGuardAndKill(target);

                Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Soulless.ToString(), "Assign " + CustomRoles.Soulless.ToString());
                Logger.Info($"{killer.GetNameWithRole()} : 剩余{RememberLimit}次魅惑机会", "Imitator");
                return;
            }
            if (IncompatibleNeutralMode.GetValue() == 1)
            {
                RememberLimit--;
                SendRPC();
                killer.RpcSetCustomRole(CustomRoles.NWitch);

                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatedWitch")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorImitated")));
                Utils.NotifyRoles();

                NWitch.Add(killer.PlayerId);

                killer.ResetKillCooldown();
                killer.SetKillCooldown();
                if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
                target.RpcGuardAndKill(killer);
                target.RpcGuardAndKill(target);

                Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Soulless.ToString(), "Assign " + CustomRoles.Soulless.ToString());
                Logger.Info($"{killer.GetNameWithRole()} : 剩余{RememberLimit}次魅惑机会", "Imitator");
                return;
            }
            if (IncompatibleNeutralMode.GetValue() == 2)
            {
                RememberLimit--;
                SendRPC();
                killer.RpcSetCustomRole(CustomRoles.Pursuer);

                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatedPursuer")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorImitated")));
                Utils.NotifyRoles();

                Pursuer.Add(killer.PlayerId);

                killer.ResetKillCooldown();
                killer.SetKillCooldown();
                if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
                target.RpcGuardAndKill(killer);
                target.RpcGuardAndKill(target);

                Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Soulless.ToString(), "Assign " + CustomRoles.Soulless.ToString());
                Logger.Info($"{killer.GetNameWithRole()} : 剩余{RememberLimit}次魅惑机会", "Imitator");
                return;
            }
            if (IncompatibleNeutralMode.GetValue() == 3)
            {
                RememberLimit--;
                SendRPC();
                killer.RpcSetCustomRole(CustomRoles.Totocalcio);

                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatedFollower")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorImitated")));
                Utils.NotifyRoles();

                Totocalcio.Add(killer.PlayerId);

                killer.ResetKillCooldown();
                killer.SetKillCooldown();
                if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
                target.RpcGuardAndKill(killer);
                target.RpcGuardAndKill(target);

                Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Soulless.ToString(), "Assign " + CustomRoles.Soulless.ToString());
                Logger.Info($"{killer.GetNameWithRole()} : 剩余{RememberLimit}次魅惑机会", "Imitator");
                return;
            }
            if (IncompatibleNeutralMode.GetValue() == 4)
            {
                RememberLimit--;
                SendRPC();
                killer.RpcSetCustomRole(CustomRoles.Maverick);

                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatedMaverick")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorImitated")));
                Utils.NotifyRoles();


                killer.ResetKillCooldown();
                killer.SetKillCooldown();
                if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
                target.RpcGuardAndKill(killer);
                target.RpcGuardAndKill(target);

                Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Soulless.ToString(), "Assign " + CustomRoles.Soulless.ToString());
                Logger.Info($"{killer.GetNameWithRole()} : 剩余{RememberLimit}次魅惑机会", "Imitator");
                return;
            }
            if (IncompatibleNeutralMode.GetValue() == 5)
            {
                RememberLimit--;
                SendRPC();
                killer.RpcSetCustomRole(CustomRoles.Amnesiac);

                Amnesiac.Add(killer.PlayerId);


                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatedAmnesiac")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorImitated")));
                Utils.NotifyRoles();


                killer.ResetKillCooldown();
                killer.SetKillCooldown();
                if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
                target.RpcGuardAndKill(killer);
                target.RpcGuardAndKill(target);

                Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Soulless.ToString(), "Assign " + CustomRoles.Soulless.ToString());
                Logger.Info($"{killer.GetNameWithRole()} : 剩余{RememberLimit}次魅惑机会", "Imitator");
                return;
            }
        }
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorInvalidTarget")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{RememberLimit}次魅惑机会", "Imitator");
        if (CanBeRememberedImpostor(target))
        {
            RememberLimit--;
            SendRPC();
            killer.RpcSetCustomRole(CustomRoles.Refugee);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatedImpostor")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorImitated")));
            Utils.NotifyRoles();

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);

            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Soulless.ToString(), "Assign " + CustomRoles.Soulless.ToString());
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{RememberLimit}次魅惑机会", "Imitator");
            return;
        }
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorInvalidTarget")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{RememberLimit}次魅惑机会", "Imitator");
        if (CanBeRememberedCrewmate(target))
        {
            RememberLimit--;
            SendRPC();
            killer.RpcSetCustomRole(CustomRoles.Sheriff);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatedCrewmate")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorImitated")));
            Utils.NotifyRoles();

            Sheriff.Add(killer.PlayerId);

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);

            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Soulless.ToString(), "Assign " + CustomRoles.Soulless.ToString());
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{RememberLimit}次魅惑机会", "Imitator");
            return;
        }
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorInvalidTarget")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{RememberLimit}次魅惑机会", "Imitator");
        if (CanBeRememberedPoisoner(target))
        {
            RememberLimit--;
            SendRPC();
            killer.RpcSetCustomRole(CustomRoles.Poisoner);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatedNeutralKiller")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorImitated")));
            Utils.NotifyRoles();

            Poisoner.Add(killer.PlayerId);

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);

            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Soulless.ToString(), "Assign " + CustomRoles.Soulless.ToString());
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{RememberLimit}次魅惑机会", "Imitator");
            return;
        }
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorInvalidTarget")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{RememberLimit}次魅惑机会", "Imitator");
        if (CanBeRememberedJuggernaut(target))
        {
            RememberLimit--;
            SendRPC();
            killer.RpcSetCustomRole(CustomRoles.Juggernaut);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatedNeutralKiller")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorImitated")));
            Utils.NotifyRoles();

            Juggernaut.Add(killer.PlayerId);

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);

            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Soulless.ToString(), "Assign " + CustomRoles.Soulless.ToString());
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{RememberLimit}次魅惑机会", "Imitator");
            return;
        }
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorInvalidTarget")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{RememberLimit}次魅惑机会", "Imitator");
        if (CanBeRememberedHexMaster(target))
        {
            RememberLimit--;
            SendRPC();
            killer.RpcSetCustomRole(CustomRoles.HexMaster);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatedNeutralKiller")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorImitated")));
            Utils.NotifyRoles();

            HexMaster.Add(killer.PlayerId);

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);

            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Soulless.ToString(), "Assign " + CustomRoles.Soulless.ToString());
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{RememberLimit}次魅惑机会", "Imitator");
            return;
        }
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorInvalidTarget")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{RememberLimit}次魅惑机会", "Imitator");
        //if (CanBeRememberedOccultist(target))
        //{
        //    RememberLimit--;
        //    SendRPC();
        //    killer.RpcSetCustomRole(CustomRoles.HexMaster);

        //    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatedNeutralKiller")));
        //    target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorImitated")));
        //    Utils.NotifyRoles();

        //    HexMaster.Add(killer.PlayerId);

        //    killer.ResetKillCooldown();
        //    killer.SetKillCooldown();
        //    if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
        //    target.RpcGuardAndKill(killer);
        //    target.RpcGuardAndKill(target);

        //    Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Soulless.ToString(), "Assign " + CustomRoles.Soulless.ToString());
        //    Logger.Info($"{killer.GetNameWithRole()} : 剩余{RememberLimit}次魅惑机会", "Imitator");
        //    return;
        //}
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorInvalidTarget")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{RememberLimit}次魅惑机会", "Imitator");
        if (CanBeRememberedBloodKnight(target))
        {
            RememberLimit--;
            SendRPC();
            killer.RpcSetCustomRole(CustomRoles.BloodKnight);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatedNeutralKiller")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorImitated")));
            Utils.NotifyRoles();

            BloodKnight.Add(killer.PlayerId);

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);

            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Soulless.ToString(), "Assign " + CustomRoles.Soulless.ToString());
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{RememberLimit}次魅惑机会", "Imitator");
            return;
        }
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorInvalidTarget")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{RememberLimit}次魅惑机会", "Imitator");
    }
    public static string GetRememberLimit() => Utils.ColorString(RememberLimit >= 1 ? Utils.GetRoleColor(CustomRoles.Imitator) : Color.gray, $"({RememberLimit})");
    public static bool CanBeRememberedNeutralKiller(this PlayerControl pc)
    {
        return pc != null && ((pc.GetCustomRole().IsAmneNK()));
    }
    public static bool CanBeRememberedNeutral(this PlayerControl pc)
    {
        return pc != null && ((pc.GetCustomRole().IsAmneMaverick()));
    }
    public static bool CanBeRememberedImpostor(this PlayerControl pc)
    {
        return pc != null && (pc.GetCustomRole().IsImpostor() || pc.Is(CustomRoles.Madmate));
    }
    public static bool CanBeRememberedCrewmate(this PlayerControl pc)
    {
        return pc != null && (pc.GetCustomRole().IsCrewmate() && !pc.Is(CustomRoles.Madmate));
    }
    public static bool CanBeRememberedJackal(this PlayerControl pc)
    {
        return pc != null && (pc.Is(CustomRoles.Jackal));
    }
    public static bool CanBeRememberedHexMaster(this PlayerControl pc)
    {
        return pc != null && (pc.Is(CustomRoles.HexMaster));
    }
    //public static bool CanBeRememberedOccultist(this PlayerControl pc)
    //{
    //    return pc != null && (pc.Is(CustomRoles.Occultist));
    //}
    public static bool CanBeRememberedPoisoner(this PlayerControl pc)
    {
        return pc != null && (pc.Is(CustomRoles.Poisoner));
    }
    public static bool CanBeRememberedJuggernaut(this PlayerControl pc)
    {
        return pc != null && (pc.Is(CustomRoles.Juggernaut));
    }
    public static bool CanBeRememberedBloodKnight(this PlayerControl pc)
    {
        return pc != null && (pc.Is(CustomRoles.BloodKnight));
    }
    public static bool KnowRole(PlayerControl player, PlayerControl target)
    {
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
