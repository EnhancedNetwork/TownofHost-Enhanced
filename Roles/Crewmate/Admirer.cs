using Hazel;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

public static class Admirer
{
    private static readonly int Id = 30000;
    private static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    public static OptionItem AdmireCooldown;
    public static OptionItem KnowTargetRole;
    private static int AdmireLimit = new();

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.Admirer);
        AdmireCooldown = FloatOptionItem.Create(Id + 10, "AdmireCooldown", new(1f, 180f, 1f), 5f, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Admirer])
            .SetValueFormat(OptionFormat.Seconds);
        KnowTargetRole = BooleanOptionItem.Create(Id + 13, "AdmirerKnowTargetRole", true, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Admirer]);
    }
    public static void Init()
    {
        playerIdList = new();
        AdmireLimit = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        AdmireLimit = 1;
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    private static void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetAdmireLimit, SendOption.Reliable, -1);
        writer.Write(AdmireLimit);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        AdmireLimit = reader.ReadInt32();
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = AdmireLimit >= 1 ? AdmireCooldown.GetFloat() : 300f;
    public static bool CanUseKillButton(PlayerControl player) => !player.Data.IsDead && AdmireLimit >= 1;
    public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (AdmireLimit < 1) return;
        if (Mini.Age != 18 && (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Succubus), GetString("CantRecruit")));
            return;
        }
        if (CanBeAdmired(target))
        {
            if (!killer.Is(CustomRoles.Recruit) && !killer.Is(CustomRoles.Charmed) && !killer.Is(CustomRoles.Infected) && !killer.Is(CustomRoles.Contagious) && !killer.Is(CustomRoles.Admired))
            {
                AdmireLimit--;
            SendRPC();
            target.RpcSetCustomRole(CustomRoles.Admired);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Admirer), GetString("AdmiredPlayer")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Admirer), GetString("AdmirerAdmired")));
            Utils.NotifyRoles();

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);

            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Admirer.ToString(), "Assign " + CustomRoles.Admirer.ToString());
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{AdmireLimit}次魅惑机会", "Admirer");
            return;
            }
            if (killer.Is(CustomRoles.Madmate))
            {
                AdmireLimit--;
            SendRPC();
            target.RpcSetCustomRole(CustomRoles.Madmate);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Madmate), GetString("AdmiredPlayer")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Madmate), GetString("AdmirerAdmired")));
            Utils.NotifyRoles();

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);

            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Madmate.ToString(), "Assign " + CustomRoles.Madmate.ToString());
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{AdmireLimit}次魅惑机会", "Admirer");
            return;
            }
            if (killer.Is(CustomRoles.Recruit))
            {
                AdmireLimit--;
            SendRPC();
            target.RpcSetCustomRole(CustomRoles.Recruit);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Recruit), GetString("AdmiredPlayer")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Recruit), GetString("AdmirerAdmired")));
            Utils.NotifyRoles();

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);

            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Recruit.ToString(), "Assign " + CustomRoles.Recruit.ToString());
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{AdmireLimit}次魅惑机会", "Admirer");
            return;
            }
            if (killer.Is(CustomRoles.Charmed))
            {
                AdmireLimit--;
            SendRPC();
            target.RpcSetCustomRole(CustomRoles.Recruit);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Charmed), GetString("AdmiredPlayer")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Charmed), GetString("AdmirerAdmired")));
            Utils.NotifyRoles();

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);

            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Charmed.ToString(), "Assign " + CustomRoles.Charmed.ToString());
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{AdmireLimit}次魅惑机会", "Admirer");
            return;
            }
            if (killer.Is(CustomRoles.Infected))
            {
                AdmireLimit--;
            SendRPC();
            target.RpcSetCustomRole(CustomRoles.Infected);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Infected), GetString("AdmiredPlayer")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Infected), GetString("AdmirerAdmired")));
            Utils.NotifyRoles();

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);

            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Infected.ToString(), "Assign " + CustomRoles.Infected.ToString());
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{AdmireLimit}次魅惑机会", "Admirer");
            return;
            }
            if (killer.Is(CustomRoles.Contagious))
            {
                AdmireLimit--;
            SendRPC();
            target.RpcSetCustomRole(CustomRoles.Contagious);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Contagious), GetString("AdmiredPlayer")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Contagious), GetString("AdmirerAdmired")));
            Utils.NotifyRoles();

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);

            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Contagious.ToString(), "Assign " + CustomRoles.Contagious.ToString());
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{AdmireLimit}次魅惑机会", "Admirer");
            return;
            }
        }
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Admirer), GetString("AdmirerInvalidTarget")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{AdmireLimit}次魅惑机会", "Admirer");
        return;
    }
    public static bool KnowRole(PlayerControl player, PlayerControl target)
    {
        if (player.Is(CustomRoles.Admired) && target.Is(CustomRoles.Admirer)) return true;
        if (KnowTargetRole.GetBool() && player.Is(CustomRoles.Admirer) && target.Is(CustomRoles.Admired)) return true;
        return false;
    }
    public static string GetAdmireLimit() => Utils.ColorString(AdmireLimit >= 1 ? Utils.GetRoleColor(CustomRoles.Admirer).ShadeColor(0.25f) : Color.gray, $"({AdmireLimit})");
    public static bool CanBeAdmired(this PlayerControl pc)
    {
        return pc != null && (pc.GetCustomRole().IsCrewmate() || pc.GetCustomRole().IsImpostor() || 
            (pc.GetCustomRole().IsNeutral())) && !pc.Is(CustomRoles.Soulless) && !pc.Is(CustomRoles.Admired) && !pc.Is(CustomRoles.Lovers) && !pc.Is(CustomRoles.Loyal) && !((pc.Is(CustomRoles.NiceMini) || pc.Is(CustomRoles.EvilMini)) && Mini.Age < 18);
    }
}
