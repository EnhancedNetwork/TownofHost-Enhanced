using Hazel;
using System.Collections.Generic;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

public static class CursedSoul
{
    private static readonly int Id = 14000;
    private static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    public static OptionItem CurseCooldown;
    public static OptionItem CurseCooldownIncrese;
    public static OptionItem CurseMax;
    public static OptionItem KnowTargetRole;
    public static OptionItem CanCurseNeutral;
    private static int CurseLimit = new();

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.CursedSoul, 1, zeroOne: false);
        CurseCooldown = FloatOptionItem.Create(Id + 10, "CursedSoulCurseCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CursedSoul])
            .SetValueFormat(OptionFormat.Seconds);
        CurseCooldownIncrese = FloatOptionItem.Create(Id + 11, "CursedSoulCurseCooldownIncrese", new(0f, 180f, 2.5f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CursedSoul])
            .SetValueFormat(OptionFormat.Seconds);
        CurseMax = IntegerOptionItem.Create(Id + 12, "CursedSoulCurseMax", new(1, 15, 1), 3, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CursedSoul])
            .SetValueFormat(OptionFormat.Times);
        KnowTargetRole = BooleanOptionItem.Create(Id + 13, "CursedSoulKnowTargetRole", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CursedSoul]);
        CanCurseNeutral = BooleanOptionItem.Create(Id + 16, "CursedSoulCanCurseNeutral", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CursedSoul]);
    }
    public static void Init()
    {
        playerIdList = new();
        CurseLimit = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        CurseLimit = CurseMax.GetInt();
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    private static void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCursedSoulCurseLimit, SendOption.Reliable, -1);
        writer.Write(CurseLimit);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        CurseLimit = reader.ReadInt32();
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CurseLimit >= 1 ? CurseCooldown.GetFloat() + (CurseMax.GetInt() - CurseLimit) * CurseCooldownIncrese.GetFloat() : 300f;
    public static bool CanUseKillButton(PlayerControl player) => !player.Data.IsDead && CurseLimit >= 1;
    public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (CurseLimit < 1) return;
        if (Mini.Age < 18 && (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Succubus), GetString("CantRecruit")));
            return;
        }
        if (CanBeSoulless(target))
        {
            CurseLimit--;
            SendRPC();
            target.RpcSetCustomRole(CustomRoles.Soulless);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.CursedSoul), GetString("CursedSoulSoullessPlayer")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.CursedSoul), GetString("SoullessByCursedSoul")));
            Utils.NotifyRoles();

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
       //     target.RpcGuardAndKill(killer);
       //     target.RpcGuardAndKill(target);

            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Soulless.ToString(), "Assign " + CustomRoles.Soulless.ToString());
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{CurseLimit}次魅惑机会", "CursedSoul");
            return;
        }
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.CursedSoul), GetString("CursedSoulInvalidTarget")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{CurseLimit}次魅惑机会", "CursedSoul");
        return;
    }
    public static bool KnowRole(PlayerControl player, PlayerControl target)
    {
       // if (player.Is(CustomRoles.Soulless) && target.Is(CustomRoles.CursedSoul)) return true;
        if (KnowTargetRole.GetBool() && player.Is(CustomRoles.CursedSoul) && target.Is(CustomRoles.Soulless)) return true;
        return false;
    }
    public static string GetCurseLimit() => Utils.ColorString(CurseLimit >= 1 ? Utils.GetRoleColor(CustomRoles.CursedSoul) : Color.gray, $"({CurseLimit})");
    public static bool CanBeSoulless(this PlayerControl pc)
    {
        return pc != null && (pc.GetCustomRole().IsCrewmate() || pc.GetCustomRole().IsImpostor() || 
            (CanCurseNeutral.GetBool() && pc.GetCustomRole().IsNeutral())) && !pc.Is(CustomRoles.Soulless) && !pc.Is(CustomRoles.Admired) && !pc.Is(CustomRoles.Loyal);
    }
}